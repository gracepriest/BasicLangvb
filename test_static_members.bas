Class Calculator
    ' Static field
    Public Shared Pi As Double = 3.14159

    ' Static function
    Public Shared Function Add(a As Integer, b As Integer) As Integer
        Return a + b
    End Function

    ' Static subroutine
    Public Shared Sub PrintMessage(msg As String)
        PrintLine(msg)
    End Sub

    ' Instance field for comparison
    Public Name As String = "MyCalculator"

    ' Instance method for comparison
    Public Function Square(x As Integer) As Integer
        ' Can use Me in instance method
        PrintLine(Me.Name)
        Return x * x
    End Function
End Class

' Test static members
Sub Main()
    ' Access static members via class name
    Dim sum As Integer = Calculator.Add(10, 20)
    PrintLine("10 + 20 = " & sum)

    ' Access static field
    PrintLine("Pi = " & Calculator.Pi)

    ' Call static subroutine
    Calculator.PrintMessage("Hello from static method!")

    ' Create instance to test instance members
    Dim calc As New Calculator()
    calc.Name = "MyCalc"
    Dim squared As Integer = calc.Square(5)
    PrintLine("5 squared = " & squared)
End Sub
