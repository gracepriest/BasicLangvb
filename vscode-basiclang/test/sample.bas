' Sample BasicLang program to test LSP features

' Test autocomplete for built-in functions
Dim message As String = "Hello, World!"
PrintLine(message)



' Test hover documentation
Dim x As Integer = 10
Dim y As Integer = 20
Dim result As Integer = x + y

PrintLine(CStr(result))

' Test function definition and go-to-definition
Function Add(a As Integer, b As Integer) As Integer
    Return a + b
End Function

' Test subroutine
Sub SayHello(name As String)
    PrintLine("Hello, " & name & "!")
End Sub

' Test class
Class Person
    Private _name As String

    Sub New(name As String)
        _name = name
    End Sub

    Public Function GetName() As String
        Return _name
    End Function
End Class

' Test control flow
If x > 5 Then
    PrintLine("x is greater than 5")
Else
    PrintLine("x is 5 or less")
End If

' Test loop
For i = 1 To 10
    PrintLine(CStr(i))
Next

' Call our function
Dim sum As Integer = Add(5, 3)
PrintLine("Sum: " & CStr(sum))

' Call subroutine
SayHello("BasicLang")

' Test math functions
Dim sqrtVal As Double = Sqrt(16)
PrintLine("Square root of 16: " & CStr(sqrtVal))
