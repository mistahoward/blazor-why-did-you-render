using System.Text.Json;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Blazor.WhyDidYouRender.Diagnostics;

/// <summary>
/// Provides diagnostic endpoints for viewing error tracking information.
/// </summary>
public static class ErrorDiagnosticsEndpoint {
    /// <summary>
    /// Maps error diagnostics endpoints to the application.
    /// </summary>
    /// <param name="app">The web application builder.</param>
    /// <param name="basePath">The base path for diagnostics endpoints (default: "/_whydidyourender").</param>
    /// <returns>The web application for chaining.</returns>
    public static WebApplication MapErrorDiagnostics(this WebApplication app, string basePath = "/_whydidyourender") {
        if (!app.Environment.IsDevelopment()) {
            return app;
        }

        var group = app.MapGroup(basePath);

        group.MapGet("/errors/stats", async (HttpContext context) => {
            var errorTracker = context.RequestServices.GetService<IErrorTracker>();
            if (errorTracker == null) {
                await context.Response.WriteAsync("Error tracking not enabled");
                return;
            }

            var stats = errorTracker.GetErrorStatistics();
            var json = JsonSerializer.Serialize(stats, new JsonSerializerOptions {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(json);
        });

        group.MapGet("/errors/recent", async (HttpContext context) => {
            var errorTracker = context.RequestServices.GetService<IErrorTracker>();
            if (errorTracker == null) {
                await context.Response.WriteAsync("Error tracking not enabled");
                return;
            }

            var countParam = context.Request.Query["count"].FirstOrDefault();
            var count = int.TryParse(countParam, out var c) ? c : 50;

            var errors = errorTracker.GetRecentErrors(count);
            var json = JsonSerializer.Serialize(errors, new JsonSerializerOptions {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(json);
        });

        group.MapGet("/errors", async (HttpContext context) => {
            var html = GenerateErrorDashboardHtml(basePath);
            context.Response.ContentType = "text/html";
            await context.Response.WriteAsync(html);
        });

        group.MapPost("/errors/clear", async (HttpContext context) => {
            var errorTracker = context.RequestServices.GetService<IErrorTracker>();
            if (errorTracker == null) {
                await context.Response.WriteAsync("Error tracking not enabled");
                return;
            }

            var hoursParam = context.Request.Query["hours"].FirstOrDefault();
            var hours = int.TryParse(hoursParam, out var h) ? h : 24;

            errorTracker.ClearOldErrors(TimeSpan.FromHours(hours));
            await context.Response.WriteAsync($"Cleared errors older than {hours} hours");
        });

        return app;
    }

    /// <summary>
    /// Generates the HTML for the error dashboard.
    /// </summary>
    /// <param name="basePath">The base path for API endpoints.</param>
    /// <returns>The HTML content for the error dashboard.</returns>
    private static string GenerateErrorDashboardHtml(string basePath) {
        return @"<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>WhyDidYouRender - Error Diagnostics</title>
    <style>
        body { font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif; margin: 0; padding: 20px; background: #f5f5f5; }
        .container { max-width: 1200px; margin: 0 auto; }
        .header { background: white; padding: 20px; border-radius: 8px; margin-bottom: 20px; box-shadow: 0 2px 4px rgba(0,0,0,0.1); }
        .stats { display: grid; grid-template-columns: repeat(auto-fit, minmax(200px, 1fr)); gap: 15px; margin-bottom: 20px; }
        .stat-card { background: white; padding: 15px; border-radius: 8px; box-shadow: 0 2px 4px rgba(0,0,0,0.1); }
        .stat-value { font-size: 24px; font-weight: bold; color: #e74c3c; }
        .stat-label { color: #666; font-size: 14px; }
        .errors-section { background: white; padding: 20px; border-radius: 8px; box-shadow: 0 2px 4px rgba(0,0,0,0.1); }
        .error-item { border-bottom: 1px solid #eee; padding: 15px 0; }
        .error-item:last-child { border-bottom: none; }
        .error-header { display: flex; justify-content: space-between; align-items: center; margin-bottom: 8px; }
        .error-id { font-family: monospace; background: #f8f9fa; padding: 2px 6px; border-radius: 4px; font-size: 12px; }
        .error-severity { padding: 2px 8px; border-radius: 12px; font-size: 12px; font-weight: bold; }
        .severity-error { background: #fee; color: #c53030; }
        .severity-warning { background: #fffbeb; color: #d69e2e; }
        .severity-info { background: #ebf8ff; color: #3182ce; }
        .severity-critical { background: #fed7d7; color: #e53e3e; }
        .error-message { font-weight: 500; margin-bottom: 5px; }
        .error-details { font-size: 14px; color: #666; }
        .refresh-btn { background: #3182ce; color: white; border: none; padding: 8px 16px; border-radius: 4px; cursor: pointer; }
        .refresh-btn:hover { background: #2c5aa0; }
        .loading { text-align: center; padding: 40px; color: #666; }
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <h1>🔍 WhyDidYouRender - Error Diagnostics</h1>
            <p>Monitor and diagnose errors in render tracking operations</p>
            <button class=""refresh-btn"" onclick=""loadData()"">🔄 Refresh</button>
        </div>

        <div class=""stats"" id=""stats"">
            <div class=""loading"">Loading statistics...</div>
        </div>

        <div class=""errors-section"">
            <h2>Recent Errors</h2>
            <div id=""errors"">
                <div class=""loading"">Loading errors...</div>
            </div>
        </div>
    </div>

    <script>
        async function loadStats() {
            try {
                console.log('Fetching stats from: " + basePath + @"/errors/stats');
                const response = await fetch('" + basePath + @"/errors/stats');

                if (!response.ok) {
                    throw new Error(`HTTP ${response.status}: ${response.statusText}`);
                }

                const stats = await response.json();
                console.log('Received stats:', stats);

                const totalErrors = stats.totalErrors || stats.TotalErrors || 0;
                const errorsLastHour = stats.errorsLastHour || stats.ErrorsLastHour || 0;
                const errorsLast24Hours = stats.errorsLast24Hours || stats.ErrorsLast24Hours || 0;
                const errorRate = stats.errorRate || stats.ErrorRate || 0;

                document.getElementById('stats').innerHTML = `
                    <div class=""stat-card"">
                        <div class=""stat-value"">${totalErrors}</div>
                        <div class=""stat-label"">Total Errors</div>
                    </div>
                    <div class=""stat-card"">
                        <div class=""stat-value"">${errorsLastHour}</div>
                        <div class=""stat-label"">Last Hour</div>
                    </div>
                    <div class=""stat-card"">
                        <div class=""stat-value"">${errorsLast24Hours}</div>
                        <div class=""stat-label"">Last 24 Hours</div>
                    </div>
                    <div class=""stat-card"">
                        <div class=""stat-value"">${errorRate.toFixed(2)}</div>
                        <div class=""stat-label"">Errors/Min</div>
                    </div>
                `;
            } catch (error) {
                console.error('Error loading stats:', error);
                document.getElementById('stats').innerHTML = `<div class=""stat-card"">Error loading statistics: ${error.message}</div>`;
            }
        }

        async function loadErrors() {
            try {
                console.log('Fetching errors from: " + basePath + @"/errors/recent?count=20');
                const response = await fetch('" + basePath + @"/errors/recent?count=20');

                if (!response.ok) {
                    throw new Error(`HTTP ${response.status}: ${response.statusText}`);
                }

                const errors = await response.json();
                console.log('Received errors:', errors);

                if (errors.length > 0) {
                    console.log('First error structure:', errors[0]);
                    console.log('Severity value:', errors[0].severity || errors[0].Severity);
                    console.log('Severity type:', typeof (errors[0].severity || errors[0].Severity));
                }

                if (!Array.isArray(errors)) {
                    throw new Error('Expected array of errors, got: ' + typeof errors);
                }

                if (errors.length === 0) {
                    document.getElementById('errors').innerHTML = '<div class=""loading"">No errors found</div>';
                    return;
                }

                const errorsHtml = errors.map(error => {
                    const errorId = error.errorId || error.ErrorId || 'unknown';

                    let severity = error.severity || error.Severity || 'unknown';
                    if (typeof severity === 'number') {
                        const severityNames = ['info', 'warning', 'error', 'critical'];
                        severity = severityNames[severity] || 'unknown';
                    }
                    severity = String(severity).toLowerCase();

                    const message = error.message || error.Message || 'No message';
                    const timestamp = error.timestamp || error.Timestamp;
                    const componentName = error.componentName || error.ComponentName;
                    const trackingMethod = error.trackingMethod || error.TrackingMethod;
                    const exceptionType = error.exceptionType || error.ExceptionType;

                    return `
                        <div class=""error-item"">
                            <div class=""error-header"">
                                <span class=""error-id"">${errorId}</span>
                                <span class=""error-severity severity-${severity}"">${severity.toUpperCase()}</span>
                                <span style=""margin-left: auto; font-size: 12px; color: #666;"">
                                    ${timestamp ? new Date(timestamp).toLocaleString() : 'Unknown time'}
                                </span>
                            </div>
                            <div class=""error-message"">${message}</div>
                            <div class=""error-details"">
                                ${componentName ? `Component: ${componentName}` : ''}
                                ${trackingMethod ? ` | Method: ${trackingMethod}` : ''}
                                ${exceptionType ? ` | Type: ${exceptionType}` : ''}
                            </div>
                        </div>
                    `;
                }).join('');

                document.getElementById('errors').innerHTML = errorsHtml;
            } catch (error) {
                console.error('Error loading errors:', error);
                document.getElementById('errors').innerHTML = `<div class=""loading"">Error loading errors: ${error.message}</div>`;
            }
        }

        function loadData() {
            loadStats();
            loadErrors();
        }

        loadData();
        setInterval(loadData, 30000);
    </script>
</body>
</html>";
    }
}
