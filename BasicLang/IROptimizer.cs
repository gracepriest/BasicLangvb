using System;
using System.Collections.Generic;
using System.Linq;
using BasicLang.Compiler.SemanticAnalysis;

namespace BasicLang.Compiler.IR.Optimization
{
    /// <summary>
    /// Base class for optimization passes
    /// </summary>
    public abstract class OptimizationPass
    {
        public string Name { get; protected set; }
        public int ModificationCount { get; protected set; }
        
        protected OptimizationPass(string name)
        {
            Name = name;
        }
        
        public abstract bool Run(IRModule module);
        
        protected void ReportModification()
        {
            ModificationCount++;
        }
    }
    
    /// <summary>
    /// Constant folding - evaluate constant expressions at compile time
    /// </summary>
    public class ConstantFoldingPass : OptimizationPass
    {
        public ConstantFoldingPass() : base("Constant Folding") { }
        
        public override bool Run(IRModule module)
        {
            ModificationCount = 0;
            
            foreach (var function in module.Functions)
            {
                if (function.IsExternal) continue;
                
                foreach (var block in function.Blocks)
                {
                    FoldBlock(block);
                }
            }
            
            return ModificationCount > 0;
        }
        
        private void FoldBlock(BasicBlock block)
        {
            for (int i = 0; i < block.Instructions.Count; i++)
            {
                var instruction = block.Instructions[i];
                
                if (instruction is IRBinaryOp binaryOp)
                {
                    var folded = TryFoldBinary(binaryOp);
                    if (folded != null)
                    {
                        block.Instructions[i] = folded;
                        ReportModification();
                    }
                }
                else if (instruction is IRUnaryOp unaryOp)
                {
                    var folded = TryFoldUnary(unaryOp);
                    if (folded != null)
                    {
                        block.Instructions[i] = folded;
                        ReportModification();
                    }
                }
                else if (instruction is IRCompare compare)
                {
                    var folded = TryFoldCompare(compare);
                    if (folded != null)
                    {
                        block.Instructions[i] = folded;
                        ReportModification();
                    }
                }
            }
        }
        
        private IRConstant TryFoldBinary(IRBinaryOp op)
        {
            if (!(op.Left is IRConstant left) || !(op.Right is IRConstant right))
                return null;
            
            try
            {
                object result = op.Operation switch
                {
                    BinaryOpKind.Add => FoldAdd(left.Value, right.Value),
                    BinaryOpKind.Sub => FoldSub(left.Value, right.Value),
                    BinaryOpKind.Mul => FoldMul(left.Value, right.Value),
                    BinaryOpKind.Div => FoldDiv(left.Value, right.Value),
                    BinaryOpKind.IntDiv => FoldIntDiv(left.Value, right.Value),
                    BinaryOpKind.Mod => FoldMod(left.Value, right.Value),
                    BinaryOpKind.And => FoldAnd(left.Value, right.Value),
                    BinaryOpKind.Or => FoldOr(left.Value, right.Value),
                    BinaryOpKind.Xor => FoldXor(left.Value, right.Value),
                    BinaryOpKind.Shl => FoldShl(left.Value, right.Value),
                    BinaryOpKind.Shr => FoldShr(left.Value, right.Value),
                    _ => null
                };
                
                if (result != null)
                {
                    return new IRConstant(result, op.Type);
                }
            }
            catch
            {
                // Division by zero, overflow, etc.
            }
            
            return null;
        }
        
        private IRConstant TryFoldUnary(IRUnaryOp op)
        {
            if (!(op.Operand is IRConstant operand))
                return null;
            
            try
            {
                object result = op.Operation switch
                {
                    UnaryOpKind.Neg => FoldNeg(operand.Value),
                    UnaryOpKind.Not => FoldNot(operand.Value),
                    UnaryOpKind.Inc => FoldInc(operand.Value),
                    UnaryOpKind.Dec => FoldDec(operand.Value),
                    _ => null
                };
                
                if (result != null)
                {
                    return new IRConstant(result, op.Type);
                }
            }
            catch { }
            
            return null;
        }
        
        private IRConstant TryFoldCompare(IRCompare cmp)
        {
            if (!(cmp.Left is IRConstant left) || !(cmp.Right is IRConstant right))
                return null;
            
            try
            {
                bool result = cmp.Comparison switch
                {
                    CompareKind.Eq => CompareEq(left.Value, right.Value),
                    CompareKind.Ne => !CompareEq(left.Value, right.Value),
                    CompareKind.Lt => CompareLt(left.Value, right.Value),
                    CompareKind.Le => !CompareGt(left.Value, right.Value),
                    CompareKind.Gt => CompareGt(left.Value, right.Value),
                    CompareKind.Ge => !CompareLt(left.Value, right.Value),
                    _ => false
                };
                
                return new IRConstant(result, cmp.Type);
            }
            catch { }
            
            return null;
        }
        
        // Arithmetic operations
        private object FoldAdd(object a, object b)
        {
            if (a is int ia && b is int ib) return ia + ib;
            if (a is long la && b is long lb) return la + lb;
            if (a is float fa && b is float fb) return fa + fb;
            if (a is double da && b is double db) return da + db;
            if (a is string sa && b is string sb) return sa + sb;
            return null;
        }
        
        private object FoldSub(object a, object b)
        {
            if (a is int ia && b is int ib) return ia - ib;
            if (a is long la && b is long lb) return la - lb;
            if (a is float fa && b is float fb) return fa - fb;
            if (a is double da && b is double db) return da - db;
            return null;
        }
        
        private object FoldMul(object a, object b)
        {
            if (a is int ia && b is int ib) return ia * ib;
            if (a is long la && b is long lb) return la * lb;
            if (a is float fa && b is float fb) return fa * fb;
            if (a is double da && b is double db) return da * db;
            return null;
        }
        
        private object FoldDiv(object a, object b)
        {
            if (a is int ia && b is int ib && ib != 0) return ia / ib;
            if (a is long la && b is long lb && lb != 0) return la / lb;
            if (a is float fa && b is float fb && fb != 0) return fa / fb;
            if (a is double da && b is double db && db != 0) return da / db;
            return null;
        }
        
        private object FoldIntDiv(object a, object b)
        {
            if (a is int ia && b is int ib && ib != 0) return ia / ib;
            if (a is long la && b is long lb && lb != 0) return la / lb;
            return null;
        }
        
        private object FoldMod(object a, object b)
        {
            if (a is int ia && b is int ib && ib != 0) return ia % ib;
            if (a is long la && b is long lb && lb != 0) return la % lb;
            return null;
        }
        
        private object FoldAnd(object a, object b)
        {
            if (a is int ia && b is int ib) return ia & ib;
            if (a is long la && b is long lb) return la & lb;
            if (a is bool ba && b is bool bb) return ba && bb;
            return null;
        }
        
        private object FoldOr(object a, object b)
        {
            if (a is int ia && b is int ib) return ia | ib;
            if (a is long la && b is long lb) return la | lb;
            if (a is bool ba && b is bool bb) return ba || bb;
            return null;
        }
        
        private object FoldXor(object a, object b)
        {
            if (a is int ia && b is int ib) return ia ^ ib;
            if (a is long la && b is long lb) return la ^ lb;
            return null;
        }
        
        private object FoldShl(object a, object b)
        {
            if (a is int ia && b is int ib) return ia << ib;
            if (a is long la && b is int lb) return la << lb;
            return null;
        }
        
        private object FoldShr(object a, object b)
        {
            if (a is int ia && b is int ib) return ia >> ib;
            if (a is long la && b is int lb) return la >> lb;
            return null;
        }
        
        private object FoldNeg(object a)
        {
            if (a is int ia) return -ia;
            if (a is long la) return -la;
            if (a is float fa) return -fa;
            if (a is double da) return -da;
            return null;
        }
        
        private object FoldNot(object a)
        {
            if (a is bool ba) return !ba;
            return null;
        }
        
        private object FoldInc(object a)
        {
            if (a is int ia) return ia + 1;
            if (a is long la) return la + 1;
            return null;
        }
        
        private object FoldDec(object a)
        {
            if (a is int ia) return ia - 1;
            if (a is long la) return la - 1;
            return null;
        }
        
        // Comparison operations
        private bool CompareEq(object a, object b)
        {
            return Equals(a, b);
        }
        
        private bool CompareLt(object a, object b)
        {
            if (a is int ia && b is int ib) return ia < ib;
            if (a is long la && b is long lb) return la < lb;
            if (a is float fa && b is float fb) return fa < fb;
            if (a is double da && b is double db) return da < db;
            return false;
        }
        
        private bool CompareGt(object a, object b)
        {
            if (a is int ia && b is int ib) return ia > ib;
            if (a is long la && b is long lb) return la > lb;
            if (a is float fa && b is float fb) return fa > fb;
            if (a is double da && b is double db) return da > db;
            return false;
        }
    }
    
    /// <summary>
    /// Dead code elimination - remove instructions that don't affect program output
    /// </summary>
    public class DeadCodeEliminationPass : OptimizationPass
    {
        public DeadCodeEliminationPass() : base("Dead Code Elimination") { }
        
        public override bool Run(IRModule module)
        {
            ModificationCount = 0;
            
            foreach (var function in module.Functions)
            {
                if (function.IsExternal) continue;
                
                // Build CFG
                var cfg = new ControlFlowGraph(function);
                cfg.Build();
                
                // Remove unreachable blocks
                int removed = cfg.RemoveUnreachableBlocks();
                ModificationCount += removed;
                
                // Remove dead instructions
                foreach (var block in function.Blocks)
                {
                    RemoveDeadInstructions(block);
                }
            }
            
            return ModificationCount > 0;
        }
        
        private void RemoveDeadInstructions(BasicBlock block)
        {
            var used = new HashSet<IRValue>();

            // Mark instructions that are used
            foreach (var inst in block.Instructions)
            {
                MarkUsed(inst, used);
            }

            // Remove unused assignments
            for (int i = block.Instructions.Count - 1; i >= 0; i--)
            {
                var inst = block.Instructions[i];

                // Don't remove instructions that represent assignments to named variables
                // (non-temp names indicate the result is assigned to a real variable)
                if (inst is IRValue v && !string.IsNullOrEmpty(v.Name) && !v.Name.StartsWith("_tmp"))
                {
                    continue;
                }

                if (inst is IRBinaryOp binaryOp && !used.Contains(binaryOp))
                {
                    block.Instructions.RemoveAt(i);
                    ReportModification();
                }
                else if (inst is IRUnaryOp unaryOp && !used.Contains(unaryOp))
                {
                    block.Instructions.RemoveAt(i);
                    ReportModification();
                }
                else if (inst is IRCompare compare && !used.Contains(compare))
                {
                    block.Instructions.RemoveAt(i);
                    ReportModification();
                }
                else if (inst is IRLoad load && !used.Contains(load))
                {
                    block.Instructions.RemoveAt(i);
                    ReportModification();
                }
            }
        }
        
        private void MarkUsed(IRInstruction inst, HashSet<IRValue> used)
        {
            if (inst is IRBinaryOp binaryOp)
            {
                used.Add(binaryOp.Left);
                used.Add(binaryOp.Right);
            }
            else if (inst is IRUnaryOp unaryOp)
            {
                used.Add(unaryOp.Operand);
            }
            else if (inst is IRCompare compare)
            {
                used.Add(compare.Left);
                used.Add(compare.Right);
            }
            else if (inst is IRStore store)
            {
                used.Add(store.Value);
                used.Add(store.Address);
            }
            else if (inst is IRLoad load)
            {
                used.Add(load.Address);
            }
            else if (inst is IRCall call)
            {
                foreach (var arg in call.Arguments)
                {
                    used.Add(arg);
                }
            }
            else if (inst is IRReturn ret && ret.Value != null)
            {
                used.Add(ret.Value);
            }
            else if (inst is IRConditionalBranch condBr)
            {
                used.Add(condBr.Condition);
            }
            else if (inst is IRSwitch switchInst)
            {
                used.Add(switchInst.Value);
            }
            else if (inst is IRAssignment assignment)
            {
                used.Add(assignment.Value);
            }
        }
    }
    
    /// <summary>
    /// Copy propagation - replace uses of copied variables with their source
    /// </summary>
    public class CopyPropagationPass : OptimizationPass
    {
        public CopyPropagationPass() : base("Copy Propagation") { }
        
        public override bool Run(IRModule module)
        {
            ModificationCount = 0;
            
            foreach (var function in module.Functions)
            {
                if (function.IsExternal) continue;
                
                foreach (var block in function.Blocks)
                {
                    PropagateCopies(block);
                }
            }
            
            return ModificationCount > 0;
        }
        
        private void PropagateCopies(BasicBlock block)
        {
            var copies = new Dictionary<IRVariable, IRValue>();
            
            foreach (var inst in block.Instructions)
            {
                // Replace uses
                ReplaceUses(inst, copies);
                
                // Track copy assignments
                if (inst is IRAssignment assignment && 
                    assignment.Target is IRVariable target &&
                    assignment.Value is IRValue value)
                {
                    copies[target] = value;
                }
            }
        }
        
        private void ReplaceUses(IRInstruction inst, Dictionary<IRVariable, IRValue> copies)
        {
            if (inst is IRBinaryOp binaryOp)
            {
                if (binaryOp.Left is IRVariable leftVar && copies.ContainsKey(leftVar))
                {
                    binaryOp.Left = copies[leftVar];
                    ReportModification();
                }
                if (binaryOp.Right is IRVariable rightVar && copies.ContainsKey(rightVar))
                {
                    binaryOp.Right = copies[rightVar];
                    ReportModification();
                }
            }
            else if (inst is IRUnaryOp unaryOp)
            {
                if (unaryOp.Operand is IRVariable operandVar && copies.ContainsKey(operandVar))
                {
                    unaryOp.Operand = copies[operandVar];
                    ReportModification();
                }
            }
            else if (inst is IRCompare compare)
            {
                if (compare.Left is IRVariable leftVar && copies.ContainsKey(leftVar))
                {
                    compare.Left = copies[leftVar];
                    ReportModification();
                }
                if (compare.Right is IRVariable rightVar && copies.ContainsKey(rightVar))
                {
                    compare.Right = copies[rightVar];
                    ReportModification();
                }
            }
        }
    }
    
    /// <summary>
    /// Common subexpression elimination - avoid recomputing identical expressions
    /// </summary>
    public class CommonSubexpressionEliminationPass : OptimizationPass
    {
        public CommonSubexpressionEliminationPass() : base("Common Subexpression Elimination") { }
        
        public override bool Run(IRModule module)
        {
            ModificationCount = 0;
            
            foreach (var function in module.Functions)
            {
                if (function.IsExternal) continue;
                
                foreach (var block in function.Blocks)
                {
                    EliminateCommonSubexpressions(block);
                }
            }
            
            return ModificationCount > 0;
        }
        
        private void EliminateCommonSubexpressions(BasicBlock block)
        {
            var expressions = new Dictionary<string, IRValue>();
            
            for (int i = 0; i < block.Instructions.Count; i++)
            {
                var inst = block.Instructions[i];
                
                if (inst is IRBinaryOp binaryOp)
                {
                    var key = $"{binaryOp.Operation}_{binaryOp.Left.Name}_{binaryOp.Right.Name}";
                    
                    if (expressions.ContainsKey(key))
                    {
                        // Replace this expression with the previous one
                        var replacement = expressions[key];
                        ReplaceAllUses(block, binaryOp, replacement);
                        block.Instructions.RemoveAt(i);
                        i--;
                        ReportModification();
                    }
                    else
                    {
                        expressions[key] = binaryOp;
                    }
                }
            }
        }
        
        private void ReplaceAllUses(BasicBlock block, IRValue oldValue, IRValue newValue)
        {
            foreach (var inst in block.Instructions)
            {
                if (inst is IRBinaryOp binaryOp)
                {
                    if (ReferenceEquals(binaryOp.Left, oldValue))
                        binaryOp.Left = newValue;
                    if (ReferenceEquals(binaryOp.Right, oldValue))
                        binaryOp.Right = newValue;
                }
                else if (inst is IRUnaryOp unaryOp)
                {
                    if (ReferenceEquals(unaryOp.Operand, oldValue))
                        unaryOp.Operand = newValue;
                }
            }
        }
    }
    
    /// <summary>
    /// Loop invariant code motion - move loop-invariant code outside loops
    /// </summary>
    public class LoopInvariantCodeMotionPass : OptimizationPass
    {
        public LoopInvariantCodeMotionPass() : base("Loop Invariant Code Motion") { }
        
        public override bool Run(IRModule module)
        {
            ModificationCount = 0;
            
            foreach (var function in module.Functions)
            {
                if (function.IsExternal) continue;
                
                var cfg = new ControlFlowGraph(function);
                cfg.Build();
                cfg.ComputeDominators();
                cfg.IdentifyLoops();
                
                foreach (var loop in cfg.NaturalLoops)
                {
                    HoistInvariants(loop, cfg);
                }
            }
            
            return ModificationCount > 0;
        }
        
        private void HoistInvariants(List<BasicBlock> loop, ControlFlowGraph cfg)
        {
            var loopSet = new HashSet<BasicBlock>(loop);
            var header = loop.FirstOrDefault(b => b.Predecessors.Any(p => !loopSet.Contains(p)));
            
            if (header == null) return;
            
            // Find preheader (block before loop header)
            var preheader = header.Predecessors.FirstOrDefault(p => !loopSet.Contains(p));
            if (preheader == null) return;
            
            var invariants = new HashSet<IRInstruction>();
            
            // Find loop-invariant instructions
            bool changed = true;
            while (changed)
            {
                changed = false;
                
                foreach (var block in loop)
                {
                    foreach (var inst in block.Instructions)
                    {
                        if (IsLoopInvariant(inst, loopSet, invariants))
                        {
                            if (invariants.Add(inst))
                            {
                                changed = true;
                            }
                        }
                    }
                }
            }
            
            // Move invariants to preheader
            foreach (var block in loop)
            {
                for (int i = block.Instructions.Count - 1; i >= 0; i--)
                {
                    var inst = block.Instructions[i];
                    
                    if (invariants.Contains(inst))
                    {
                        block.Instructions.RemoveAt(i);
                        
                        // Insert before preheader's terminator
                        int insertPos = preheader.Instructions.Count;
                        if (insertPos > 0 && preheader.Instructions[insertPos - 1] is IRBranch)
                            insertPos--;
                        
                        preheader.Instructions.Insert(insertPos, inst);
                        ReportModification();
                    }
                }
            }
        }
        
        private bool IsLoopInvariant(IRInstruction inst, HashSet<BasicBlock> loop, HashSet<IRInstruction> knownInvariants)
        {
            // Terminators and side-effect instructions are not invariant
            if (inst is IRBranch || inst is IRConditionalBranch || inst is IRReturn ||
                inst is IRStore || inst is IRCall)
            {
                return false;
            }
            
            // Check if all operands are invariant
            if (inst is IRBinaryOp binaryOp)
            {
                return IsValueInvariant(binaryOp.Left, loop, knownInvariants) &&
                       IsValueInvariant(binaryOp.Right, loop, knownInvariants);
            }
            else if (inst is IRUnaryOp unaryOp)
            {
                return IsValueInvariant(unaryOp.Operand, loop, knownInvariants);
            }
            else if (inst is IRCompare compare)
            {
                return IsValueInvariant(compare.Left, loop, knownInvariants) &&
                       IsValueInvariant(compare.Right, loop, knownInvariants);
            }
            
            return false;
        }
        
        private bool IsValueInvariant(IRValue value, HashSet<BasicBlock> loop, HashSet<IRInstruction> knownInvariants)
        {
            if (value is IRConstant)
                return true;
            
            if (value is IRVariable variable)
            {
                // Parameter variables are invariant
                if (variable.IsParameter)
                    return true;
                
                // Global variables could change
                if (variable.IsGlobal)
                    return false;
            }
            
            // Check if the defining instruction is a known invariant
            if (value is IRInstruction inst)
            {
                return !loop.Contains(inst.ParentBlock) || knownInvariants.Contains(inst);
            }
            
            return false;
        }
    }
    
    /// <summary>
    /// Strength reduction - replace expensive operations with cheaper equivalents
    /// </summary>
    public class StrengthReductionPass : OptimizationPass
    {
        public StrengthReductionPass() : base("Strength Reduction") { }
        
        public override bool Run(IRModule module)
        {
            ModificationCount = 0;
            
            foreach (var function in module.Functions)
            {
                if (function.IsExternal) continue;
                
                foreach (var block in function.Blocks)
                {
                    ReduceStrength(block);
                }
            }
            
            return ModificationCount > 0;
        }
        
        private void ReduceStrength(BasicBlock block)
        {
            for (int i = 0; i < block.Instructions.Count; i++)
            {
                var inst = block.Instructions[i];
                
                if (inst is IRBinaryOp binaryOp)
                {
                    var reduced = TryReduceBinary(binaryOp);
                    if (reduced != null)
                    {
                        block.Instructions[i] = reduced;
                        ReportModification();
                    }
                }
            }
        }
        
        private IRInstruction TryReduceBinary(IRBinaryOp op)
        {
            // Multiplication by power of 2 Ã¢â€ â€™ shift
            if (op.Operation == BinaryOpKind.Mul)
            {
                if (op.Right is IRConstant constant && constant.Value is int power)
                {
                    if (IsPowerOfTwo(power))
                    {
                        int shift = (int)Math.Log(power, 2);
                        var shiftAmount = new IRConstant(shift, op.Right.Type);
                        return new IRBinaryOp(op.Name, BinaryOpKind.Shl, op.Left, shiftAmount, op.Type);
                    }
                }
            }
            
            // Division by power of 2 Ã¢â€ â€™ shift
            if (op.Operation == BinaryOpKind.Div)
            {
                if (op.Right is IRConstant constant && constant.Value is int power)
                {
                    if (IsPowerOfTwo(power))
                    {
                        int shift = (int)Math.Log(power, 2);
                        var shiftAmount = new IRConstant(shift, op.Right.Type);
                        return new IRBinaryOp(op.Name, BinaryOpKind.Shr, op.Left, shiftAmount, op.Type);
                    }
                }
            }
            
            // Modulo by power of 2 Ã¢â€ â€™ bitwise AND
            if (op.Operation == BinaryOpKind.Mod)
            {
                if (op.Right is IRConstant constant && constant.Value is int power)
                {
                    if (IsPowerOfTwo(power))
                    {
                        var mask = new IRConstant(power - 1, op.Right.Type);
                        return new IRBinaryOp(op.Name, BinaryOpKind.And, op.Left, mask, op.Type);
                    }
                }
            }
            
            return null;
        }
        
        private bool IsPowerOfTwo(int n)
        {
            return n > 0 && (n & (n - 1)) == 0;
        }
    }
    
    /// <summary>
    /// Optimization pipeline - runs multiple passes in sequence
    /// </summary>
    public class OptimizationPipeline
    {
        private readonly List<OptimizationPass> _passes;
        private int _maxIterations;
        
        public OptimizationPipeline(int maxIterations = 10)
        {
            _passes = new List<OptimizationPass>();
            _maxIterations = maxIterations;
        }
        
        public void AddPass(OptimizationPass pass)
        {
            _passes.Add(pass);
        }
        
        public void AddStandardPasses()
        {
            AddPass(new ConstantFoldingPass());
            AddPass(new CopyPropagationPass());
            AddPass(new DeadCodeEliminationPass());
            AddPass(new CommonSubexpressionEliminationPass());
            AddPass(new StrengthReductionPass());
        }
        
        public void AddAggressivePasses()
        {
            AddStandardPasses();
            AddPass(new LoopInvariantCodeMotionPass());
        }
        
        public OptimizationResult Run(IRModule module)
        {
            var result = new OptimizationResult();
            
            for (int iteration = 0; iteration < _maxIterations; iteration++)
            {
                bool anyChanges = false;
                
                foreach (var pass in _passes)
                {
                    bool changed = pass.Run(module);
                    
                    result.PassResults.Add(new PassResult
                    {
                        PassName = pass.Name,
                        Iteration = iteration,
                        ModificationCount = pass.ModificationCount,
                        MadeChanges = changed
                    });
                    
                    if (changed)
                    {
                        anyChanges = true;
                        result.TotalModifications += pass.ModificationCount;
                    }
                }
                
                if (!anyChanges)
                {
                    result.IterationsRun = iteration + 1;
                    break;
                }
                
                result.IterationsRun = iteration + 1;
            }
            
            return result;
        }
    }
    
    public class OptimizationResult
    {
        public int IterationsRun { get; set; }
        public int TotalModifications { get; set; }
        public List<PassResult> PassResults { get; set; }
        
        public OptimizationResult()
        {
            PassResults = new List<PassResult>();
        }
        
        public override string ToString()
        {
            return $"Ran {IterationsRun} iterations, made {TotalModifications} total modifications";
        }
    }
    
    public class PassResult
    {
        public string PassName { get; set; }
        public int Iteration { get; set; }
        public int ModificationCount { get; set; }
        public bool MadeChanges { get; set; }
        
        public override string ToString()
        {
            return $"[Iteration {Iteration}] {PassName}: {ModificationCount} modifications";
        }
    }
}
