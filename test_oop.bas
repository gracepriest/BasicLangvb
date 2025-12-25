Class Person
    Private _name As String
    Private _age As Integer

    Sub New(name As String)
        _name = name
        _age = 0
    End Sub

    Public Virtual Function Greet() As String
        Return "Hello, " & _name
    End Function

    Public Sub SetAge(age As Integer)
        _age = age
    End Sub

    Public Function GetAge() As Integer
        Return _age
    End Function
End Class

Class Employee
    Inherits Person

    Private _employeeId As Integer

    Sub New(name As String, employeeId As Integer)
        MyBase.New(name)
        _employeeId = employeeId
    End Sub

    Public Overrides Function Greet() As String
        Return "Hi, I'm employee #" & _employeeId
    End Function
End Class

Sub Main()
    Dim p As Person = New Person("John")
    p.SetAge(30)
    PrintLine(p.Greet())
    PrintLine("Age: " & p.GetAge())

    Dim e As Employee = New Employee("Jane", 12345)
    e.SetAge(25)
    PrintLine(e.Greet())
    PrintLine("Age: " & e.GetAge())
End Sub
