using emc_api.Repositories;
using Serilog;
using System.Net;
using System.Net.Sockets;
using emc_api.Services;
using emc_api.Middleware;
using Microsoft.Data.Sqlite;
using emc_api.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .WriteTo.Async(a => a.Console())
    .WriteTo.Async(a => a.File("logs/emc-.log", rollingInterval: RollingInterval.Day, outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}"))
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
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.WriteIndented = true;
    options.SerializerOptions.IncludeFields = true;
});
// Add CORS support
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", builder =>
    {
        builder.WithOrigins("http://localhost:1420")
            .AllowAnyMethod()
            .AllowAnyHeader()
            .WithExposedHeaders("Authorization")
            .AllowCredentials()
            .SetPreflightMaxAge(TimeSpan.FromMinutes(10)); // 缓存预检请求结果
    });
});

// Configure SQLite connection
builder.Configuration.AddJsonFile("appsettings.json", optional: false);
var userDbStr = builder.Configuration.GetConnectionString("UserConnection");
var bizDbStr = builder.Configuration.GetConnectionString("BizConnection");
var dir = builder.Configuration.GetValue<string>("data:dir")?.Replace("\\", Path.DirectorySeparatorChar.ToString());
Console.WriteLine(dir);



// Initialize database
// Register repositories
builder.Services.AddSingleton<ILoggerService, LoggerService>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IBizRepository, BizRepository>();
var userDatabaseInitializer = new DatabaseInitializer(new SqliteConnection(userDbStr), "User");
await userDatabaseInitializer.InitializeAsync();
var bizDatabaseInitializer = new DatabaseInitializer(new SqliteConnection(bizDbStr), "Biz");
await bizDatabaseInitializer.InitializeAsync();
// 注册自定义认证方案
var time = builder.Configuration.GetValue<string>("jwt:AccessTokenExpirationMinutes");
Console.WriteLine(time);
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(o =>
{
    o.TokenValidationParameters = new TokenValidationParameters
    {
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey
            (Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"])),
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ClockSkew = TimeSpan.Zero
    };
});

// builder.Services.AddAuthentication("HeaderScheme")
//     .AddScheme<AuthenticationSchemeOptions, HeaderAuthenticationHandler>("HeaderScheme", options => { });
// 定义基于角色/策略的授权
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminPolicy", policy => policy.RequireRole("Admin"));
    options.AddPolicy("EngineerPolicy", policy => policy.RequireRole("Engineer"));
});

// Configure Kestrel
builder.WebHost.ConfigureKestrel(options =>
{
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
// 配置静态文件和SPA fallback
app.UseCors("AllowAll");
app.UseDefaultFiles();
app.UseStaticFiles();
app.MapFallbackToFile("index.html");
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseAuthentication();
app.UseAuthorization();
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "EMC Reservation API v1");
    options.RoutePrefix = "swagger";
});

// Enable CORS

app.UseSerilogRequestLogging();


// 获取局域网IP和server port，并写入portal.txt
var hostEntry = Dns.GetHostEntry(Dns.GetHostName());
var localIp = hostEntry.AddressList.FirstOrDefault(a => a.AddressFamily == AddressFamily.InterNetwork)?.ToString() ?? "127.0.0.1";
var port = app.Urls.FirstOrDefault() is string url ? new Uri(url).Port : 5001;
var portalUrl = $"http://{localIp}:{port}";
var portalPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "portal.txt");
System.IO.File.WriteAllText(portalPath, portalUrl);
Log.Logger.Information("Server will started at {PortalUrl}", portalUrl);

// 使用控制器路由
app.MapControllers();

// Run the application
app.Run("http://0.0.0.0:5001");
