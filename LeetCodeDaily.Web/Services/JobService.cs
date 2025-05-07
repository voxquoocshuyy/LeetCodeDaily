using Quartz;
using Quartz.Impl;

namespace LeetCodeDaily.Web.Services;

public interface IJobService
{
    Task RunDailyProblemJobAsync();
}

public class JobService : IJobService
{
    private readonly ISchedulerFactory _schedulerFactory;
    private readonly ILogger<JobService> _logger;

    public JobService(ISchedulerFactory schedulerFactory, ILogger<JobService> logger)
    {
        _schedulerFactory = schedulerFactory;
        _logger = logger;
    }

    public async Task RunDailyProblemJobAsync()
    {
        try
        {
            var scheduler = await _schedulerFactory.GetScheduler();
            var jobKey = new JobKey("DailyProblemJob");
            
            // Trigger the job immediately
            await scheduler.TriggerJob(jobKey);
            _logger.LogInformation("Daily problem job triggered successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error triggering daily problem job");
            throw;
        }
    }
} 