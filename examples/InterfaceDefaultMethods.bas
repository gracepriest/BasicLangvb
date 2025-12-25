' Interface Default Methods Example
' Demonstrates C# 8.0+ default interface method implementation feature in BasicLang

Interface IGreeter
    ' Abstract method - must be implemented by classes
    Function Greet(name As String) As String

    ' Default implementation - classes can use or override
    Function GreetAll(names() As String) As String
        Dim result As String = ""
        Dim i As Integer
        For i = 0 To UBound(names)
            result = result & Greet(names(i)) & vbCrLf
        Next
        Return result
    End Function

    ' Another default implementation
    Function GreetWithPrefix(name As String, prefix As String) As String
        Return prefix & " " & Greet(name)
    End Function
End Interface

Class FormalGreeter
    Implements IGreeter

    ' Must implement abstract method
    Function Greet(name As String) As String Implements IGreeter.Greet
        Return "Good day, " & name
    End Function

    ' Can override default implementation
    Function GreetWithPrefix(name As String, prefix As String) As String Implements IGreeter.GreetWithPrefix
        Return prefix & ": " & Greet(name) & "!"
    End Function

    ' GreetAll is inherited from interface default implementation
End Class

Class CasualGreeter
    Implements IGreeter

    ' Must implement abstract method
    Function Greet(name As String) As String Implements IGreeter.Greet
        Return "Hey, " & name & "!"
    End Function

    ' Uses default implementations for GreetAll and GreetWithPrefix
End Class

Sub Main()
    PrintLine("=== Formal Greeter ===")
    Dim formal As IGreeter = New FormalGreeter()
    PrintLine(formal.Greet("Alice"))
    PrintLine(formal.GreetWithPrefix("Bob", "HELLO"))

    Dim names(2) As String
    names(0) = "Charlie"
    names(1) = "Diana"
    names(2) = "Eve"
    PrintLine(formal.GreetAll(names))

    PrintLine("")
    PrintLine("=== Casual Greeter ===")
    Dim casual As IGreeter = New CasualGreeter()
    PrintLine(casual.Greet("Frank"))
    PrintLine(casual.GreetWithPrefix("Grace", "Hi"))
    PrintLine(casual.GreetAll(names))
End Sub
