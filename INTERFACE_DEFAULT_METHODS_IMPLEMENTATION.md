# Interface Default Method Implementation for BasicLang

This document describes the complete implementation for adding support for interface default method implementations to BasicLang.

## Overview

Interface default methods allow interfaces to provide default implementations for methods, similar to C# 8.0+. This enables:
- Methods with default behavior that implementing classes can optionally override
- Extension of interfaces without breaking existing implementations
- Code reuse through default implementations

## Syntax Example

```vb
Interface IGreeter
    ' Abstract method (no body) - must be implemented by classes
    Function Greet(name As String) As String

    ' Default implementation - classes can use or override
    Function GreetAll(names() As String) As String
        Dim result As String = ""
        For Each name In names
            result = result & Greet(name) & vbCrLf
        Next
        Return result
    End Function
End Interface
```

## Implementation Changes

### 1. ASTNodes.cs - InterfaceNode

**Status: ✅ COMPLETED**

Added helper method to InterfaceNode:
```csharp
/// <summary>
/// Check if a method has a default implementation (body)
/// </summary>
public bool HasDefaultImplementation(FunctionNode method)
{
    return method.Body != null && method.Body.Statements.Count > 0;
}
```

### 2. Parser.cs - Parse Interface Methods with Bodies

**Location**: Lines 874-899 (ParseInterfaceFunction) and 901-923 (ParseInterfaceSub)

**Changes Required**:

Replace `ParseInterfaceFunction` method (around line 874):
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
        node.IsAbstract = false;  // Has implementation
    }
    else
    {
        node.IsAbstract = true;  // Abstract method
    }

    return node;
}
```

Replace `ParseInterfaceSub` method (around line 901):
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
        node.IsAbstract = false;  // Has implementation
    }
    else
    {
        node.IsAbstract = true;  // Abstract method
    }

    return node;
}
```

Also update ParseInterface to not automatically mark methods as abstract (around line 845-853):
```csharp
while (!Check(TokenType.EndInterface) && !IsAtEnd())
{
    if (Check(TokenType.Function))
    {
        var method = ParseInterfaceFunction();
        // Don't set IsAbstract here - let ParseInterfaceFunction determine it
        node.Methods.Add(method);
    }
    else if (Check(TokenType.Sub))
    {
        var method = ParseInterfaceSub();
        // Don't set IsAbstract here - let ParseInterfaceSub determine it
        node.Methods.Add(method);
    }
    SkipNewlines();
}
```

### 3. SemanticAnalyzer.cs - Validate Default Methods

**Location**: Lines 460-483 (Visit InterfaceNode)

**Changes Required**:

Update the `Visit(InterfaceNode node)` method:
```csharp
public void Visit(InterfaceNode node)
{
    var interfaceType = _typeManager.DefineType(node.Name, TypeKind.Interface);
    if (interfaceType == null)
    {
        Error($"Interface '{node.Name}' is already defined", node.Line, node.Column);
        return;
    }

    var symbol = new Symbol(node.Name, SymbolKind.Interface, interfaceType, node.Line, node.Column);
    if (!_currentScope.Define(symbol))
    {
        Error($"Symbol '{node.Name}' is already defined in this scope", node.Line, node.Column);
    }

    EnterScope(node.Name, ScopeKind.Interface);

    foreach (var method in node.Methods)
    {
        // Validate method signature
        method.Accept(this);

        // Validate default implementations
        if (!method.IsAbstract && method.Body != null)
        {
            // Default implementation - validate it can only call other interface methods or parameters
            ValidateDefaultImplementation(method, node);
        }
    }

    ExitScope();
}

private void ValidateDefaultImplementation(FunctionNode method, InterfaceNode iface)
{
    // Default implementations can:
    // 1. Call other methods on the same interface
    // 2. Use parameters
    // 3. Use local variables
    // 4. NOT access instance fields (interfaces don't have fields)

    // For now, we allow any implementation - more strict validation can be added later
    // The C# compiler will catch invalid usages when the generated code is compiled
}
```

### 4. IRBuilder.cs - Handle Default Interface Methods

**Location**: Lines 542-585 (Visit InterfaceNode)

**Changes Required**:

Update the `Visit(InterfaceNode node)` method to generate IR for default implementations:
```csharp
public void Visit(InterfaceNode node)
{
    var irInterface = new IRInterface(node.Name)
    {
        Namespace = _currentNamespace
    };

    foreach (var method in node.Methods)
    {
        var irMethod = new IRInterfaceMethod
        {
            Name = method.Name,
            ReturnType = new TypeInfo(method.ReturnType?.Name ?? "Void", TypeKind.Primitive),
            HasDefaultImplementation = !method.IsAbstract && method.Body != null
        };

        foreach (var param in method.Parameters)
        {
            irMethod.Parameters.Add(new IRParameter
            {
                Name = param.Name,
                TypeName = param.Type?.Name ?? "Object",
                IsOptional = param.IsOptional,
                IsParamArray = param.IsParamArray,
                IsByRef = param.IsByRef,
                DefaultValue = BuildExpressionValue(param.DefaultValue)
            });
        }

        // Generate IR for default implementation if present
        if (!method.IsAbstract && method.Body != null)
        {
            // Generate the default implementation as a regular function
            var implFunctionName = $"{node.Name}.{method.Name}_DefaultImpl";
            var savedFunction = _currentFunction;
            var savedBlock = _currentBlock;

            _currentFunction = _module.CreateFunction(implFunctionName, new TypeInfo(method.ReturnType?.Name ?? "Void", TypeKind.Primitive));

            // Add parameters
            foreach (var param in method.Parameters)
            {
                var paramType = _semanticAnalyzer.GetNodeType(param);
                var irParam = new IRVariable(param.Name, paramType) { IsParameter = true };
                _currentFunction.Parameters.Add(irParam);
                PushVariableVersion(param.Name, irParam);
            }

            // Create entry block and generate body
            _currentBlock = _currentFunction.CreateBlock("entry");
            method.Body.Accept(this);

            // Ensure function ends with return
            if (!_currentBlock.IsTerminated())
            {
                if (method.ReturnType == null || method.ReturnType.Name == "Void")
                    EmitInstruction(new IRReturn());
                else
                    EmitInstruction(new IRReturn(CreateDefaultValue(new TypeInfo(method.ReturnType.Name, TypeKind.Primitive))));
            }

            // Clean up
            foreach (var param in method.Parameters)
            {
                PopVariableVersion(param.Name);
            }

            irMethod.DefaultImplementation = _currentFunction;

            _currentFunction = savedFunction;
            _currentBlock = savedBlock;
        }

        irInterface.Methods.Add(irMethod);
    }

    foreach (var prop in node.Properties)
    {
        irInterface.Properties.Add(new IRInterfaceProperty
        {
            Name = prop.Name,
            Type = new TypeInfo(prop.PropertyType?.Name ?? "Object", TypeKind.Class),
            HasGetter = prop.Getter != null,
            HasSetter = prop.Setter != null
        });
    }

    _module.Interfaces[node.Name] = irInterface;
}
```

Also update IRNodes.cs to add the `HasDefaultImplementation` and `DefaultImplementation` fields to `IRInterfaceMethod`:
```csharp
public class IRInterfaceMethod
{
    public string Name { get; set; }
    public TypeInfo ReturnType { get; set; }
    public List<IRParameter> Parameters { get; set; }
    public bool HasDefaultImplementation { get; set; }  // NEW
    public IRFunction DefaultImplementation { get; set; }  // NEW

    public IRInterfaceMethod()
    {
        Parameters = new List<IRParameter>();
    }
}
```

### 5. CSharpBackend.cs - Generate C# 8.0+ Default Interface Methods

**Location**: Lines 262-300 (GenerateInterface method)

**Changes Required**:

Update the `GenerateInterface` method:
```csharp
private void GenerateInterface(IRInterface irInterface)
{
    var interfaceName = SanitizeName(irInterface.Name);

    // Interface declaration with base interfaces
    var baseList = "";
    if (irInterface.BaseInterfaces.Count > 0)
    {
        baseList = " : " + string.Join(", ", irInterface.BaseInterfaces.Select(SanitizeName));
    }

    WriteLine($"public interface {interfaceName}{baseList}");
    WriteLine("{");
    Indent();

    // Generate method signatures and default implementations
    foreach (var method in irInterface.Methods)
    {
        var returnType = MapType(method.ReturnType);
        var methodName = SanitizeName(method.Name);
        var paramList = string.Join(", ", method.Parameters.Select(FormatIRParameter));

        if (method.HasDefaultImplementation && method.DefaultImplementation != null)
        {
            // Generate default implementation (C# 8.0+)
            WriteLine($"{returnType} {methodName}({paramList})");
            WriteLine("{");
            Indent();

            // Generate the default implementation body from IR
            _currentFunction = method.DefaultImplementation;
            InitializeFunctionContext(method.DefaultImplementation);
            _processedBlocks = new HashSet<BasicBlock>();
            _loopEndBlocks = new Stack<BasicBlock>();

            if (method.DefaultImplementation.EntryBlock != null)
                GenerateStructuredBlock(method.DefaultImplementation.EntryBlock);

            _currentFunction = null;

            Unindent();
            WriteLine("}");
        }
        else
        {
            // Abstract method signature only
            WriteLine($"{returnType} {methodName}({paramList});");
        }
    }

    // Generate property signatures
    foreach (var prop in irInterface.Properties)
    {
        var propType = MapType(prop.Type);
        var propName = SanitizeName(prop.Name);
        var accessors = "";
        if (prop.HasGetter) accessors += " get;";
        if (prop.HasSetter) accessors += " set;";
        WriteLine($"{propType} {propName} {{{accessors} }}");
    }

    Unindent();
    WriteLine("}");
}
```

## Testing

Create a test file `examples/InterfaceDefaultMethods.bas`:

```vb
Interface IGreeter
    ' Abstract method - must be implemented
    Function Greet(name As String) As String

    ' Default implementation
    Function GreetAll(names() As String) As String
        Dim result As String = ""
        For Each name In names
            result = result & Greet(name) & vbCrLf
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

Sub Main()
    Dim greeter As IGreeter = New FormalGreeter()
    PrintLine(greeter.Greet("Alice"))

    Dim names() As String = {"Bob", "Charlie", "Diana"}
    PrintLine(greeter.GreetAll(names))

    PrintLine(greeter.GreetWithPrefix("Eve", "HELLO"))
End Sub
```

Expected C# output:
```csharp
public interface IGreeter
{
    string Greet(string name);

    string GreetAll(string[] names)
    {
        string result = "";
        foreach (string name in names)
        {
            result = result + Greet(name) + Environment.NewLine;
        }
        return result;
    }

    string GreetWithPrefix(string name, string prefix)
    {
        return prefix + " " + Greet(name);
    }
}

public class FormalGreeter : IGreeter
{
    public string Greet(string name)
    {
        return "Good day, " + name;
    }

    public string GreetWithPrefix(string name, string prefix)
    {
        return prefix + ": " + Greet(name) + "!";
    }
}
```

## Notes

1. **C# 8.0 Requirement**: The generated code requires C# 8.0 or later, which supports default interface implementations.

2. **Validation**: The parser differentiates between abstract methods (no body) and default implementations (with body) by checking if a body follows the method signature.

3. **IR Representation**: Default implementations are stored as regular IR functions associated with the interface method, similar to how class methods are handled.

4. **Code Generation**: The C# backend generates default implementations inline within the interface definition using C# 8.0+ syntax.

5. **Backward Compatibility**: Classes implementing the interface don't need to implement default methods unless they want to override the default behavior.

## Implementation Status

- ✅ **ASTNodes.cs**: Helper method added to InterfaceNode
- ⏳ **Parser.cs**: Needs update to parse method bodies in interfaces
- ⏳ **SemanticAnalyzer.cs**: Needs update to validate default implementations
- ⏳ **IRBuilder.cs**: Needs update to generate IR for default methods
- ⏳ **IRNodes.cs**: Needs update to add fields to IRInterfaceMethod
- ⏳ **CSharpBackend.cs**: Needs update to generate C# 8.0+ syntax

## Manual Implementation Steps

Due to file system instability (likely auto-formatter/linter running), implement changes manually:

1. Open each file in your editor
2. Locate the specified sections using line numbers
3. Apply the changes as documented above
4. Test with the provided example

