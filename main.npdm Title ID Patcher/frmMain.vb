Imports System.IO
Imports System.Collections
Imports System.Text.RegularExpressions
Imports System.Security.Principal

Public Class frmMain
    Dim DBGameIDPath As String = Application.StartupPath & "\gametitles.txt"

    Dim identity = WindowsIdentity.GetCurrent()
    Dim principal = New WindowsPrincipal(identity)
    Dim isElevated As Boolean = principal.IsInRole(WindowsBuiltInRole.Administrator)
    Private Sub Button1_Click(sender As Object, e As EventArgs) Handles btnPatch.Click
        Dim folderPath As String = Microsoft.VisualBasic.Left(txtExefsPath.Text, Len(txtExefsPath.Text) - 6)
        Dim pFolder As String = Microsoft.VisualBasic.Left(txtExefsPath.Text, Len(txtExefsPath.Text) - 17)
        Try
            If Directory.Exists(pFolder & "\" & txtTitleID.Text) Then
                MsgBox("Game ID: " & txtTitleID.Text & " is already exist!")
                Exit Sub
            End If

            ' CloseFolder(pFolder & "\")
            Dim targetTitleIdULong As ULong = Convert.ToUInt64(txtTitleID.Text, 16)
            Dim npdmFilePath As String = Path.Combine(txtExefsPath.Text & "\exefs", "main.npdm")
            Dim npdmBytes As Byte() = File.ReadAllBytes(npdmFilePath)
            Dim patchedNpdmBytes As Byte() = getPatchedNpdmBytes(npdmBytes, targetTitleIdULong)
            File.Delete(npdmFilePath)
            File.WriteAllBytes(npdmFilePath, patchedNpdmBytes)
            FileIO.FileSystem.RenameDirectory(txtExefsPath.Text, txtTitleID.Text)
            txtExefsPath.Text = Microsoft.VisualBasic.Left(txtExefsPath.Text, Len(txtExefsPath.Text) - 16) & txtTitleID.Text
            MsgBox("Successfully Patched!", vbInformation)
            If isArg = True Then End
            ' Process.Start(pFolder)
            'End If
        Catch ex As Exception
            MsgBox(ex.Message)
        End Try

    End Sub

    Public Sub CloseFolder(myfolder As String)
        Dim OpenFolder As Object = CreateObject("shell.application")
        For Each item In OpenFolder.Windows
            If item.document.folder.self.Path = myfolder Then
                item.Quit()
            End If
        Next
    End Sub
    Public Function getPatchedNpdmBytes(fileBytes As Byte(), targetTitleId As ULong) As Byte()
        Dim aci0RawOffset As Integer = BitConverter.ToInt32(fileBytes, 112)
        'If fileBytes(aci0RawOffset) <> 65 Or fileBytes(aci0RawOffset) <> 67 Or fileBytes(aci0RawOffset) <> 73 Or fileBytes(aci0RawOffset) <> 48 Then
        '    MsgBox("Unable to decrypt NCA file, check your keyset! You should remove created files.")
        '    Exit Function
        'End If
        Dim TitleIdBytes As Byte() = BitConverter.GetBytes(targetTitleId)
        Array.Copy(TitleIdBytes, 0, fileBytes, aci0RawOffset + 16, TitleIdBytes.Length)
        Return fileBytes
    End Function

    Private Sub txtExefsPath_DragDrop(sender As Object, e As DragEventArgs) Handles txtExefsPath.DragDrop, Me.DragDrop
        Dim DroppedFiles() As String = e.Data.GetData(DataFormats.FileDrop)
        Try
            For Each Entry As String In DroppedFiles
                txtExefsPath.Text = Entry
            Next
            cmbTitles.Focus()
        Catch ex As Exception
            MessageBox.Show(ex.Message)
        End Try
    End Sub
    Private Sub txtExefsPath_DragEnter(sender As Object, e As DragEventArgs) Handles txtExefsPath.DragEnter, Me.DragEnter
        If e.Data.GetDataPresent(DataFormats.FileDrop) Then e.Effect = DragDropEffects.Copy
    End Sub
    Sub LoadTitles()
        Try
            btnDelete.Enabled = False
            Dim xRead As IO.StreamReader
            Dim words As New Dictionary(Of String, String)()
            xRead = File.OpenText(DBGameIDPath)
            Do Until xRead.EndOfStream
                Dim strArr() As String = xRead.ReadLine.Split(""",""")
                words.Add(strArr(1), strArr(3))
            Loop
            cmbTitles.DataSource = New BindingSource(words, Nothing)
            cmbTitles.DisplayMember = "Value"
            cmbTitles.ValueMember = "Key"
            xRead.Close()
            If cmbTitles.Items.Count > 0 Then btnDelete.Enabled = True Else btnDelete.Enabled = False
        Catch ex As Exception
            MsgBox(ex.Message)
        End Try
    End Sub
    Private Function ValidatePath(Path As String) As Boolean
        ValidatePath = False
        If File.Exists(Path & "\romfs.bin") And File.Exists(Path & "\exefs\main.npdm") Then txtExefsPath.ForeColor = Color.Black : Return True : Exit Function
        txtExefsPath.ForeColor = Color.Red
    End Function
    Dim isArg As Boolean = False
    Private Sub Form1_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        If My.Computer.Registry.GetValue("HKEY_CLASSES_ROOT\Folder\shell\Patch Game ID", "Icon", Nothing) Is Nothing Then btnContextMenu.Text = "Add to Context Menu" Else btnContextMenu.Text = "Remove from Context Menu"

        For Each arg As String In My.Application.CommandLineArgs
            If ValidatePath(arg.ToString) = True Then
                txtExefsPath.Text = arg.ToString
                isArg = True
            ElseIf arg.ToString <> "" Then
                MsgBox("Selected folder doesn't contains romfs.bin and main.npdm", vbInformation)
                End
            End If
        Next

        LoadTitles()

        'cmbTitles.DataSource = New BindingSource(TARGET_TITLES, Nothing)
        'cmbTitles.DisplayMember = "Value"
        'cmbTitles.ValueMember = "Key"
    End Sub


    Private Sub cmbTitles_TextChanged(sender As Object, e As EventArgs) Handles cmbTitles.TextChanged
        Try
            Dim key As String = DirectCast(cmbTitles.SelectedItem, KeyValuePair(Of String, String)).Key
            'Dim value As String = DirectCast(cmbTitles.SelectedItem, KeyValuePair(Of String, String)).Value
            txtTitleID.Text = StrConv(key, vbUpperCase)
        Catch ex As Exception
            If btnPatch.Enabled = True Then txtTitleID.Text = ""
        End Try
        If btnDelete.Text = "SAVE" Then
            If cmbTitles.Text <> "" And (New Regex("^[a-fA-F0-9]{16}$")).Match(txtTitleID.Text).Success Then btnDelete.Enabled = True Else btnDelete.Enabled = False
        End If
    End Sub

    Private Sub txtTitleID_TextChanged(sender As Object, e As EventArgs) Handles txtTitleID.TextChanged, txtExefsPath.TextChanged
        On Error Resume Next
        Dim pFolder As String = Microsoft.VisualBasic.Left(txtExefsPath.Text, Len(txtExefsPath.Text) - 16)
        Dim isFolderExist As Boolean = False
        For Each Dir As String In Directory.GetDirectories(pFolder)
            If Dir.Remove(0, pFolder.Length) = txtTitleID.Text Then isFolderExist = True
        Next
        If Microsoft.VisualBasic.Right(txtExefsPath.Text, 16) = txtTitleID.Text Or isFolderExist = True Then txtTitleID.ForeColor = Color.Red Else txtTitleID.ForeColor = Color.Black
        If txtTitleID.ForeColor = Color.Black And ValidatePath(txtExefsPath.Text) = True And txtTitleID.Text <> "" And
            (New Regex("^[a-fA-F0-9]{16}$")).Match(txtTitleID.Text).Success And btnDelete.Text = "DELETE" Then
            btnPatch.Enabled = True
        Else
            btnPatch.Enabled = False
        End If
        If btnDelete.Text = "SAVE" Then
            If cmbTitles.Text <> "" And (New Regex("^[a-fA-F0-9]{16}$")).Match(txtTitleID.Text).Success Then btnDelete.Enabled = True Else btnDelete.Enabled = False
        End If
    End Sub
    Dim filestring As String
    Private Sub Button1_Click_1(sender As Object, e As EventArgs) Handles btnNew.Click
        Select Case btnNew.Text
            Case "NEW"
                cmbTitles.Text = ""
                txtTitleID.Text = ""
                cmbTitles.Focus()
                btnNew.Text = "CANCEL"
                btnDelete.Text = "SAVE"
                btnDelete.Enabled = False
            Case "CANCEL"
                LoadTitles()
                btnNew.Text = "NEW"
                btnDelete.Text = "DELETE"
                cmbTitles.Focus()
        End Select
    End Sub

    Private Sub Button1_Click_2(sender As Object, e As EventArgs) Handles btnDelete.Click
        Select Case btnDelete.Text
            Case "DELETE"
                If MsgBox("Do you want to delete this game?" & vbCrLf & "Game Title: " & cmbTitles.Text & vbCrLf & "Game ID: " & txtTitleID.Text, vbQuestion + vbYesNo) = vbYes Then
                    Dim lines As List(Of String) = System.IO.File.ReadAllLines(DBGameIDPath).ToList
                    lines.RemoveAt(cmbTitles.SelectedIndex) ' index starts at 0 
                    System.IO.File.WriteAllLines(DBGameIDPath, lines)
                    MsgBox("Game Title: " & cmbTitles.Text & " with Game ID: " & txtTitleID.Text & vbCrLf & "has been deleted!", vbInformation, "Deleted")
                    LoadTitles()
                End If
            Case "SAVE"
                Try
                    Dim xRead As IO.StreamReader
                    Dim words As New Dictionary(Of String, String)()
                    xRead = File.OpenText(DBGameIDPath)
                    Do Until xRead.EndOfStream
                        Dim strArr() As String = xRead.ReadLine.Split(""",""")
                        If strArr(1) = txtTitleID.Text Then
                            MsgBox("Game ID: " & txtTitleID.Text & " is already exist!")
                            xRead.Close()
                            Exit Sub
                        End If
                    Loop
                    xRead.Close()

                Catch ex As Exception

                End Try


                Dim append As Boolean = False
                If (System.IO.File.Exists(DBGameIDPath)) Then
                    append = True
                End If
                Using xWrite As System.IO.StreamWriter = New System.IO.StreamWriter(DBGameIDPath, append)
                    xWrite.WriteLine("{""" & txtTitleID.Text & """, """ & cmbTitles.Text & """}")
                End Using
                MsgBox("Saved!", vbInformation)
                LoadTitles()
                btnNew.Text = "NEW"
                btnDelete.Text = "DELETE"
        End Select

    End Sub

    Private Sub btnBrowse_Click(sender As Object, e As EventArgs) Handles btnBrowse.Click
        If FolderBrowserDialog1.ShowDialog() = DialogResult.OK Then
            txtExefsPath.Text = FolderBrowserDialog1.SelectedPath
        End If
    End Sub

    Private Sub Button1_Click_3(sender As Object, e As EventArgs) Handles btnContextMenu.Click
        If (isElevated) Then
            Select Case btnContextMenu.Text
                Case "Add to Context Menu"
                    My.Computer.Registry.ClassesRoot.CreateSubKey("Folder\shell\Patch Game ID\command")
                    My.Computer.Registry.SetValue("HKEY_CLASSES_ROOT\Folder\shell\Patch Game ID", "Icon", Application.ExecutablePath)
                    My.Computer.Registry.SetValue("HKEY_CLASSES_ROOT\Folder\shell\Patch Game ID\command", "", """" & Application.ExecutablePath & """ ""%1""")
                    MsgBox("Successfully added!", vbInformation)
                    btnContextMenu.Text = "Remove from Context Menu"
                Case "Remove from Context Menu"
                    My.Computer.Registry.ClassesRoot.DeleteSubKey("Folder\shell\Patch Game ID")
                    MsgBox("Successfully removed!", vbInformation)
                    btnContextMenu.Text = "Add to Context Menu"
            End Select
        Else
            If MsgBox("Program requires Administrator privileges to continue, click OK to restart the program with administrator privileges", vbInformation + vbOKCancel, "Notice") = vbOK Then RestartElevated()
        End If
    End Sub
    Public Sub RestartElevated()
        Dim startInfo As ProcessStartInfo = New ProcessStartInfo()
        startInfo.UseShellExecute = True
        startInfo.WorkingDirectory = Environment.CurrentDirectory
        startInfo.FileName = Application.ExecutablePath
        startInfo.Verb = "runas"
        Try
            Dim p As Process = Process.Start(startInfo)
        Catch ex As Exception
            Return 'If cancelled, do nothing
        End Try
        Application.Exit()
    End Sub

End Class