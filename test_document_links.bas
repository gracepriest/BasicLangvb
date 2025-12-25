' BasicLang Document Link Handler Test
' This file demonstrates the DocumentLinkHandler functionality
' GitHub: https://github.com/basiclang/basiclang
' Documentation: https://basiclang.org/docs

' Import statements - these will be clickable links
Import "stdlib.bas"
Import "utils.bas"

' File path strings - these will be clickable if the files exist
Dim configPath As String = "C:\config\settings.ini"
Dim dataPath As String = ".\data\input.txt"
Dim relativePath As String = "..\shared\common.bas"

Function LoadFile(filename As String) As String
    ' Load file from path
    ' More info at: https://docs.microsoft.com/en-us/dotnet/api/system.io.file
    Return ""
End Function

Sub Main()
    PrintLine("Document Links Demo")

    ' More URLs in comments
    ' Reference: https://stackoverflow.com/questions/1234567
    ' API Docs: https://api.example.com/v1/documentation
End Sub
