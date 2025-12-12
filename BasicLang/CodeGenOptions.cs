using System;

namespace BasicLang.Compiler.CodeGen
{
    /// <summary>
    /// Configuration options for C# code generation
    /// </summary>
    public class CodeGenOptions
    {
        /// <summary>
        /// Namespace for generated code
        /// </summary>
        public string Namespace { get; set; } = "GeneratedCode";
        
        /// <summary>
        /// Class name for the main generated class
        /// </summary>
        public string ClassName { get; set; } = "Program";
        
        /// <summary>
        /// Whether to generate a Main method
        /// </summary>
        public bool GenerateMainMethod { get; set; } = true;
        
        /// <summary>
        /// Whether to generate comments in the output
        /// </summary>
        public bool GenerateComments { get; set; } = true;
        
        /// <summary>
        /// Access modifier for generated methods (public, private, internal)
        /// </summary>
        public string MethodAccessModifier { get; set; } = "public";
        
        /// <summary>
        /// Access modifier for generated classes (public, private, internal)
        /// </summary>
        public string ClassAccessModifier { get; set; } = "public";
        
        /// <summary>
        /// Number of spaces per indentation level
        /// </summary>
        public int IndentSize { get; set; } = 4;
        
        /// <summary>
        /// Whether to use tabs instead of spaces
        /// </summary>
        public bool UseTabs { get; set; } = false;
        
        /// <summary>
        /// Whether to generate XML documentation comments
        /// </summary>
        public bool GenerateXmlDocs { get; set; } = false;
    }
}
