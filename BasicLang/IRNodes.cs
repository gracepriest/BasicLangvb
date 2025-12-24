using System;
using System.Collections.Generic;
using BasicLang.Compiler.SemanticAnalysis;

namespace BasicLang.Compiler.IR
{
    /// <summary>
    /// Base class for all IR instructions
    /// SSA-based three-address code representation
    /// </summary>
    public abstract class IRInstruction
    {
        public int Id { get; set; }
        public BasicBlock ParentBlock { get; set; }
        public TypeInfo Type { get; set; }
        
        protected IRInstruction(TypeInfo type = null)
        {
            Type = type;
        }
        
        public abstract void Accept(IIRVisitor visitor);
        public abstract override string ToString();
    }
    
    /// <summary>
    /// Visitor interface for IR traversal
    /// </summary>
    public interface IIRVisitor
    {
        void Visit(IRFunction function);
        void Visit(BasicBlock block);
        void Visit(IRConstant constant);
        void Visit(IRVariable variable);
        void Visit(IRBinaryOp binaryOp);
        void Visit(IRUnaryOp unaryOp);
        void Visit(IRAssignment assignment);
        void Visit(IRLoad load);
        void Visit(IRStore store);
        void Visit(IRCall call);
        void Visit(IRReturn ret);
        void Visit(IRBranch branch);
        void Visit(IRConditionalBranch condBranch);
        void Visit(IRPhi phi);
        void Visit(IRAlloca alloca);
        void Visit(IRGetElementPtr gep);
        void Visit(IRCast cast);
        void Visit(IRCompare compare);
        void Visit(IRSwitch switchInst);
        void Visit(IRLabel label);
        void Visit(IRComment comment);
        void Visit(IRArrayAlloc arrayAlloc);
        void Visit(IRArrayStore arrayStore);
        void Visit(IRAwait awaitInst);
        void Visit(IRYield yieldInst);
        void Visit(IRNewObject newObj);
        void Visit(IRInstanceMethodCall methodCall);
        void Visit(IRBaseMethodCall baseCall);
        void Visit(IRFieldAccess fieldAccess);
    }
    
    // ============================================================================
    // IR Values (can be used as operands)
    // ============================================================================
    
    /// <summary>
    /// Base class for values that can be used in expressions
    /// </summary>
    public abstract class IRValue : IRInstruction
    {
        public string Name { get; set; }
        
        protected IRValue(string name, TypeInfo type) : base(type)
        {
            Name = name;
        }
    }
    
    /// <summary>
    /// Constant value
    /// </summary>
    public class IRConstant : IRValue
    {
        public object Value { get; set; }
        
        public IRConstant(object value, TypeInfo type) 
            : base($"const_{value}", type)
        {
            Value = value;
        }
        
        public override void Accept(IIRVisitor visitor) => visitor.Visit(this);
        
        public override string ToString() => $"{Value}";
    }
    
    /// <summary>
    /// Variable (SSA register)
    /// </summary>
    public class IRVariable : IRValue
    {
        public int Version { get; set; }
        public bool IsParameter { get; set; }
        public bool IsGlobal { get; set; }
        
        public IRVariable(string name, TypeInfo type, int version = 0) 
            : base(name, type)
        {
            Version = version;
        }
        
        public override void Accept(IIRVisitor visitor) => visitor.Visit(this);
        
        public override string ToString()
        {
            if (IsParameter)
                return $"%{Name}";
            if (IsGlobal)
                return $"@{Name}";
            return Version > 0 ? $"%{Name}.{Version}" : $"%{Name}";
        }
    }
    
    // ============================================================================
    // Arithmetic and Logic Operations
    // ============================================================================
    
    /// <summary>
    /// Binary operation: result = left op right
    /// </summary>
    public class IRBinaryOp : IRValue
    {
        public IRValue Left { get; set; }
        public IRValue Right { get; set; }
        public BinaryOpKind Operation { get; set; }
        
        public IRBinaryOp(string resultName, BinaryOpKind op, IRValue left, IRValue right, TypeInfo type)
            : base(resultName, type)
        {
            Operation = op;
            Left = left;
            Right = right;
        }
        
        public override void Accept(IIRVisitor visitor) => visitor.Visit(this);
        
        public override string ToString() => 
            $"{Name} = {Operation.ToString().ToLower()} {Left}, {Right}";
    }
    
    public enum BinaryOpKind
    {
        // Arithmetic
        Add, Sub, Mul, Div, Mod, IntDiv,
        
        // Bitwise
        And, Or, Xor, Shl, Shr,
        
        // Comparison (returns boolean)
        Eq, Ne, Lt, Le, Gt, Ge,
        
        // String
        Concat
    }
    
    /// <summary>
    /// Unary operation: result = op operand
    /// </summary>
    public class IRUnaryOp : IRValue
    {
        public IRValue Operand { get; set; }
        public UnaryOpKind Operation { get; set; }
        
        public IRUnaryOp(string resultName, UnaryOpKind op, IRValue operand, TypeInfo type)
            : base(resultName, type)
        {
            Operation = op;
            Operand = operand;
        }
        
        public override void Accept(IIRVisitor visitor) => visitor.Visit(this);
        
        public override string ToString() => 
            $"{Name} = {Operation.ToString().ToLower()} {Operand}";
    }
    
    public enum UnaryOpKind
    {
        Neg,        // Arithmetic negation
        Not,        // Logical negation
        BitwiseNot, // Bitwise negation
        Inc,        // Increment
        Dec         // Decrement
    }
    
    /// <summary>
    /// Comparison operation: result = compare left, right
    /// </summary>
    public class IRCompare : IRValue
    {
        public IRValue Left { get; set; }
        public IRValue Right { get; set; }
        public CompareKind Comparison { get; set; }
        
        public IRCompare(string resultName, CompareKind cmp, IRValue left, IRValue right, TypeInfo type)
            : base(resultName, type)
        {
            Comparison = cmp;
            Left = left;
            Right = right;
        }
        
        public override void Accept(IIRVisitor visitor) => visitor.Visit(this);
        
        public override string ToString() => 
            $"{Name} = cmp {Comparison.ToString().ToLower()} {Left}, {Right}";
    }
    
    public enum CompareKind
    {
        Eq,  // Equal
        Ne,  // Not equal
        Lt,  // Less than
        Le,  // Less or equal
        Gt,  // Greater than
        Ge   // Greater or equal
    }
    
    // ============================================================================
    // Memory Operations
    // ============================================================================
    
    /// <summary>
    /// Load value from memory: result = load ptr
    /// </summary>
    public class IRLoad : IRValue
    {
        public IRValue Address { get; set; }
        
        public IRLoad(string resultName, IRValue address, TypeInfo type)
            : base(resultName, type)
        {
            Address = address;
        }
        
        public override void Accept(IIRVisitor visitor) => visitor.Visit(this);
        
        public override string ToString() => 
            $"{Name} = load {Type} {Address}";
    }
    
    /// <summary>
    /// Store value to memory: store value, ptr
    /// </summary>
    public class IRStore : IRInstruction
    {
        public IRValue Value { get; set; }
        public IRValue Address { get; set; }
        
        public IRStore(IRValue value, IRValue address)
        {
            Value = value;
            Address = address;
        }
        
        public override void Accept(IIRVisitor visitor) => visitor.Visit(this);
        
        public override string ToString() => 
            $"store {Value}, {Address}";
    }
    
    /// <summary>
    /// Allocate stack memory: result = alloca type
    /// </summary>
    public class IRAlloca : IRValue
    {
        public int Size { get; set; }
        
        public IRAlloca(string resultName, TypeInfo type, int size = 1)
            : base(resultName, type)
        {
            Size = size;
        }
        
        public override void Accept(IIRVisitor visitor) => visitor.Visit(this);
        
        public override string ToString() => 
            $"{Name} = alloca {Type}" + (Size > 1 ? $", {Size}" : "");
    }
    
    /// <summary>
    /// Get element pointer: result = gep ptr, indices
    /// </summary>
    public class IRGetElementPtr : IRValue
    {
        public IRValue BasePointer { get; set; }
        public List<IRValue> Indices { get; set; }
        
        public IRGetElementPtr(string resultName, IRValue basePtr, TypeInfo type)
            : base(resultName, type)
        {
            BasePointer = basePtr;
            Indices = new List<IRValue>();
        }
        
        public override void Accept(IIRVisitor visitor) => visitor.Visit(this);
        
        public override string ToString() => 
            $"{Name} = gep {BasePointer}, [{string.Join(", ", Indices)}]";
    }
    
    // ============================================================================
    // Control Flow Operations
    // ============================================================================
    
    /// <summary>
    /// Unconditional branch: br label
    /// </summary>
    public class IRBranch : IRInstruction
    {
        public BasicBlock Target { get; set; }
        
        public IRBranch(BasicBlock target)
        {
            Target = target;
        }
        
        public override void Accept(IIRVisitor visitor) => visitor.Visit(this);
        
        public override string ToString() => $"br {Target.Name}";
    }
    
    /// <summary>
    /// Conditional branch: br cond, trueLabel, falseLabel
    /// </summary>
    public class IRConditionalBranch : IRInstruction
    {
        public IRValue Condition { get; set; }
        public BasicBlock TrueTarget { get; set; }
        public BasicBlock FalseTarget { get; set; }
        
        public IRConditionalBranch(IRValue condition, BasicBlock trueTarget, BasicBlock falseTarget)
        {
            Condition = condition;
            TrueTarget = trueTarget;
            FalseTarget = falseTarget;
        }
        
        public override void Accept(IIRVisitor visitor) => visitor.Visit(this);
        
        public override string ToString() => 
            $"br {Condition}, {TrueTarget.Name}, {FalseTarget.Name}";
    }
    
    /// <summary>
    /// Multi-way branch: switch value, default, [case: label, ...]
    /// </summary>
    public class IRSwitch : IRInstruction
    {
        public IRValue Value { get; set; }
        public BasicBlock DefaultTarget { get; set; }
        public List<(IRValue CaseValue, BasicBlock Target)> Cases { get; set; }
        
        public IRSwitch(IRValue value, BasicBlock defaultTarget)
        {
            Value = value;
            DefaultTarget = defaultTarget;
            Cases = new List<(IRValue, BasicBlock)>();
        }
        
        public override void Accept(IIRVisitor visitor) => visitor.Visit(this);
        
        public override string ToString() => 
            $"switch {Value}, {DefaultTarget.Name}, [{Cases.Count} cases]";
    }
    
    /// <summary>
    /// Return from function: ret value
    /// </summary>
    public class IRReturn : IRInstruction
    {
        public IRValue Value { get; set; }
        
        public IRReturn(IRValue value = null)
        {
            Value = value;
        }
        
        public override void Accept(IIRVisitor visitor) => visitor.Visit(this);
        
        public override string ToString() => 
            Value != null ? $"ret {Value}" : "ret void";
    }
    
    // ============================================================================
    // Function Operations
    // ============================================================================
    
    /// <summary>
    /// Function call: result = call function(args)
    /// </summary>
    public class IRCall : IRValue
    {
        public string FunctionName { get; set; }
        public List<IRValue> Arguments { get; set; }
        public bool IsTailCall { get; set; }
        
        public IRCall(string resultName, string functionName, TypeInfo returnType)
            : base(resultName, returnType)
        {
            FunctionName = functionName;
            Arguments = new List<IRValue>();
        }
        
        public override void Accept(IIRVisitor visitor) => visitor.Visit(this);
        
        public override string ToString()
        {
            var args = string.Join(", ", Arguments);
            var tail = IsTailCall ? "tail " : "";
            return Type != null && Type.Name != "Void"
                ? $"{Name} = {tail}call {FunctionName}({args})"
                : $"{tail}call {FunctionName}({args})";
        }
    }
    
    // ============================================================================
    // SSA Operations
    // ============================================================================
    
    /// <summary>
    /// Phi node: result = phi [value1, block1], [value2, block2], ...
    /// Used for SSA form to merge values from different control flow paths
    /// </summary>
    public class IRPhi : IRValue
    {
        public List<(IRValue Value, BasicBlock Block)> Operands { get; set; }
        
        public IRPhi(string resultName, TypeInfo type)
            : base(resultName, type)
        {
            Operands = new List<(IRValue, BasicBlock)>();
        }
        
        public override void Accept(IIRVisitor visitor) => visitor.Visit(this);
        
        public override string ToString()
        {
            var operands = string.Join(", ", 
                Operands.ConvertAll(op => $"[{op.Value}, {op.Block.Name}]"));
            return $"{Name} = phi {Type} {operands}";
        }
    }
    
    // ============================================================================
    // Type Operations
    // ============================================================================
    
    /// <summary>
    /// Type cast: result = cast value to type
    /// </summary>
    public class IRCast : IRValue
    {
        public IRValue Value { get; set; }
        public TypeInfo SourceType { get; set; }
        public CastKind Kind { get; set; }
        
        public IRCast(string resultName, IRValue value, TypeInfo sourceType, TypeInfo targetType, CastKind kind)
            : base(resultName, targetType)
        {
            Value = value;
            SourceType = sourceType;
            Kind = kind;
        }
        
        public override void Accept(IIRVisitor visitor) => visitor.Visit(this);
        
        public override string ToString() => 
            $"{Name} = {Kind.ToString().ToLower()} {Value} to {Type}";
    }
    
    public enum CastKind
    {
        Bitcast,    // No-op cast (pointer types)
        Trunc,      // Truncate to smaller type
        ZExt,       // Zero extend to larger type
        SExt,       // Sign extend to larger type
        FPTrunc,    // Float truncate
        FPExt,      // Float extend
        FPToUI,     // Float to unsigned int
        FPToSI,     // Float to signed int
        UIToFP,     // Unsigned int to float
        SIToFP,     // Signed int to float
        PtrToInt,   // Pointer to integer
        IntToPtr    // Integer to pointer
    }
    
    // ============================================================================
    // Misc Operations
    // ============================================================================
    
    /// <summary>
    /// Assignment (for non-SSA variables): var = value
    /// </summary>
    public class IRAssignment : IRInstruction
    {
        public IRVariable Target { get; set; }
        public IRValue Value { get; set; }
        
        public IRAssignment(IRVariable target, IRValue value)
        {
            Target = target;
            Value = value;
        }
        
        public override void Accept(IIRVisitor visitor) => visitor.Visit(this);
        
        public override string ToString() => $"{Target} = {Value}";
    }
    
    /// <summary>
    /// Label (for legacy support)
    /// </summary>
    public class IRLabel : IRInstruction
    {
        public string Name { get; set; }
        
        public IRLabel(string name)
        {
            Name = name;
        }
        
        public override void Accept(IIRVisitor visitor) => visitor.Visit(this);
        
        public override string ToString() => $"{Name}:";
    }
    
    /// <summary>
    /// Comment for debugging
    /// </summary>
    public class IRComment : IRInstruction
    {
        public string Text { get; set; }
        
        public IRComment(string text)
        {
            Text = text;
        }
        
        public override void Accept(IIRVisitor visitor) => visitor.Visit(this);

        public override string ToString() => $"; {Text}";
    }

    /// <summary>
    /// Array allocation - allocates an array of a given size
    /// </summary>
    public class IRArrayAlloc : IRValue
    {
        public TypeInfo ElementType { get; set; }
        public int Size { get; set; }

        public IRArrayAlloc(string name, TypeInfo elementType, int size)
            : base(name, new TypeInfo($"{elementType.Name}[]", TypeKind.Array) { ElementType = elementType })
        {
            ElementType = elementType;
            Size = size;
        }

        public override void Accept(IIRVisitor visitor) => visitor.Visit(this);

        public override string ToString() => $"{Name} = new {ElementType.Name}[{Size}]";
    }

    /// <summary>
    /// Array store - stores a value at an index in an array
    /// </summary>
    public class IRArrayStore : IRInstruction
    {
        public IRValue Array { get; set; }
        public IRValue Index { get; set; }
        public IRValue Value { get; set; }

        public IRArrayStore(IRValue array, IRValue index, IRValue value)
        {
            Array = array;
            Index = index;
            Value = value;
        }

        public override void Accept(IIRVisitor visitor) => visitor.Visit(this);

        public override string ToString() => $"{Array.Name}[{Index}] = {Value}";
    }

    /// <summary>
    /// IR Await - awaits an async expression
    /// </summary>
    public class IRAwait : IRValue
    {
        public IRValue Expression { get; set; }

        public IRAwait(string resultName, IRValue expression, TypeInfo resultType)
            : base(resultName, resultType)
        {
            Expression = expression;
        }

        public override void Accept(IIRVisitor visitor) => visitor.Visit(this);

        public override string ToString() => $"{Name} = await {Expression}";
    }

    /// <summary>
    /// IR Yield - yields a value from an iterator
    /// </summary>
    public class IRYield : IRInstruction
    {
        public IRValue Value { get; set; }
        public bool IsBreak { get; set; }

        public IRYield(IRValue value, bool isBreak = false)
        {
            Value = value;
            IsBreak = isBreak;
        }

        public override void Accept(IIRVisitor visitor) => visitor.Visit(this);

        public override string ToString() => IsBreak ? "yield break" : $"yield return {Value}";
    }

    // ============================================================================
    // Basic Block and Function
    // ============================================================================
    
    /// <summary>
    /// Basic block - a sequence of instructions with a single entry and exit
    /// </summary>
    public class BasicBlock
    {
        public string Name { get; set; }
        public List<IRInstruction> Instructions { get; set; }
        public List<BasicBlock> Predecessors { get; set; }
        public List<BasicBlock> Successors { get; set; }
        public IRFunction ParentFunction { get; set; }
        public int Id { get; set; }
        
        // For optimization passes
        public bool IsVisited { get; set; }
        public HashSet<BasicBlock> Dominators { get; set; }
        public BasicBlock ImmediateDominator { get; set; }
        public HashSet<BasicBlock> DominanceFrontier { get; set; }
        
        public BasicBlock(string name)
        {
            Name = name;
            Instructions = new List<IRInstruction>();
            Predecessors = new List<BasicBlock>();
            Successors = new List<BasicBlock>();
            Dominators = new HashSet<BasicBlock>();
            DominanceFrontier = new HashSet<BasicBlock>();
        }
        
        public void AddInstruction(IRInstruction instruction)
        {
            Instructions.Add(instruction);
            instruction.ParentBlock = this;
        }
        
        public IRInstruction GetTerminator()
        {
            return Instructions.Count > 0 ? Instructions[Instructions.Count - 1] : null;
        }
        
        public bool IsTerminated()
        {
            var terminator = GetTerminator();
            return terminator is IRBranch || 
                   terminator is IRConditionalBranch || 
                   terminator is IRReturn ||
                   terminator is IRSwitch;
        }
        
        public void Accept(IIRVisitor visitor)
        {
            visitor.Visit(this);
        }
        
        public override string ToString() => Name;
    }
    
    /// <summary>
    /// IR Function - contains basic blocks
    /// </summary>
    public class IRFunction
    {
        public string Name { get; set; }
        public TypeInfo ReturnType { get; set; }
        public List<IRVariable> Parameters { get; set; }
        public List<BasicBlock> Blocks { get; set; }
        public BasicBlock EntryBlock { get; set; }
        public List<IRVariable> LocalVariables { get; set; }
        public bool IsExternal { get; set; }
        public List<string> GenericParameters { get; set; }
        public bool IsAsync { get; set; }
        public bool IsIterator { get; set; }

        private int _nextBlockId = 0;
        private int _nextTempId = 0;

        public IRFunction(string name, TypeInfo returnType)
        {
            Name = name;
            ReturnType = returnType;
            Parameters = new List<IRVariable>();
            Blocks = new List<BasicBlock>();
            LocalVariables = new List<IRVariable>();
            GenericParameters = new List<string>();
        }
        
        public BasicBlock CreateBlock(string name = null)
        {
            if (name == null)
                name = $"bb{_nextBlockId}";
            
            var block = new BasicBlock(name)
            {
                Id = _nextBlockId++,
                ParentFunction = this
            };
            
            Blocks.Add(block);
            
            if (EntryBlock == null)
                EntryBlock = block;
            
            return block;
        }
        
        public string GetNextTempName()
        {
            return $"t{_nextTempId++}";
        }
        
        public void Accept(IIRVisitor visitor)
        {
            visitor.Visit(this);
        }
        
        public override string ToString() => $"function {Name}";
    }
    
    /// <summary>
    /// IR Module - top-level container
    /// </summary>
    public class IRModule
    {
        public string Name { get; set; }
        public List<IRFunction> Functions { get; set; }
        public Dictionary<string, IRVariable> GlobalVariables { get; set; }
        public Dictionary<string, TypeInfo> Types { get; set; }
        public Dictionary<string, IRExternDeclaration> ExternDeclarations { get; set; }
        public Dictionary<string, IRClass> Classes { get; set; }

        public IRModule(string name)
        {
            Name = name;
            Functions = new List<IRFunction>();
            GlobalVariables = new Dictionary<string, IRVariable>();
            Types = new Dictionary<string, TypeInfo>();
            ExternDeclarations = new Dictionary<string, IRExternDeclaration>(StringComparer.OrdinalIgnoreCase);
            Classes = new Dictionary<string, IRClass>(StringComparer.OrdinalIgnoreCase);
        }

        public IRFunction CreateFunction(string name, TypeInfo returnType)
        {
            var function = new IRFunction(name, returnType);
            Functions.Add(function);
            return function;
        }

        public IRVariable CreateGlobalVariable(string name, TypeInfo type)
        {
            var variable = new IRVariable(name, type)
            {
                IsGlobal = true
            };
            GlobalVariables[name] = variable;
            return variable;
        }

        /// <summary>
        /// Check if a function is an extern
        /// </summary>
        public bool IsExtern(string name) => ExternDeclarations.ContainsKey(name);

        /// <summary>
        /// Get extern declaration by name
        /// </summary>
        public IRExternDeclaration GetExtern(string name)
        {
            ExternDeclarations.TryGetValue(name, out var externDecl);
            return externDecl;
        }
    }

    /// <summary>
    /// Represents an extern (platform-native) function declaration
    /// </summary>
    public class IRExternDeclaration
    {
        public string Name { get; set; }
        public bool IsFunction { get; set; }
        public string ReturnType { get; set; }
        public List<IRParameter> Parameters { get; set; }
        public Dictionary<string, string> PlatformImplementations { get; set; }

        public IRExternDeclaration()
        {
            Parameters = new List<IRParameter>();
            PlatformImplementations = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Get the implementation string for a specific platform
        /// </summary>
        public string GetImplementation(string platform)
        {
            if (PlatformImplementations.TryGetValue(platform, out var impl))
                return impl;
            return null;
        }

        /// <summary>
        /// Check if this extern has an implementation for the given platform
        /// </summary>
        public bool HasImplementation(string platform)
        {
            return PlatformImplementations.ContainsKey(platform);
        }
    }

    /// <summary>
    /// Represents a parameter in an IR function or extern
    /// </summary>
    public class IRParameter
    {
        public string Name { get; set; }
        public string TypeName { get; set; }
        public TypeInfo Type { get; set; }
    }

    /// <summary>
    /// Represents a class definition in IR
    /// </summary>
    public class IRClass
    {
        public string Name { get; set; }
        public string BaseClass { get; set; }
        public List<string> Interfaces { get; set; }
        public List<IRField> Fields { get; set; }
        public List<IRMethod> Methods { get; set; }
        public List<IRProperty> Properties { get; set; }
        public List<IRConstructor> Constructors { get; set; }
        public List<string> GenericParameters { get; set; }

        public IRClass(string name)
        {
            Name = name;
            Interfaces = new List<string>();
            Fields = new List<IRField>();
            Methods = new List<IRMethod>();
            Properties = new List<IRProperty>();
            Constructors = new List<IRConstructor>();
            GenericParameters = new List<string>();
        }
    }

    /// <summary>
    /// Represents a field in an IR class
    /// </summary>
    public class IRField
    {
        public string Name { get; set; }
        public TypeInfo Type { get; set; }
        public AccessModifier Access { get; set; }
        public bool IsStatic { get; set; }
        public IRValue Initializer { get; set; }
    }

    /// <summary>
    /// Represents a method in an IR class
    /// </summary>
    public class IRMethod
    {
        public string Name { get; set; }
        public TypeInfo ReturnType { get; set; }
        public AccessModifier Access { get; set; }
        public bool IsStatic { get; set; }
        public bool IsVirtual { get; set; }
        public bool IsOverride { get; set; }
        public bool IsSealed { get; set; }
        public List<IRVariable> Parameters { get; set; }
        public IRFunction Implementation { get; set; }

        public IRMethod()
        {
            Parameters = new List<IRVariable>();
        }
    }

    /// <summary>
    /// Represents a property in an IR class
    /// </summary>
    public class IRProperty
    {
        public string Name { get; set; }
        public TypeInfo Type { get; set; }
        public AccessModifier Access { get; set; }
        public bool IsStatic { get; set; }
        public bool IsReadOnly { get; set; }
        public bool IsWriteOnly { get; set; }
        public IRFunction Getter { get; set; }
        public IRFunction Setter { get; set; }
    }

    /// <summary>
    /// Represents a constructor in an IR class
    /// </summary>
    public class IRConstructor
    {
        public AccessModifier Access { get; set; }
        public List<IRVariable> Parameters { get; set; }
        public IRFunction Implementation { get; set; }
        public List<IRValue> BaseConstructorArgs { get; set; }

        public IRConstructor()
        {
            Parameters = new List<IRVariable>();
            BaseConstructorArgs = new List<IRValue>();
        }
    }

    /// <summary>
    /// Represents a new object instantiation: new ClassName(args)
    /// </summary>
    public class IRNewObject : IRValue
    {
        public string ClassName { get; set; }
        public List<IRValue> Arguments { get; set; }

        public IRNewObject(string resultName, string className, TypeInfo type)
            : base(resultName, type)
        {
            ClassName = className;
            Arguments = new List<IRValue>();
        }

        public override void Accept(IIRVisitor visitor) => visitor.Visit(this);
        public override string ToString()
        {
            var args = string.Join(", ", Arguments.Select(a => a.Name));
            return $"{Name} = new {ClassName}({args})";
        }
    }

    /// <summary>
    /// Represents a method call on an object instance: obj.Method(args)
    /// </summary>
    public class IRInstanceMethodCall : IRValue
    {
        public IRValue Object { get; set; }
        public string MethodName { get; set; }
        public List<IRValue> Arguments { get; set; }
        public bool IsVirtual { get; set; }

        public IRInstanceMethodCall(string resultName, IRValue obj, string methodName, TypeInfo returnType)
            : base(resultName, returnType)
        {
            Object = obj;
            MethodName = methodName;
            Arguments = new List<IRValue>();
        }

        public override void Accept(IIRVisitor visitor) => visitor.Visit(this);
        public override string ToString()
        {
            var args = string.Join(", ", Arguments.Select(a => a.Name));
            return $"{Name} = {Object.Name}.{MethodName}({args})";
        }
    }

    /// <summary>
    /// Represents a base class method call: base.Method(args) or MyBase.Method(args)
    /// </summary>
    public class IRBaseMethodCall : IRValue
    {
        public string MethodName { get; set; }
        public List<IRValue> Arguments { get; set; }

        public IRBaseMethodCall(string resultName, string methodName, TypeInfo returnType)
            : base(resultName, returnType)
        {
            MethodName = methodName;
            Arguments = new List<IRValue>();
        }

        public override void Accept(IIRVisitor visitor) => visitor.Visit(this);
        public override string ToString()
        {
            var args = string.Join(", ", Arguments.Select(a => a.Name));
            return $"{Name} = base.{MethodName}({args})";
        }
    }

    /// <summary>
    /// Represents a field access on an object: obj.Field
    /// </summary>
    public class IRFieldAccess : IRValue
    {
        public IRValue Object { get; set; }
        public string FieldName { get; set; }

        public IRFieldAccess(string resultName, IRValue obj, string fieldName, TypeInfo type)
            : base(resultName, type)
        {
            Object = obj;
            FieldName = fieldName;
        }

        public override void Accept(IIRVisitor visitor) => visitor.Visit(this);
        public override string ToString() => $"{Name} = {Object.Name}.{FieldName}";
    }

    /// <summary>
    /// Access modifier for class members
    /// </summary>
    public enum AccessModifier
    {
        Public,
        Private,
        Protected,
        Friend
    }
}
