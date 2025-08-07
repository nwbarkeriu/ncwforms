using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

// Add request logging middleware
app.Use(async (context, next) =>
{
    var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
    var userAgent = context.Request.Headers["User-Agent"].FirstOrDefault() ?? "Unknown";
    var ip = context.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
    var path = context.Request.Path.Value ?? "/";
    
    // Only log actual page visits (not static files, SignalR, security probes, etc.)
    if (!path.StartsWith("/_blazor") &&
        !path.StartsWith("/_framework") &&
        !path.StartsWith("/css") &&
        !path.StartsWith("/js") &&
        !path.StartsWith("/lib") &&
        !path.Contains(".css") &&
        !path.Contains(".js") &&
        !path.Contains(".png") &&
        !path.Contains(".ico") &&
        !path.Contains("favicon") &&
        !IsSecurityProbe(path))
    {
        var logMessage = $"VISIT: {DateTime.Now:yyyy-MM-dd HH:mm:ss} | IP: {ip} | Path: {path} | Browser: {ExtractBrowser(userAgent)}";
        
        // Log to console (captured by systemd)
        logger.LogInformation(logMessage);
        
        // Also write to file in production
        if (!app.Environment.IsDevelopment())
        {
            await WriteToLogFileAsync(logMessage);
        }
    }
    else if (IsSecurityProbe(path))
    {
        // Log security probes separately
        var probeMessage = $"PROBE: {DateTime.Now:yyyy-MM-dd HH:mm:ss} | IP: {ip} | Path: {path} | Browser: {ExtractBrowser(userAgent)}";
        logger.LogWarning(probeMessage);
        
        if (!app.Environment.IsDevelopment())
        {
            await WriteToSecurityLogAsync(probeMessage);
        }
    }
    
    await next();
});

// Helper method for browser detection
static string ExtractBrowser(string userAgent)
{
    if (string.IsNullOrEmpty(userAgent)) return "Unknown";
    if (userAgent.Contains("Chrome") && !userAgent.Contains("Edge")) return "Chrome";
    if (userAgent.Contains("Firefox")) return "Firefox";
    if (userAgent.Contains("Safari") && !userAgent.Contains("Chrome")) return "Safari";
    if (userAgent.Contains("Edge")) return "Edge";
    return "Other";
}

// Helper method to detect security probes
static bool IsSecurityProbe(string path)
{
    var probePaths = new[]
    {
        ".php", ".env", ".yml", ".properties", ".xml", ".config",
        "phpinfo", "api/", "admin/", "wp-", "test.", "backup",
        ".git", ".svn", "config.", "application."
    };
    
    return probePaths.Any(probe => path.Contains(probe, StringComparison.OrdinalIgnoreCase));
}

// Helper method to write to log file
static async Task WriteToLogFileAsync(string message)
{
    try
    {
        var logsDir = "/var/www/ncwforms/logs";
        Directory.CreateDirectory(logsDir);
        var logFile = Path.Combine(logsDir, $"visits-{DateTime.Now:yyyy-MM-dd}.log");
        await File.AppendAllTextAsync(logFile, message + Environment.NewLine);
    }
    catch
    {
        // Silently continue if file logging fails
    }
}

// Helper method to write to security log file
static async Task WriteToSecurityLogAsync(string message)
{
    try
    {
        var logsDir = "/var/www/ncwforms/logs";
        Directory.CreateDirectory(logsDir);
        var logFile = Path.Combine(logsDir, $"security-{DateTime.Now:yyyy-MM-dd}.log");
        await File.AppendAllTextAsync(logFile, message + Environment.NewLine);
    }
    catch
    {
        // Silently continue if file logging fails
    }
}

app.UseStaticFiles();

app.UseRouting();

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();
