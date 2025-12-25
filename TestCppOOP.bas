Class Person
    Private _name As String
    Private _age As Integer

    Public Sub New(name As String, age As Integer)
        _name = name
        _age = age
    End Sub

    Public Function GetName() As String
        Return _name
    End Function

    Public Sub SetName(name As String)
        _name = name
    End Sub

    Public Overridable Function Greet() As String
        Return "Hello, I'm " & _name
    End Function

    Public Shared Function GetSpecies() As String
        Return "Human"
    End Function
End Class

Class Employee
    Inherits Person

    Private _company As String

    Public Sub New(name As String, age As Integer, company As String)
        MyBase.New(name, age)
        _company = company
    End Sub

    Public Overrides Function Greet() As String
        Return MyBase.Greet() & " from " & _company
    End Function

    Public Function GetCompany() As String
        Return _company
    End Function
End Class

Sub Main()
    Dim p As Person = New Person("Alice", 30)
    PrintLine(p.Greet())

    Dim e As Employee = New Employee("Bob", 25, "TechCorp")
    PrintLine(e.Greet())
    PrintLine("Species: " & Person.GetSpecies())
End Sub
