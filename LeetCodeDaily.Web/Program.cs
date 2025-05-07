using LeetCodeDaily.Web.Jobs;
using LeetCodeDaily.Web.Services;
using Microsoft.AspNetCore.Components;
using Quartz;
using Quartz.Impl;
using Quartz.Spi;

var builder = WebApplication.CreateBuilder(args);

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

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();
