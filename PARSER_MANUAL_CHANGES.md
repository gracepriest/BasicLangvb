# Manual Changes Required for Parser.cs

Due to file locking/formatting issues, please manually apply these changes to `BasicLang/Parser.cs`:

## Change 1: Remove automatic IsAbstract assignment in ParseInterface

**Location**: Around line 845-853

**Find:**
```csharp
while (!Check(TokenType.EndInterface) && !IsAtEnd())
{
    if (Check(TokenType.Function))
    {
        var method = ParseInterfaceFunction();
        method.IsAbstract = true;  // <-- REMOVE THIS LINE
        node.Methods.Add(method);
    }
    else if (Check(TokenType.Sub))
    {
        var method = ParseInterfaceSub();
        method.IsAbstract = true;  // <-- REMOVE THIS LINE
        node.Methods.Add(method);
    }
    SkipNewlines();
}
```

**Replace with:**
```csharp
while (!Check(TokenType.EndInterface) && !IsAtEnd())
{
    if (Check(TokenType.Function))
    {
        var method = ParseInterfaceFunction();
        // Don't set IsAbstract - let ParseInterfaceFunction determine it
        node.Methods.Add(method);
    }
    else if (Check(TokenType.Sub))
    {
        var method = ParseInterfaceSub();
        // Don't set IsAbstract - let ParseInterfaceSub determine it
        node.Methods.Add(method);
    }
    SkipNewlines();
}
```

## Change 2: Update ParseInterfaceFunction to parse bodies

**Location**: Around line 874-899

**Find:**
```csharp
private FunctionNode ParseInterfaceFunction()
{
    var token = Consume(TokenType.Function, "Expected 'Function'");
    var node = new FunctionNode(token.Line, token.Column);

    node.Name = Consume(TokenType.Identifier, "Expected function name").Lexeme;

    Consume(TokenType.LeftParen, "Expected '(' after function name");
    if (!Check(TokenType.RightParen))
    {
        do
        {
            node.Parameters.Add(ParseParameter());
        } while (Match(TokenType.Comma));
    }
    Consume(TokenType.RightParen, "Expected ')' after parameters");

    if (Match(TokenType.As))
    {
        node.ReturnType = ParseTypeReference();
    }

    // Interface methods have no body
    ConsumeNewlines();
    return node;
}
```

**Replace with:**
```csharp
private FunctionNode ParseInterfaceFunction()
{
    var token = Consume(TokenType.Function, "Expected 'Function'");
    var node = new FunctionNode(token.Line, token.Column);

    node.Name = Consume(TokenType.Identifier, "Expected function name").Lexeme;

    Consume(TokenType.LeftParen, "Expected '(' after function name");
    if (!Check(TokenType.RightParen))
    {
        do
        {
            node.Parameters.Add(ParseParameter());
        } while (Match(TokenType.Comma));
    }
    Consume(TokenType.RightParen, "Expected ')' after parameters");

    if (Match(TokenType.As))
    {
        node.ReturnType = ParseTypeReference();
    }

    ConsumeNewlines();

    // Check if this method has a default implementation (body)
    if (!Check(TokenType.EndInterface) && !Check(TokenType.Function) && !Check(TokenType.Sub) && !IsAtEnd())
    {
        // Parse default implementation
        node.Body = ParseBlock(new[] { TokenType.EndFunction, TokenType.EndInterface, TokenType.Function, TokenType.Sub });

        if (Check(TokenType.EndFunction))
        {
            Consume(TokenType.EndFunction, "Expected 'End Function'");
            ConsumeNewlines();
        }
        node.IsAbstract = false;
    }
    else
    {
        node.IsAbstract = true;
    }

    return node;
}
```

## Change 3: Update ParseInterfaceSub to parse bodies

**Location**: Around line 901-923

**Find:**
```csharp
private FunctionNode ParseInterfaceSub()
{
    var token = Consume(TokenType.Sub, "Expected 'Sub'");
    var node = new FunctionNode(token.Line, token.Column);

    node.Name = Consume(TokenType.Identifier, "Expected sub name").Lexeme;

    Consume(TokenType.LeftParen, "Expected '(' after sub name");
    if (!Check(TokenType.RightParen))
    {
        do
        {
            node.Parameters.Add(ParseParameter());
        } while (Match(TokenType.Comma));
    }
    Consume(TokenType.RightParen, "Expected ')' after parameters");

    node.ReturnType = new TypeReference("Void");

    // Interface methods have no body
    ConsumeNewlines();
    return node;
}
```

**Replace with:**
```csharp
private FunctionNode ParseInterfaceSub()
{
    var token = Consume(TokenType.Sub, "Expected 'Sub'");
    var node = new FunctionNode(token.Line, token.Column);

    node.Name = Consume(TokenType.Identifier, "Expected sub name").Lexeme;

    Consume(TokenType.LeftParen, "Expected '(' after sub name");
    if (!Check(TokenType.RightParen))
    {
        do
        {
            node.Parameters.Add(ParseParameter());
        } while (Match(TokenType.Comma));
    }
    Consume(TokenType.RightParen, "Expected ')' after parameters");

    node.ReturnType = new TypeReference("Void");

    ConsumeNewlines();

    // Check if this method has a default implementation (body)
    if (!Check(TokenType.EndInterface) && !Check(TokenType.Function) && !Check(TokenType.Sub) && !IsAtEnd())
    {
        // Parse default implementation
        node.Body = ParseBlock(new[] { TokenType.EndSub, TokenType.EndInterface, TokenType.Function, TokenType.Sub });

        if (Check(TokenType.EndSub))
        {
            Consume(TokenType.EndSub, "Expected 'End Sub'");
            ConsumeNewlines();
        }
        node.IsAbstract = false;
    }
    else
    {
        node.IsAbstract = true;
    }

    return node;
}
```

## How to Apply

1. Open `BasicLang/Parser.cs` in your editor
2. Search for each section using the "Find" text
3. Replace with the corresponding "Replace with" text
4. Save the file

Alternatively, apply the patch file `interface_default_methods.patch` using:
```
git apply interface_default_methods.patch
```

Note: If the line numbers don't match exactly, search for the code patterns - the logic should be the same even if line numbers have shifted slightly.
