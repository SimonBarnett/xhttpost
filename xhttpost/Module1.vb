Imports System.IO
Imports System.Security.AccessControl

Module Module1

    '-x c:\eve.xml -u http://dev.emerge-it.co.uk:8080/posthandler.ashx

    Private arg As clArg

    Sub Main(ByVal args As String())

        arg = New clArg(args)

        Try
            With arg.Keys
                If .Contains("?") Or .Count = 0 Then
                    Help()
                Else
                    If Not .Contains("u") Then _
                        Throw New Exception("Missing URL.")

                    If (.Contains("x") And .Contains("f")) Or (Not .Contains("x") And Not .Contains("f")) Then _
                        Throw New Exception("Please specify either -(x)ml file OR -(f)older.")
                    If .Contains("f") Then
                        If Not Directory.Exists(arg("f")) Then _
                            Throw New Exception(String.Format("Invalid folder specified [{0}].", arg("f")))
                    End If
                    If .Contains("x") Then
                        If Not File.Exists(arg("x")) Then _
                            Throw New Exception(String.Format("Invalid File specified [{0}].", arg("f")))
                    End If

                    If .Contains("m") And .Contains("d") Then _
                        Throw New Exception("Please specify either -(m)ove OR -(d)elete.")
                    If .Contains("m") Then
                        If Not Directory.Exists(arg("m")) Then _
                            Throw New Exception("Invalid move folder.")
                    End If

                    If Not .Contains("t") Then
                        arg.Add("t", "*.xml")
                    End If

                    If .Contains("x") Then
                        Using sr As New StreamReader(arg("x"))
                            Log("Sending [{0}] to [{1}]...", arg("x"), arg("u"))
                            Console.WriteLine(Post(arg("u"), sr.ReadToEnd).ToString())
                            sr.Close()

                            If .Contains("m") Then
                                Move(arg("x"), arg("m"))
                            ElseIf .Contains("d") Then
                                Delete(arg("x"))
                            End If
                        End Using

                    ElseIf .Contains("f") Then
                        For Each F As String In Directory.GetFiles(arg("f"), arg("t"))
                            Using sr As New StreamReader(F)
                                Log("Sending [{0}] to [{1}]...", F, arg("u"))
                                Try
                                    Console.WriteLine(Post(arg("u"), sr.ReadToEnd))
                                    sr.Close()
                                    If .Contains("m") Then
                                        Move(F, arg("m"))
                                    ElseIf .Contains("d") Then
                                        Delete(F)
                                    End If

                                Catch ex As Exception
                                    Log(ex.Message)

                                End Try

                            End Using

                        Next

                    End If

                End If

            End With

        Catch EX As Exception
            Log(EX.Message)

        End Try

    End Sub

    Private Sub Help()
        Dim syntax As New Dictionary(Of String, String)
        With syntax
            .Add("-u [Url]", "Url of the handler endpoint")
            .Add("(-x [file]|-f [dir])", "The file or folder to transmit")
            .Add("{(-m [dir]|-d)}", "Optional: Move (to dir) or delete the files after transmission")
            .Add("{-t}", "Optional: File pattern to search -f folder (default .xml)")

        End With
        Console.WriteLine()
        Console.WriteLine("Syntax:")
        Console.WriteLine()
        Console.Write("  {0}.exe ", System.Reflection.Assembly.GetExecutingAssembly.GetName.Name)
        For Each k As String In syntax.Keys
            Console.Write("{0} ", k)
        Next
        Console.WriteLine()
        Console.WriteLine()
        For Each k As String In syntax.Keys
            Console.WriteLine("  {0}", k)
            Console.WriteLine("     {0}", syntax(k))
            Console.WriteLine()
        Next
        Console.WriteLine()
        'Console.ReadKey()

    End Sub

    Private Function Post(ByVal Server As String, ByVal xmldata As String) As String

        Dim requestStream As Stream = Nothing
        Dim uploadResponse As Net.HttpWebResponse = Nothing
        Dim myEncoder As New System.Text.ASCIIEncoding
        Dim bytes As Byte() = myEncoder.GetBytes(xmldata)
        Dim ms As MemoryStream = New MemoryStream(bytes)

        Try

            Dim uploadRequest As Net.HttpWebRequest = CType(
                Net.HttpWebRequest.Create(Server),
                Net.HttpWebRequest
            )
            With uploadRequest
                .Method = Net.WebRequestMethods.Http.Post
                .Proxy = Nothing
                Select Case xmldata.Trim.Substring(0, 1)
                    Case "{"
                        .ContentType = "text/json"

                    Case "<"
                        .ContentType = "text/xml"

                    Case Else
                        Throw New Exception("Invalid data in file.")

                End Select
            End With

            ' Connect
            requestStream = uploadRequest.GetRequestStream()

            ' Upload the XML
            Dim buffer(1024) As Byte
            Dim bytesRead As Integer
            While True
                bytesRead = ms.Read(buffer, 0, buffer.Length)
                If bytesRead = 0 Then
                    Exit While
                End If
                requestStream.Write(buffer, 0, bytesRead)
            End While

            ' The request stream must be closed before getting the response.
            requestStream.Close()

            uploadResponse = uploadRequest.GetResponse()
            Dim reader As New StreamReader(uploadResponse.GetResponseStream)

            Log("Status: {0}", uploadResponse.StatusCode.ToString)

            Return reader.ReadToEnd

        Catch uex As UriFormatException
            Throw New Exception(
                String.Format(
                    "Malformed URL [{0}]. {1}",
                    Server,
                    uex.Message
                )
            )

        Catch uex As Net.WebException
            Throw New Exception(
                String.Format(
                    "Web Error connecting to [{0}]. {1}",
                    Server,
                    uex.Message
                )
            )

        Catch uex As Exception
            Throw uex

        Finally
            If uploadResponse IsNot Nothing Then
                uploadResponse.Close()
            End If
            If requestStream IsNot Nothing Then
                requestStream.Close()
            End If

        End Try

    End Function


#Region "Fso Methods"

    Private Sub Move(ByVal Source As String, ByVal Dest As String)
        Log("Moving from {0} -> {1}.", Source, Dest)
        If File.Exists(
            String.Format("{0}\{1}",
                Dest, Source.Split("\").Last
            )
        ) Then
            Delete(String.Format("{0}\{1}",
                Dest, Source.Split("\").Last
            )
        )
        End If
        File.Move(
            Source, String.Format(
                "{0}\{1}",
                Dest,
                Source.Split("\").Last
            )
        )
    End Sub

    Private Sub Delete(ByVal Filename As String)
        Log("Deleting {0}.", Filename)
        While File.Exists(Filename)
            Try
                File.Delete(Filename)
            Catch
                System.Threading.Thread.Sleep(100)
            End Try
        End While
    End Sub

#End Region

#Region "Logging"

    Private Function LogRoot() As DirectoryInfo
        Return New DirectoryInfo(
            Path.Combine(
                new FileInfo(System.Reflection.Assembly.GetExecutingAssembly.GetName.FullName).Directory.FullName,
                "logs"
            )
        )

    End Function

    Private Function LogFolder() As DirectoryInfo
        Return New DirectoryInfo(
            Path.Combine(
                LogRoot.FullName,
                Now.ToString("yyyy-MM")
            )
        )

    End Function

    Private Function currentlog() As FileInfo
        With LogFolder()
            If Not .Exists Then .Create()
            Return New FileInfo(
                Path.Combine(
                    .FullName,
                    String.Format(
                        "{0}.txt",
                        Now.ToString("yyMMdd")
                    )
                )
            )

        End With

    End Function

    Public Sub Log(ByVal str, ByVal ParamArray args())
        Console.WriteLine(String.Format(str, args))

        Try
            Using log As New StreamWriter(currentlog.FullName, True)
                log.WriteLine("{0}> {1}", Format(Now, "hh:mm:ss"), String.Format(str, args))
            End Using

        Catch ex As Exception
            Console.WriteLine(ex.Message)

        End Try


    End Sub

#End Region

End Module
