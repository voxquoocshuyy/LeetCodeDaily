using LeetCodeDaily.Web.Services;
using Quartz;
using Polly;
using Polly.Retry;

namespace LeetCodeDaily.Web.Jobs;

public class DailyProblemJob : IJob
{
    private readonly LeetCodeService _leetCodeService;
    private readonly ILogger<DailyProblemJob> _logger;
    private readonly AsyncRetryPolicy _retryPolicy;

    public DailyProblemJob(LeetCodeService leetCodeService, ILogger<DailyProblemJob> logger)
    {
        _leetCodeService = leetCodeService;
        _logger = logger;

        // Configure retry policy
        _retryPolicy = Policy
            .Handle<Exception>()
            .WaitAndRetryAsync(3, retryAttempt => 
                TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                onRetry: (exception, timeSpan, retryCount, context) =>
                {
                    _logger.LogWarning(exception, 
                        "Retry {RetryCount} after {Delay}ms for daily problem job", 
                        retryCount, timeSpan.TotalMilliseconds);
                });
    }

    public async Task Execute(IJobExecutionContext context)
    {
        _logger.LogInformation("Starting to fetch daily problem at {Time}", DateTime.UtcNow);

        try
        {
            await _retryPolicy.ExecuteAsync(async () =>
            {
                var problem = await _leetCodeService.GetDailyProblemAsync();
                if (problem != null)
                {
                    _logger.LogInformation(
                        "Successfully fetched daily problem: {Title} (ID: {Id}, Difficulty: {Difficulty})",
                        problem.Title,
                        problem.QuestionFrontendId,
                        problem.Difficulty);
                }
                else
                {
                    _logger.LogWarning("Failed to fetch daily problem - no problem returned");
                    throw new Exception("No problem returned from service");
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, 
                "Error executing daily problem job after all retries at {Time}", 
                DateTime.UtcNow);
            throw; // Rethrow to let Quartz handle the failure
        }
    }
} 