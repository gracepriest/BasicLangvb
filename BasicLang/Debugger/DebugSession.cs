using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using BasicLang.Compiler;
using BasicLang.Compiler.AST;
using BasicLang.Compiler.IR;
using BasicLang.Compiler.SemanticAnalysis;

namespace BasicLang.Debugger
{
    /// <summary>
    /// Debug Adapter Protocol session handler
    /// </summary>
    public class DebugSession
    {
        private readonly Stream _input;
        private readonly Stream _output;
        private readonly Dictionary<string, HashSet<int>> _breakpoints = new();
        private readonly Dictionary<int, VariableInfo> _variables = new();
        private readonly List<StackFrameInfo> _stackFrames = new();

        private DebuggableInterpreter _interpreter;
        private string _currentFile;
        private bool _running;
        private bool _stopOnEntry;
        private int _nextVariableRef = 1;
        private int _seq = 0;

        public DebugSession(Stream input, Stream output)
        {
            _input = input;
            _output = output;
        }

        public async Task RunAsync()
        {
            var buffer = new StringBuilder();
            var reader = new StreamReader(_input, Encoding.UTF8);

            while (true)
            {
                var line = await reader.ReadLineAsync();
                if (line == null) break;

                if (line.StartsWith("Content-Length:"))
                {
                    var length = int.Parse(line.Substring(15).Trim());
                    await reader.ReadLineAsync(); // Empty line

                    var chars = new char[length];
                    await reader.ReadBlockAsync(chars, 0, length);
                    var content = new string(chars);

                    await HandleMessageAsync(content);
                }
            }
        }

        private async Task HandleMessageAsync(string content)
        {
            try
            {
                var message = JsonSerializer.Deserialize<DAPMessage>(content);
                if (message == null) return;

                DAPResponse response = message.Command switch
                {
                    "initialize" => HandleInitialize(message),
                    "launch" => await HandleLaunchAsync(message),
                    "setBreakpoints" => HandleSetBreakpoints(message),
                    "configurationDone" => HandleConfigurationDone(message),
                    "threads" => HandleThreads(message),
                    "stackTrace" => HandleStackTrace(message),
                    "scopes" => HandleScopes(message),
                    "variables" => HandleVariables(message),
                    "continue" => HandleContinue(message),
                    "next" => HandleNext(message),
                    "stepIn" => HandleStepIn(message),
                    "stepOut" => HandleStepOut(message),
                    "pause" => HandlePause(message),
                    "disconnect" => HandleDisconnect(message),
                    "evaluate" => HandleEvaluate(message),
                    _ => CreateResponse(message, true)
                };

                await SendResponseAsync(response);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Debug session error: {ex.Message}");
            }
        }

        private DAPResponse HandleInitialize(DAPMessage request)
        {
            var response = CreateResponse(request, true);
            response.Body = new Dictionary<string, object>
            {
                ["supportsConfigurationDoneRequest"] = true,
                ["supportsFunctionBreakpoints"] = false,
                ["supportsConditionalBreakpoints"] = false,
                ["supportsEvaluateForHovers"] = true,
                ["supportsStepBack"] = false,
                ["supportsSetVariable"] = false,
                ["supportsRestartFrame"] = false,
                ["supportsGotoTargetsRequest"] = false,
                ["supportsStepInTargetsRequest"] = false,
                ["supportsCompletionsRequest"] = false,
                ["supportsModulesRequest"] = false,
                ["supportsExceptionOptions"] = false,
                ["supportsValueFormattingOptions"] = false,
                ["supportsExceptionInfoRequest"] = false,
                ["supportTerminateDebuggee"] = true,
                ["supportsDelayedStackTraceLoading"] = false,
                ["supportsLoadedSourcesRequest"] = false
            };

            // Send initialized event
            Task.Run(async () =>
            {
                await Task.Delay(100);
                await SendEventAsync("initialized", null);
            });

            return response;
        }

        private async Task<DAPResponse> HandleLaunchAsync(DAPMessage request)
        {
            var args = request.Arguments;

            if (args.TryGetProperty("program", out var programProp))
            {
                _currentFile = programProp.GetString();
            }

            if (args.TryGetProperty("stopOnEntry", out var stopProp))
            {
                _stopOnEntry = stopProp.GetBoolean();
            }

            // Compile and prepare the program
            if (!string.IsNullOrEmpty(_currentFile) && File.Exists(_currentFile))
            {
                try
                {
                    var source = await File.ReadAllTextAsync(_currentFile);
                    var lexer = new Lexer(source);
                    var tokens = lexer.Tokenize();
                    var parser = new Parser(tokens);
                    var ast = parser.Parse();

                    var semanticAnalyzer = new SemanticAnalyzer();
                    semanticAnalyzer.Analyze(ast);

                    var irBuilder = new IRBuilder(semanticAnalyzer);
                    var module = irBuilder.Build(ast);

                    _interpreter = new DebuggableInterpreter(module);
                    _interpreter.BreakpointHit += OnBreakpointHit;
                    _interpreter.StepComplete += OnStepComplete;
                    _interpreter.OutputProduced += OnOutputProduced;

                    // Set breakpoints
                    foreach (var bp in _breakpoints)
                    {
                        foreach (var line in bp.Value)
                        {
                            _interpreter.SetBreakpoint(bp.Key, line);
                        }
                    }

                    _running = true;

                    if (_stopOnEntry)
                    {
                        await SendEventAsync("stopped", new Dictionary<string, object>
                        {
                            ["reason"] = "entry",
                            ["threadId"] = 1
                        });
                    }
                }
                catch (Exception ex)
                {
                    await SendEventAsync("output", new Dictionary<string, object>
                    {
                        ["category"] = "stderr",
                        ["output"] = $"Error: {ex.Message}\n"
                    });
                }
            }

            return CreateResponse(request, true);
        }

        private DAPResponse HandleSetBreakpoints(DAPMessage request)
        {
            var args = request.Arguments;
            var breakpoints = new List<object>();

            if (args.TryGetProperty("source", out var source) &&
                source.TryGetProperty("path", out var pathProp))
            {
                var path = pathProp.GetString();
                _breakpoints[path] = new HashSet<int>();

                if (args.TryGetProperty("breakpoints", out var bpArray))
                {
                    foreach (var bp in bpArray.EnumerateArray())
                    {
                        if (bp.TryGetProperty("line", out var lineProp))
                        {
                            var line = lineProp.GetInt32();
                            _breakpoints[path].Add(line);

                            breakpoints.Add(new Dictionary<string, object>
                            {
                                ["verified"] = true,
                                ["line"] = line
                            });

                            _interpreter?.SetBreakpoint(path, line);
                        }
                    }
                }
            }

            var response = CreateResponse(request, true);
            response.Body = new Dictionary<string, object>
            {
                ["breakpoints"] = breakpoints
            };
            return response;
        }

        private DAPResponse HandleConfigurationDone(DAPMessage request)
        {
            // Start execution if not stopping on entry
            if (!_stopOnEntry && _interpreter != null)
            {
                Task.Run(() => _interpreter.Run());
            }
            return CreateResponse(request, true);
        }

        private DAPResponse HandleThreads(DAPMessage request)
        {
            var response = CreateResponse(request, true);
            response.Body = new Dictionary<string, object>
            {
                ["threads"] = new[]
                {
                    new Dictionary<string, object>
                    {
                        ["id"] = 1,
                        ["name"] = "Main Thread"
                    }
                }
            };
            return response;
        }

        private DAPResponse HandleStackTrace(DAPMessage request)
        {
            var frames = _interpreter?.GetStackFrames() ?? new List<StackFrameInfo>();
            var stackFrames = new List<object>();

            for (int i = 0; i < frames.Count; i++)
            {
                var frame = frames[i];
                stackFrames.Add(new Dictionary<string, object>
                {
                    ["id"] = i,
                    ["name"] = frame.FunctionName,
                    ["source"] = new Dictionary<string, object>
                    {
                        ["path"] = _currentFile,
                        ["name"] = Path.GetFileName(_currentFile)
                    },
                    ["line"] = frame.Line,
                    ["column"] = 1
                });
            }

            var response = CreateResponse(request, true);
            response.Body = new Dictionary<string, object>
            {
                ["stackFrames"] = stackFrames,
                ["totalFrames"] = stackFrames.Count
            };
            return response;
        }

        private DAPResponse HandleScopes(DAPMessage request)
        {
            var args = request.Arguments;
            var frameId = 0;
            if (args.TryGetProperty("frameId", out var frameProp))
            {
                frameId = frameProp.GetInt32();
            }

            var localVarRef = _nextVariableRef++;
            _variables[localVarRef] = new VariableInfo { FrameId = frameId, Type = "locals" };

            var response = CreateResponse(request, true);
            response.Body = new Dictionary<string, object>
            {
                ["scopes"] = new[]
                {
                    new Dictionary<string, object>
                    {
                        ["name"] = "Locals",
                        ["variablesReference"] = localVarRef,
                        ["expensive"] = false
                    }
                }
            };
            return response;
        }

        private DAPResponse HandleVariables(DAPMessage request)
        {
            var args = request.Arguments;
            var varRef = 0;
            if (args.TryGetProperty("variablesReference", out var refProp))
            {
                varRef = refProp.GetInt32();
            }

            var variables = new List<object>();

            if (_variables.TryGetValue(varRef, out var varInfo) && _interpreter != null)
            {
                var locals = _interpreter.GetLocalVariables(varInfo.FrameId);
                foreach (var local in locals)
                {
                    variables.Add(new Dictionary<string, object>
                    {
                        ["name"] = local.Key,
                        ["value"] = local.Value?.ToString() ?? "Nothing",
                        ["type"] = local.Value?.GetType().Name ?? "Object",
                        ["variablesReference"] = 0
                    });
                }
            }

            var response = CreateResponse(request, true);
            response.Body = new Dictionary<string, object>
            {
                ["variables"] = variables
            };
            return response;
        }

        private DAPResponse HandleContinue(DAPMessage request)
        {
            Task.Run(() => _interpreter?.Continue());
            var response = CreateResponse(request, true);
            response.Body = new Dictionary<string, object>
            {
                ["allThreadsContinued"] = true
            };
            return response;
        }

        private DAPResponse HandleNext(DAPMessage request)
        {
            Task.Run(() => _interpreter?.StepOver());
            return CreateResponse(request, true);
        }

        private DAPResponse HandleStepIn(DAPMessage request)
        {
            Task.Run(() => _interpreter?.StepInto());
            return CreateResponse(request, true);
        }

        private DAPResponse HandleStepOut(DAPMessage request)
        {
            Task.Run(() => _interpreter?.StepOut());
            return CreateResponse(request, true);
        }

        private DAPResponse HandlePause(DAPMessage request)
        {
            _interpreter?.Pause();
            return CreateResponse(request, true);
        }

        private DAPResponse HandleDisconnect(DAPMessage request)
        {
            _running = false;
            _interpreter?.Stop();
            return CreateResponse(request, true);
        }

        private DAPResponse HandleEvaluate(DAPMessage request)
        {
            var args = request.Arguments;
            var expression = "";
            if (args.TryGetProperty("expression", out var exprProp))
            {
                expression = exprProp.GetString();
            }

            var result = _interpreter?.EvaluateExpression(expression);

            var response = CreateResponse(request, true);
            response.Body = new Dictionary<string, object>
            {
                ["result"] = result?.ToString() ?? "undefined",
                ["variablesReference"] = 0
            };
            return response;
        }

        private void OnBreakpointHit(object sender, DebugEventArgs e)
        {
            Task.Run(() => SendEventAsync("stopped", new Dictionary<string, object>
            {
                ["reason"] = "breakpoint",
                ["threadId"] = 1,
                ["allThreadsStopped"] = true
            }));
        }

        private void OnStepComplete(object sender, DebugEventArgs e)
        {
            Task.Run(() => SendEventAsync("stopped", new Dictionary<string, object>
            {
                ["reason"] = "step",
                ["threadId"] = 1,
                ["allThreadsStopped"] = true
            }));
        }

        private void OnOutputProduced(object sender, OutputEventArgs e)
        {
            Task.Run(() => SendEventAsync("output", new Dictionary<string, object>
            {
                ["category"] = "stdout",
                ["output"] = e.Text
            }));
        }

        private DAPResponse CreateResponse(DAPMessage request, bool success)
        {
            return new DAPResponse
            {
                Seq = ++_seq,
                Type = "response",
                RequestSeq = request.Seq,
                Success = success,
                Command = request.Command
            };
        }

        private async Task SendResponseAsync(DAPResponse response)
        {
            var json = JsonSerializer.Serialize(response, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            });
            await SendMessageAsync(json);
        }

        private async Task SendEventAsync(string eventName, Dictionary<string, object> body)
        {
            var evt = new DAPEvent
            {
                Seq = ++_seq,
                Type = "event",
                Event = eventName,
                Body = body
            };
            var json = JsonSerializer.Serialize(evt, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            });
            await SendMessageAsync(json);
        }

        private async Task SendMessageAsync(string json)
        {
            var bytes = Encoding.UTF8.GetBytes(json);
            var header = $"Content-Length: {bytes.Length}\r\n\r\n";
            var headerBytes = Encoding.UTF8.GetBytes(header);

            await _output.WriteAsync(headerBytes, 0, headerBytes.Length);
            await _output.WriteAsync(bytes, 0, bytes.Length);
            await _output.FlushAsync();
        }
    }

    public class DAPMessage
    {
        [JsonPropertyName("seq")]
        public int Seq { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("command")]
        public string Command { get; set; }

        [JsonPropertyName("arguments")]
        public JsonElement Arguments { get; set; }
    }

    public class DAPResponse
    {
        [JsonPropertyName("seq")]
        public int Seq { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("request_seq")]
        public int RequestSeq { get; set; }

        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("command")]
        public string Command { get; set; }

        [JsonPropertyName("body")]
        public object Body { get; set; }
    }

    public class DAPEvent
    {
        [JsonPropertyName("seq")]
        public int Seq { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("event")]
        public string Event { get; set; }

        [JsonPropertyName("body")]
        public object Body { get; set; }
    }

    public class VariableInfo
    {
        public int FrameId { get; set; }
        public string Type { get; set; }
    }

    public class StackFrameInfo
    {
        public string FunctionName { get; set; }
        public int Line { get; set; }
    }

    public class DebugEventArgs : EventArgs
    {
        public int Line { get; set; }
        public string File { get; set; }
    }

    public class OutputEventArgs : EventArgs
    {
        public string Text { get; set; }
    }
}
