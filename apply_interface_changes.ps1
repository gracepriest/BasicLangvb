# PowerShell script to apply interface default method implementation changes
# This script modifies the necessary files to add support for interface default methods

Write-Host "Applying Interface Default Method Implementation Changes..." -ForegroundColor Green

# Change 1: Parser.cs - Update ParseInterface to not automatically set IsAbstract
Write-Host "`nUpdating Parser.cs - ParseInterface method..." -ForegroundColor Yellow
$parserPath = "BasicLang\Parser.cs"
$parserContent = Get-Content $parserPath -Raw

# Remove the automatic IsAbstract = true assignments in ParseInterface
$parserContent = $parserContent -replace '(\s+var method = ParseInterfaceFunction\(\);)\s+method\.IsAbstract = true;', '$1'
$parserContent = $parserContent -replace '(\s+var method = ParseInterfaceSub\(\);)\s+method\.IsAbstract = true;', '$1'

Set-Content $parserPath $parserContent -NoNewline

# Change 2: Parser.cs - Update ParseInterfaceFunction
Write-Host "Updating Parser.cs - ParseInterfaceFunction method..." -ForegroundColor Yellow
$parserContent = Get-Content $parserPath -Raw

$oldFunction = @'
            if (Match(TokenType.As))
            {
                node.ReturnType = ParseTypeReference();
            }

            // Interface methods have no body
            ConsumeNewlines();
            return node;
        }

        private FunctionNode ParseInterfaceSub()
'@

$newFunction = @'
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

        private FunctionNode ParseInterfaceSub()
'@

$parserContent = $parserContent -replace [regex]::Escape($oldFunction), $newFunction
Set-Content $parserPath $parserContent -NoNewline

# Change 3: Parser.cs - Update ParseInterfaceSub
Write-Host "Updating Parser.cs - ParseInterfaceSub method..." -ForegroundColor Yellow
$parserContent = Get-Content $parserPath -Raw

$oldSub = @'
            node.ReturnType = new TypeReference("Void");

            // Interface methods have no body
            ConsumeNewlines();
            return node;
        }
'@

$newSub = @'
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
'@

$parserContent = $parserContent -replace [regex]::Escape($oldSub), $newSub
Set-Content $parserPath $parserContent -NoNewline

Write-Host "`nParser.cs updated successfully!" -ForegroundColor Green

# Change 4: IRNodes.cs - Add fields to IRInterfaceMethod
Write-Host "`nUpdating IRNodes.cs - IRInterfaceMethod class..." -ForegroundColor Yellow
$irNodesPath = "BasicLang\IRNodes.cs"
$irNodesContent = Get-Content $irNodesPath -Raw

$oldIRMethod = @'
    public class IRInterfaceMethod
    {
        public string Name { get; set; }
        public TypeInfo ReturnType { get; set; }
        public List<IRParameter> Parameters { get; set; }

        public IRInterfaceMethod()
        {
            Parameters = new List<IRParameter>();
        }
    }
'@

$newIRMethod = @'
    public class IRInterfaceMethod
    {
        public string Name { get; set; }
        public TypeInfo ReturnType { get; set; }
        public List<IRParameter> Parameters { get; set; }
        public bool HasDefaultImplementation { get; set; }
        public IRFunction DefaultImplementation { get; set; }

        public IRInterfaceMethod()
        {
            Parameters = new List<IRParameter>();
        }
    }
'@

$irNodesContent = $irNodesContent -replace [regex]::Escape($oldIRMethod), $newIRMethod
Set-Content $irNodesPath $irNodesContent -NoNewline

Write-Host "IRNodes.cs updated successfully!" -ForegroundColor Green

Write-Host "`nAll changes applied successfully!" -ForegroundColor Green
Write-Host "`nPlease manually update:" -ForegroundColor Yellow
Write-Host "  - SemanticAnalyzer.cs: Add validation for default implementations" -ForegroundColor Yellow
Write-Host "  - IRBuilder.cs: Generate IR for default interface methods" -ForegroundColor Yellow
Write-Host "  - CSharpBackend.cs: Generate C# 8.0+ default interface method syntax" -ForegroundColor Yellow
Write-Host "`nSee INTERFACE_DEFAULT_METHODS_IMPLEMENTATION.md for details" -ForegroundColor Yellow
