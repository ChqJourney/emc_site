using emc_api.Repositories;
using Serilog;
using emc_api.Models;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using Swashbuckle.AspNetCore.SwaggerUI;
using System.Net;
using System.Net.Sockets;
using emc_api.Services;
using emc_api.Middleware;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Data.Sqlite;
using emc_api.Data;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .WriteTo.Async(a=>a.Console())
    .WriteTo.Async(a=>a.File("logs/emc-.log", rollingInterval: RollingInterval.Day,outputTemplate:"{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}"))
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container.
builder.Services.AddControllers(); 
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Version = "v1",
        Title = "EMC Reservation API",
        Description = "An ASP.NET Core Web API for managing EMC station reservations",
        Contact = new Microsoft.OpenApi.Models.OpenApiContact
        {
            Name = "EMC Team",
            Email = "patrick.chen@intertek.com"
        }
    });
});
builder.Services.ConfigureHttpJsonOptions(options => {
    options.SerializerOptions.WriteIndented = true;
    options.SerializerOptions.IncludeFields = true;
});
// Add CORS support
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", builder =>
    {
        builder.AllowAnyOrigin()
               .AllowAnyMethod()
               .AllowAnyHeader();
    });
});

// Configure SQLite connection
builder.Configuration.AddJsonFile("appsettings.json", optional: false);
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
var dir=builder.Configuration.GetValue<string>("data:dir");
Console.WriteLine(dir);
if (string.IsNullOrEmpty(connectionString))
{
    throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
}

// 从配置中读取连接字符串（在 appsettings.json 中添加 DefaultConnection 字段）
builder.Services.AddScoped<SqliteConnection>(_ =>
    new SqliteConnection(connectionString)
);
// Initialize database
// Register repositories
builder.Services.AddSingleton<ILoggerService, LoggerService>();
builder.Services.AddScoped<IUserRepository,UserRepository>();
builder.Services.AddScoped<IBizRepository,BizRepository>();
var databaseInitializer = new DatabaseInitializer(new SqliteConnection(connectionString));
await databaseInitializer.InitializeAsync();
// 注册自定义认证方案
builder.Services.AddAuthentication("HeaderScheme")
    .AddScheme<AuthenticationSchemeOptions, HeaderAuthenticationHandler>("HeaderScheme", options => { });
// 定义基于角色/策略的授权
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminPolicy", policy => policy.RequireRole("Admin"));
    options.AddPolicy("EngineerPolicy", policy => policy.RequireRole("Engineer"));
});

// Configure Kestrel
builder.WebHost.ConfigureKestrel(options => {
    options.Limits.MaxConcurrentConnections = 100;
    options.Limits.MaxConcurrentUpgradedConnections = 100;
    options.Limits.MaxRequestBodySize = 10 * 1024;
    options.Limits.MinRequestBodyDataRate = null;
    options.Limits.MinResponseDataRate = null;
    options.Limits.KeepAliveTimeout = TimeSpan.FromMinutes(2);
    options.Limits.RequestHeadersTimeout = TimeSpan.FromMinutes(1);
});
var app = builder.Build();
// Add global exception handling middleware
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseAuthentication();
app.UseAuthorization();
// Configure the HTTP request pipeline.
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "EMC Reservation API v1");
    options.RoutePrefix = "swagger";
});

// Enable CORS
app.UseCors("AllowAll");

app.UseSerilogRequestLogging();

// 配置静态文件和SPA fallback
app.UseDefaultFiles();
app.UseStaticFiles();
app.MapFallbackToFile("index.html");

// 获取局域网IP和server port，并写入portal.txt
var hostEntry = Dns.GetHostEntry(Dns.GetHostName());
var localIp = hostEntry.AddressList.FirstOrDefault(a => a.AddressFamily == AddressFamily.InterNetwork)?.ToString() ?? "127.0.0.1";
var port = app.Urls.FirstOrDefault() is string url ? new Uri(url).Port : 5000;
var portalUrl = $"http://{localIp}:{port}";
System.IO.File.WriteAllText(Path.Combine(dir,"portal.txt"), portalUrl);
Log.Logger.Information("Server will started at {PortalUrl}", portalUrl);

// 使用控制器路由
app.MapControllers();

// Run the application
app.Run("http://0.0.0.0:5000");
