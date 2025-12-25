' Example to demonstrate Call Hierarchy functionality
' This shows how functions call each other

Function Main() As Integer
    PrintLine("Starting program...")

    Dim result As Integer
    result = Calculate(10, 5)

    PrintLine("Result: " + CStr(result))

    ProcessData(result)

    Return 0
End Function

Function Calculate(x As Integer, y As Integer) As Integer
    Dim sum As Integer
    sum = Add(x, y)

    Dim product As Integer
    product = Multiply(x, y)

    Return sum + product
End Function

Function Add(a As Integer, b As Integer) As Integer
    Return a + b
End Function

Function Multiply(a As Integer, b As Integer) As Integer
    Return a * b
End Function

Sub ProcessData(value As Integer)
    If value > 50 Then
        PrintLine("Value is high")
        LogHighValue(value)
    Else
        PrintLine("Value is low")
        LogLowValue(value)
    End If
End Sub

Sub LogHighValue(val As Integer)
    PrintLine("Logging high value: " + CStr(val))
End Sub

Sub LogLowValue(val As Integer)
    PrintLine("Logging low value: " + CStr(val))
End Sub

' Call Hierarchy for 'Calculate':
'
' Incoming Calls (Who calls Calculate?):
'   - Main() - calls Calculate at line 8
'
' Outgoing Calls (What does Calculate call?):
'   - Add() - called at line 18
'   - Multiply() - called at line 21
'
' Call Hierarchy for 'ProcessData':
'
' Incoming Calls:
'   - Main() - calls ProcessData at line 12
'
' Outgoing Calls:
'   - PrintLine() - called at lines 38, 40
'   - LogHighValue() - called at line 39
'   - LogLowValue() - called at line 41
