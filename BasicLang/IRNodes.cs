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
        
        private int _nextBlockId = 0;
        private int _nextTempId = 0;
        
        public IRFunction(string name, TypeInfo returnType)
        {
            Name = name;
            ReturnType = returnType;
            Parameters = new List<IRVariable>();
            Blocks = new List<BasicBlock>();
            LocalVariables = new List<IRVariable>();
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
        
        public IRModule(string name)
        {
            Name = name;
            Functions = new List<IRFunction>();
            GlobalVariables = new Dictionary<string, IRVariable>();
            Types = new Dictionary<string, TypeInfo>();
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
    }
}
