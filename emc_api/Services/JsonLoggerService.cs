using System;
using System.Collections.Concurrent;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using emc_api.Models;


namespace emc_api.Services
{
    /// <summary>
    /// 自定义的日志服务，使用JSON格式存储日志，并按日期分文件
    /// </summary>
    public class JsonLoggerService : ILoggerService, IAsyncDisposable
    {
        private readonly string _logDirectory;
        private readonly BlockingCollection<LogEntry> _logQueue;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly Task _processTask;
        private readonly JsonSerializerOptions _jsonOptions;
        private readonly Models.LogLevel _minimumLogLevel;
        private readonly IConfiguration _configuration;

        public JsonLoggerService(IConfiguration configuration)
        {
            _configuration = configuration;
            
            // 从配置文件中读取日志目录
            _logDirectory = _configuration["JsonLogger:Directory"] ?? "logs";
            
            // 获取最小日志级别
            if (Enum.TryParse(_configuration["JsonLogger:LogLevel"], true, out Models.LogLevel logLevel))
            {
                _minimumLogLevel = logLevel;
            }
            else
            {
                _minimumLogLevel = Models.LogLevel.Debug; // 默认为Debug级别
            }
            
            // 确保日志目录存在
            Directory.CreateDirectory(_logDirectory);
            
            _logQueue = new BlockingCollection<LogEntry>();
            _cancellationTokenSource = new CancellationTokenSource();
            _jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = null,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping // 禁用Unicode转义
            };
            
            // 启动异步处理队列的任务
            _processTask = Task.Run(ProcessLogQueueAsync);
        }

        public Task LogInformationAsync(string message)
        {
            if (ShouldLog(Models.LogLevel.Information))
            {
                _logQueue.Add(CreateLogEntry(Models.LogLevel.Information, message));
            }
            return Task.CompletedTask;
        }

        public Task LogWarningAsync(string message)
        {
            if (ShouldLog(Models.LogLevel.Warning))
            {
                _logQueue.Add(CreateLogEntry(Models.LogLevel.Warning, message));
            }
            return Task.CompletedTask;
        }

        public Task LogErrorAsync(string message, Exception? exception = null)
        {
            if (ShouldLog(Models.LogLevel.Error))
            {
                _logQueue.Add(CreateLogEntry(Models.LogLevel.Error, message, exception));
            }
            return Task.CompletedTask;
        }

        public Task LogDebugAsync(string message)
        {
            if (ShouldLog(Models.LogLevel.Debug))
            {
                _logQueue.Add(CreateLogEntry(Models.LogLevel.Debug, message));
            }
            return Task.CompletedTask;
        }

        public Task LogInformationWithRequestInfoAsync(string message, RequestInfo requestInfo)
        {
            if (ShouldLog(Models.LogLevel.Information))
            {
                _logQueue.Add(CreateLogEntry(Models.LogLevel.Information, message, null, requestInfo));
            }
            return Task.CompletedTask;
        }

        public Task LogWarningWithRequestInfoAsync(string message, RequestInfo requestInfo)
        {
            if (ShouldLog(Models.LogLevel.Warning))
            {
                _logQueue.Add(CreateLogEntry(Models.LogLevel.Warning, message, null, requestInfo));
            }
            return Task.CompletedTask;
        }

        public Task LogErrorWithRequestInfoAsync(string message, Exception? exception, RequestInfo requestInfo)
        {
            if (ShouldLog(Models.LogLevel.Error))
            {
                _logQueue.Add(CreateLogEntry(Models.LogLevel.Error, message, exception, requestInfo));
            }
            return Task.CompletedTask;
        }

        public Task LogDebugWithRequestInfoAsync(string message, RequestInfo requestInfo)
        {
            if (ShouldLog(Models.LogLevel.Debug))
            {
                _logQueue.Add(CreateLogEntry(Models.LogLevel.Debug, message, null, requestInfo));
            }
            return Task.CompletedTask;
        }

        private bool ShouldLog(Models.LogLevel level)
        {
            return level >= _minimumLogLevel;
        }

        private LogEntry CreateLogEntry(Models.LogLevel level, string message, Exception? exception = null, RequestInfo? requestInfo = null)
        {
            return new LogEntry
            {
                Timestamp = DateTime.Now,
                Level = level.ToString(),
                Message = message,
                Exception = exception != null ? new ExceptionInfo
                {
                    Type = exception.GetType().FullName,
                    Message = exception.Message,
                    StackTrace = exception.StackTrace
                } : null,
                ThreadId = Thread.CurrentThread.ManagedThreadId,
                MachineName = Environment.MachineName,
                ProcessId = Environment.ProcessId,
                RequestInfo = requestInfo
            };
        }

        private async Task ProcessLogQueueAsync()
        {
            try
            {
                foreach (var logEntry in _logQueue.GetConsumingEnumerable(_cancellationTokenSource.Token))
                {
                    string fileName = Path.Combine(_logDirectory, $"{logEntry.Timestamp:yyyy-MM-dd}.json");
                    bool fileExists = File.Exists(fileName);
                    
                    // 同时输出到控制台
                    OutputToConsole(logEntry);
                    
                    try
                    {
                        List<LogEntry> entries = new List<LogEntry>();
                        
                        // 读取现有日志文件内容（如果存在）
                        if (fileExists)
                        {
                            try
                            {
                                string fileContent = await File.ReadAllTextAsync(fileName, Encoding.UTF8);
                                if (!string.IsNullOrWhiteSpace(fileContent))
                                {
                                    entries = JsonSerializer.Deserialize<List<LogEntry>>(fileContent, _jsonOptions) ?? new List<LogEntry>();
                                }
                            }
                            catch (JsonException)
                            {
                                // 如果文件内容格式不正确，创建新的日志集合
                                entries = new List<LogEntry>();
                                await LogErrorToBackupFileAsync($"日志文件 {fileName} 格式错误，创建新的日志集合");
                            }
                        }
                        
                        // 添加新日志条目
                        entries.Add(logEntry);
                        
                        // 写回文件
                        await File.WriteAllTextAsync(fileName, JsonSerializer.Serialize(entries, _jsonOptions), Encoding.UTF8);
                    }
                    catch (Exception ex)
                    {
                        // 处理写入日志文件时的异常，将异常写入备用日志文件
                        await LogErrorToBackupFileAsync($"写入日志时出错: {ex.Message}\n{ex.StackTrace}");
                    }
                    
                    await Task.Delay(1); // 避免CPU过度使用
                }
            }
            catch (OperationCanceledException)
            {
                // 正常取消操作，不需要特殊处理
            }
            catch (Exception ex)
            {
                // 处理其他异常
                await LogErrorToBackupFileAsync($"日志处理过程中发生严重错误: {ex.Message}\n{ex.StackTrace}");
            }
        }

        // 添加输出到控制台的辅助方法
        private void OutputToConsole(LogEntry logEntry)
        {
            // 保存当前控制台颜色
            ConsoleColor originalColor = Console.ForegroundColor;
            
            // 根据日志级别设置不同的颜色
            ConsoleColor logColor = logEntry.Level switch
            {
                "Debug" => ConsoleColor.Gray,
                "Information" => ConsoleColor.Green,
                "Warning" => ConsoleColor.Yellow,
                "Error" => ConsoleColor.Red,
                _ => originalColor
            };
            
            Console.ForegroundColor = logColor;
            
            // 格式化日志内容
            string consoleMessage = $"[{logEntry.Timestamp:yyyy-MM-dd HH:mm:ss}] [{logEntry.Level}] {logEntry.Message}";
            
            // 如果有异常信息，添加到输出
            if (logEntry.Exception != null)
            {
                consoleMessage += $"\n异常: {logEntry.Exception.Type}\n消息: {logEntry.Exception.Message}\n堆栈: {logEntry.Exception.StackTrace}";
            }
            
            // 输出到控制台
            Console.WriteLine(consoleMessage);
            
            // 恢复控制台颜色
            Console.ForegroundColor = originalColor;
        }

        private async Task LogErrorToBackupFileAsync(string errorMessage)
        {
            string errorLogFile = Path.Combine(_logDirectory, "logger_errors.txt");
            try
            {
                await File.AppendAllTextAsync(errorLogFile, 
                    $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - {errorMessage}\n\n", Encoding.UTF8);
            
                // 同时输出到控制台
                ConsoleColor originalColor = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [LOGGER_ERROR] {errorMessage}");
                Console.ForegroundColor = originalColor;
            }
            catch
            {
                // 如果连备用日志文件也无法写入，则至少尝试输出到控制台
                try 
                {
                    ConsoleColor originalColor = Console.ForegroundColor;
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [CRITICAL_ERROR] 无法写入日志: {errorMessage}");
                    Console.ForegroundColor = originalColor;
                }
                catch
                {
                    // 如果连控制台也无法输出，则忽略错误
                }
            }
        }

        public async ValueTask DisposeAsync()
        {
            _cancellationTokenSource.Cancel();
            _logQueue.CompleteAdding();
            
            try
            {
                await _processTask;
            }
            catch (Exception)
            {
                // 忽略任务取消异常
            }
            
            _logQueue.Dispose();
            _cancellationTokenSource.Dispose();
        }
    }

} 