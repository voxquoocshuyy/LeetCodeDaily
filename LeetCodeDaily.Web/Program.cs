using LeetCodeDaily.Web.Jobs;
using LeetCodeDaily.Web.Middleware;
using LeetCodeDaily.Web.Services;
using Microsoft.AspNetCore.Components;
using Quartz;
using Quartz.Impl;
using Quartz.Spi;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.Elasticsearch;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext()
    .Enrich.WithMachineName()
    .Enrich.WithThreadId()
    .WriteTo.Console()
    .WriteTo.Elasticsearch(new ElasticsearchSinkOptions(new Uri("http://localhost:9200"))
    {
        AutoRegisterTemplate = true,
        AutoRegisterTemplateVersion = AutoRegisterTemplateVersion.ESv7,
        IndexFormat = $"leetcode-daily-{DateTime.UtcNow:yyyy.MM.dd}",
        NumberOfShards = 2,
        NumberOfReplicas = 1,
        CustomFormatter = new Serilog.Formatting.Elasticsearch.ElasticsearchJsonFormatter(),
        EmitEventFailure = EmitEventFailureHandling.WriteToSelfLog |
                           EmitEventFailureHandling.WriteToFailureSink |
                           EmitEventFailureHandling.RaiseCallback,
        FailureCallback = e =>
        {
            Console.WriteLine($"Unable to submit event to Elasticsearch: {e.MessageTemplate}");
            Console.WriteLine($"Exception: {e.Exception}");
        },
        RegisterTemplateFailure = RegisterTemplateRecovery.FailSink,
        DeadLetterIndexName = "deadletter-{0:yyyy.MM}",
        MinimumLogEventLevel = LogEventLevel.Information
    })
    .WriteTo.File("logs/serilog.txt", rollingInterval: RollingInterval.Day)); // Add file logging for debugging

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddHttpClient();

// Add LeetCodeService
builder.Services.AddScoped<LeetCodeService>();

// Add JobService
builder.Services.AddScoped<IJobService, JobService>();

// Add Quartz services
builder.Services.AddQuartz(q =>
{
    q.UseMicrosoftDependencyInjectionJobFactory();

    // Create a job key
    var jobKey = new JobKey("DailyProblemJob");

    // Register the job
    q.AddJob<DailyProblemJob>(opts => opts.WithIdentity(jobKey));

    // Create a trigger
    q.AddTrigger(opts => opts
        .ForJob(jobKey)
        .WithIdentity("DailyProblemJob-trigger")
        .WithCronSchedule("0 0 0 * * ?")); // Run at midnight every day
});

// Add the Quartz.NET hosted service
builder.Services.AddQuartzHostedService(q => q.WaitForJobsToComplete = true);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

// Add request ID middleware
app.UseMiddleware<RequestIdMiddleware>();

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

// Test logging
Log.Information("Application starting up...");
Log.Information("Testing Elasticsearch connection...");
Log.Information("This is a test log message at {Timestamp}", DateTime.UtcNow);
Log.Information("Another test message with structured data: {Property1} {Property2}", "Value1", "Value2");

// Add a test endpoint to generate logs
app.MapGet("/test-logging", () =>
{
    Log.Information("Test endpoint called at {Timestamp}", DateTime.UtcNow);
    return "Logging test completed";
});

app.Run();