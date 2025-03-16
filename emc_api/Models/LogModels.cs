using System;

namespace emc_api.Models
{
    /// <summary>
    /// 日志级别枚举
    /// </summary>
    public enum LogLevel
    {
        Debug,
        Information,
        Warning,
        Error
    }

    /// <summary>
    /// 日志条目模型
    /// </summary>
    public class LogEntry
    {
        public DateTime Timestamp { get; set; }
        public string Level { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public ExceptionInfo? Exception { get; set; }
        public int ThreadId { get; set; }
        public string MachineName { get; set; } = string.Empty;
        public int ProcessId { get; set; }
        public RequestInfo? RequestInfo { get; set; }
    }

    /// <summary>
    /// 请求信息模型
    /// </summary>
    public class RequestInfo
    {
        public string? TraceId { get; set; }
        public string? Method { get; set; }
        public string? Path { get; set; }
        public string? QueryString { get; set; }
        public string? ClientIp { get; set; }
        public string? UserAgent { get; set; }
    }

    /// <summary>
    /// 异常信息模型
    /// </summary>
    public class ExceptionInfo
    {
        public string? Type { get; set; }
        public string? Message { get; set; }
        public string? StackTrace { get; set; }
    }
} 