using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using BasicLang.Compiler.IR;
using BasicLang.Compiler.SemanticAnalysis;

namespace BasicLang.Debugger
{
    /// <summary>
    /// An interpreter that supports debugging operations
    /// </summary>
    public class DebuggableInterpreter
    {
        private readonly IRModule _module;
        private readonly Dictionary<string, HashSet<int>> _breakpoints = new();
        private readonly Dictionary<string, object> _globalVariables = new();
        private readonly Stack<CallFrame> _callStack = new();
        private readonly Random _random = new();

        private bool _paused;
        private bool _stepping;
        private StepMode _stepMode;
        private int _stepDepth;
        private bool _stopRequested;
        private int _currentLine;
        private readonly object _syncLock = new();
        private readonly ManualResetEventSlim _pauseEvent = new(true);

        public event EventHandler<DebugEventArgs> BreakpointHit;
        public event EventHandler<DebugEventArgs> StepComplete;
        public event EventHandler<OutputEventArgs> OutputProduced;

        private enum StepMode
        {
            None,
            Into,
            Over,
            Out
        }

        public DebuggableInterpreter(IRModule module)
        {
            _module = module;
        }

        public void SetBreakpoint(string file, int line)
        {
            lock (_syncLock)
            {
                if (!_breakpoints.ContainsKey(file))
                    _breakpoints[file] = new HashSet<int>();
                _breakpoints[file].Add(line);
            }
        }

        public void RemoveBreakpoint(string file, int line)
        {
            lock (_syncLock)
            {
                if (_breakpoints.ContainsKey(file))
                    _breakpoints[file].Remove(line);
            }
        }

        public void Run()
        {
            _stopRequested = false;
            _paused = false;
            _pauseEvent.Set();

            try
            {
                // Find and execute Main or first function
                var mainFunc = _module.Functions.FirstOrDefault(f =>
                    f.Name.Equals("Main", StringComparison.OrdinalIgnoreCase));

                if (mainFunc != null)
                {
                    ExecuteFunction(mainFunc, new object[0]);
                }
                else if (_module.Functions.Count > 0)
                {
                    ExecuteFunction(_module.Functions[0], new object[0]);
                }
            }
            catch (Exception ex)
            {
                OnOutput($"Runtime error: {ex.Message}\n");
            }
        }

        public void Continue()
        {
            lock (_syncLock)
            {
                _stepping = false;
                _paused = false;
                _pauseEvent.Set();
            }
        }

        public void StepOver()
        {
            lock (_syncLock)
            {
                _stepping = true;
                _stepMode = StepMode.Over;
                _stepDepth = _callStack.Count;
                _paused = false;
                _pauseEvent.Set();
            }
        }

        public void StepInto()
        {
            lock (_syncLock)
            {
                _stepping = true;
                _stepMode = StepMode.Into;
                _paused = false;
                _pauseEvent.Set();
            }
        }

        public void StepOut()
        {
            lock (_syncLock)
            {
                _stepping = true;
                _stepMode = StepMode.Out;
                _stepDepth = _callStack.Count - 1;
                _paused = false;
                _pauseEvent.Set();
            }
        }

        public void Pause()
        {
            lock (_syncLock)
            {
                _paused = true;
                _pauseEvent.Reset();
            }
        }

        public void Stop()
        {
            lock (_syncLock)
            {
                _stopRequested = true;
                _pauseEvent.Set();
            }
        }

        public List<StackFrameInfo> GetStackFrames()
        {
            var frames = new List<StackFrameInfo>();
            lock (_syncLock)
            {
                foreach (var frame in _callStack)
                {
                    frames.Add(new StackFrameInfo
                    {
                        FunctionName = frame.FunctionName,
                        Line = frame.CurrentLine
                    });
                }
            }
            return frames;
        }

        public Dictionary<string, object> GetLocalVariables(int frameId)
        {
            lock (_syncLock)
            {
                var frames = new List<CallFrame>(_callStack);
                if (frameId >= 0 && frameId < frames.Count)
                {
                    return new Dictionary<string, object>(frames[frameId].LocalVariables);
                }
            }
            return new Dictionary<string, object>();
        }

        public object EvaluateExpression(string expression)
        {
            // Simple variable lookup
            lock (_syncLock)
            {
                if (_callStack.Count > 0)
                {
                    var frame = _callStack.Peek();
                    if (frame.LocalVariables.TryGetValue(expression, out var value))
                        return value;
                }
                if (_globalVariables.TryGetValue(expression, out var globalValue))
                    return globalValue;
            }
            return null;
        }

        private void CheckBreakpoint(int line)
        {
            if (_stopRequested) return;

            _currentLine = line;

            // Check for breakpoints
            bool shouldBreak = false;
            lock (_syncLock)
            {
                foreach (var bp in _breakpoints)
                {
                    if (bp.Value.Contains(line))
                    {
                        shouldBreak = true;
                        break;
                    }
                }
            }

            if (shouldBreak)
            {
                _paused = true;
                BreakpointHit?.Invoke(this, new DebugEventArgs { Line = line });
                _pauseEvent.Reset();
            }
            else if (_stepping)
            {
                bool shouldStop = false;
                lock (_syncLock)
                {
                    switch (_stepMode)
                    {
                        case StepMode.Into:
                            shouldStop = true;
                            break;
                        case StepMode.Over:
                            shouldStop = _callStack.Count <= _stepDepth;
                            break;
                        case StepMode.Out:
                            shouldStop = _callStack.Count < _stepDepth;
                            break;
                    }
                }

                if (shouldStop)
                {
                    _stepping = false;
                    _paused = true;
                    StepComplete?.Invoke(this, new DebugEventArgs { Line = line });
                    _pauseEvent.Reset();
                }
            }

            // Wait if paused
            _pauseEvent.Wait();
        }

        private object ExecuteFunction(IRFunction function, object[] arguments)
        {
            var frame = new CallFrame
            {
                FunctionName = function.Name,
                LocalVariables = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
            };

            // Set up parameters
            for (int i = 0; i < function.Parameters.Count && i < arguments.Length; i++)
            {
                frame.LocalVariables[function.Parameters[i].Name] = arguments[i];
            }

            lock (_syncLock)
            {
                _callStack.Push(frame);
            }

            try
            {
                if (function.EntryBlock != null)
                {
                    return ExecuteBlock(function.EntryBlock, frame);
                }
                else if (function.Blocks.Count > 0)
                {
                    return ExecuteBlock(function.Blocks[0], frame);
                }
                return null;
            }
            finally
            {
                lock (_syncLock)
                {
                    _callStack.Pop();
                }
            }
        }

        private object ExecuteBlock(BasicBlock block, CallFrame frame)
        {
            frame.CurrentLine = Math.Abs(block.Name?.GetHashCode() ?? 1) % 1000 + 1; // Approximate line

            foreach (var instruction in block.Instructions)
            {
                if (_stopRequested) return null;

                CheckBreakpoint(frame.CurrentLine);
                var result = ExecuteInstruction(instruction, frame);

                if (result is ReturnValue rv)
                    return rv.Value;
            }

            // Handle terminator
            var terminator = block.GetTerminator();
            if (terminator != null)
            {
                if (terminator is IRBranch branch)
                {
                    return ExecuteBlock(branch.Target, frame);
                }
                else if (terminator is IRConditionalBranch condBranch)
                {
                    var cond = EvaluateValue(condBranch.Condition, frame);
                    var target = Convert.ToBoolean(cond) ? condBranch.TrueTarget : condBranch.FalseTarget;
                    return ExecuteBlock(target, frame);
                }
                else if (terminator is IRReturn ret)
                {
                    return ret.Value != null ? EvaluateValue(ret.Value, frame) : null;
                }
            }

            // Continue to successor if exists
            if (block.Successors.Count > 0)
            {
                return ExecuteBlock(block.Successors[0], frame);
            }

            return null;
        }

        private object ExecuteInstruction(IRInstruction instruction, CallFrame frame)
        {
            switch (instruction)
            {
                case IRBinaryOp binOp:
                    var left = EvaluateValue(binOp.Left, frame);
                    var right = EvaluateValue(binOp.Right, frame);
                    var result = PerformBinaryOp(binOp.Operation, left, right);
                    frame.LocalVariables[binOp.Name] = result;
                    break;

                case IRUnaryOp unaryOp:
                    var operand = EvaluateValue(unaryOp.Operand, frame);
                    var unaryResult = PerformUnaryOp(unaryOp.Operation, operand);
                    frame.LocalVariables[unaryOp.Name] = unaryResult;
                    break;

                case IRCall call:
                    var callResult = ExecuteCall(call, frame);
                    if (!string.IsNullOrEmpty(call.Name))
                        frame.LocalVariables[call.Name] = callResult;
                    break;

                case IRStore store:
                    var storeValue = EvaluateValue(store.Value, frame);
                    if (store.Address is IRVariable varRef)
                        frame.LocalVariables[varRef.Name] = storeValue;
                    break;

                case IRAlloca alloca:
                    frame.LocalVariables[alloca.Name] = GetDefaultValue(alloca.Type);
                    break;

                case IRAssignment assignment:
                    var assignValue = EvaluateValue(assignment.Value, frame);
                    frame.LocalVariables[assignment.Target.Name] = assignValue;
                    break;

                case IRReturn ret:
                    var retValue = ret.Value != null ? EvaluateValue(ret.Value, frame) : null;
                    return new ReturnValue { Value = retValue };
            }

            return null;
        }

        private object EvaluateValue(IRValue value, CallFrame frame)
        {
            switch (value)
            {
                case IRConstant constant:
                    return constant.Value;

                case IRVariable variable:
                    if (frame.LocalVariables.TryGetValue(variable.Name, out var localVal))
                        return localVal;
                    if (_globalVariables.TryGetValue(variable.Name, out var globalVal))
                        return globalVal;
                    return null;

                case IRLoad load:
                    return EvaluateValue(load.Address, frame);

                case IRBinaryOp binOp:
                    var left = EvaluateValue(binOp.Left, frame);
                    var right = EvaluateValue(binOp.Right, frame);
                    return PerformBinaryOp(binOp.Operation, left, right);

                case IRUnaryOp unaryOp:
                    var operand = EvaluateValue(unaryOp.Operand, frame);
                    return PerformUnaryOp(unaryOp.Operation, operand);

                case IRCall call:
                    return ExecuteCall(call, frame);
            }

            return null;
        }

        private object ExecuteCall(IRCall call, CallFrame frame)
        {
            var args = new List<object>();
            foreach (var arg in call.Arguments)
            {
                args.Add(EvaluateValue(arg, frame));
            }

            // Check built-in functions
            var result = ExecuteBuiltIn(call.FunctionName, args.ToArray());
            if (result != null)
                return result;

            // User function
            var func = _module.Functions.FirstOrDefault(f =>
                f.Name.Equals(call.FunctionName, StringComparison.OrdinalIgnoreCase));

            if (func != null)
            {
                return ExecuteFunction(func, args.ToArray());
            }

            return null;
        }

        private object ExecuteBuiltIn(string name, object[] args)
        {
            switch (name.ToLowerInvariant())
            {
                case "printline":
                    var text = args.Length > 0 ? args[0]?.ToString() ?? "" : "";
                    OnOutput(text + "\n");
                    return null;

                case "print":
                    OnOutput(args.Length > 0 ? args[0]?.ToString() ?? "" : "");
                    return null;

                case "cstr":
                    return args.Length > 0 ? args[0]?.ToString() ?? "" : "";

                case "cint":
                    return args.Length > 0 ? Convert.ToInt32(args[0]) : 0;

                case "cdbl":
                    return args.Length > 0 ? Convert.ToDouble(args[0]) : 0.0;

                case "sqrt":
                    return args.Length > 0 ? Math.Sqrt(Convert.ToDouble(args[0])) : 0.0;

                case "abs":
                    return args.Length > 0 ? Math.Abs(Convert.ToDouble(args[0])) : 0.0;

                case "len":
                    return args.Length > 0 ? (args[0]?.ToString()?.Length ?? 0) : 0;

                case "rnd":
                    return _random.NextDouble();
            }

            return null;
        }

        private object PerformBinaryOp(BinaryOpKind op, object left, object right)
        {
            // Handle string concatenation
            if (op == BinaryOpKind.Concat)
            {
                return (left?.ToString() ?? "") + (right?.ToString() ?? "");
            }

            // Handle string comparison
            if (left is string || right is string)
            {
                var ls = left?.ToString() ?? "";
                var rs = right?.ToString() ?? "";
                return op switch
                {
                    BinaryOpKind.Eq => ls == rs,
                    BinaryOpKind.Ne => ls != rs,
                    BinaryOpKind.Add => ls + rs, // String concatenation fallback
                    _ => 0
                };
            }

            var l = Convert.ToDouble(left ?? 0);
            var r = Convert.ToDouble(right ?? 0);

            return op switch
            {
                BinaryOpKind.Add => l + r,
                BinaryOpKind.Sub => l - r,
                BinaryOpKind.Mul => l * r,
                BinaryOpKind.Div => r != 0 ? l / r : 0,
                BinaryOpKind.Mod => r != 0 ? l % r : 0,
                BinaryOpKind.IntDiv => r != 0 ? (int)l / (int)r : 0,
                BinaryOpKind.Eq => l == r,
                BinaryOpKind.Ne => l != r,
                BinaryOpKind.Lt => l < r,
                BinaryOpKind.Le => l <= r,
                BinaryOpKind.Gt => l > r,
                BinaryOpKind.Ge => l >= r,
                BinaryOpKind.And => (l != 0) && (r != 0),
                BinaryOpKind.Or => (l != 0) || (r != 0),
                BinaryOpKind.Xor => ((l != 0) && (r == 0)) || ((l == 0) && (r != 0)),
                _ => 0
            };
        }

        private object PerformUnaryOp(UnaryOpKind op, object operand)
        {
            var v = Convert.ToDouble(operand ?? 0);

            return op switch
            {
                UnaryOpKind.Neg => -v,
                UnaryOpKind.Not => v == 0,
                UnaryOpKind.Inc => v + 1,
                UnaryOpKind.Dec => v - 1,
                _ => v
            };
        }

        private object GetDefaultValue(TypeInfo type)
        {
            if (type == null) return null;

            return type.Name?.ToLowerInvariant() switch
            {
                "integer" => 0,
                "long" => 0L,
                "single" => 0.0f,
                "double" => 0.0,
                "string" => "",
                "boolean" => false,
                _ => null
            };
        }

        private void OnOutput(string text)
        {
            OutputProduced?.Invoke(this, new OutputEventArgs { Text = text });
        }

        private class CallFrame
        {
            public string FunctionName { get; set; }
            public int CurrentLine { get; set; }
            public Dictionary<string, object> LocalVariables { get; set; }
        }

        private class ReturnValue
        {
            public object Value { get; set; }
        }
    }
}
