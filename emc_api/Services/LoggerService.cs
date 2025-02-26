using System.Collections.Concurrent;
using Serilog;
using ILogger=Serilog.ILogger;
namespace emc_api.Services{




public class LoggerService : ILoggerService
{
    private readonly ILogger<LoggerService> _logger;
    private readonly BlockingCollection<LogMessage> _logQueue;
    private readonly CancellationTokenSource _cancellationTokenSource;
    private readonly Task _processTask;

    public LoggerService(ILogger<LoggerService> logger)
    {
        _logger = logger;
        _logQueue = new BlockingCollection<LogMessage>();
        _cancellationTokenSource = new CancellationTokenSource();
        _processTask = Task.Run(ProcessLogQueue);
    }

    public Task LogInformationAsync(string message)
    {
        _logQueue.Add(new LogMessage(LogLevel.Information, message));
        return Task.CompletedTask;
    }

    public Task LogWarningAsync(string message)
    {
        _logQueue.Add(new LogMessage(LogLevel.Warning, message));
        return Task.CompletedTask;
    }

    public Task LogErrorAsync(string message, Exception? exception = null)
    {
        _logQueue.Add(new LogMessage(LogLevel.Error, message, exception));
        return Task.CompletedTask;
    }

    private async Task ProcessLogQueue()
    {
        try
        {
            foreach (var logMessage in _logQueue.GetConsumingEnumerable(_cancellationTokenSource.Token))
            {
                switch (logMessage.Level)
                {
                    case LogLevel.Information:
                        _logger.LogInformation(logMessage.Message);
                        break;
                    case LogLevel.Warning:
                        _logger.LogWarning(logMessage.Message);
                        break;
                    case LogLevel.Error:
                        _logger.LogError(logMessage.Exception, logMessage.Message);
                        break;
                }
                await Task.Delay(1); // 避免CPU过度使用
            }
        }
        catch (OperationCanceledException)
        {
            // 正常取消操作，不需要特殊处理
        }
    }

    public async ValueTask DisposeAsync()
    {
        _cancellationTokenSource.Cancel();
        _logQueue.CompleteAdding();
        await _processTask;
        _logQueue.Dispose();
        _cancellationTokenSource.Dispose();
    }

    private record LogMessage(LogLevel Level, string Message, Exception? Exception = null);

    private enum LogLevel
    {
        Information,
        Warning,
        Error
    }
}
}