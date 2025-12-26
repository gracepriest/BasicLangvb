' Test networking - HTTP GET
Sub Main()
    Dim response As String
    PrintLine "Fetching data from httpbin.org..."
    response = HttpGet("https://httpbin.org/get")
    PrintLine "Response:"
    PrintLine response
End Sub
