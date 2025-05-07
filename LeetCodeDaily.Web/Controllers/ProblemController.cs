using LeetCodeDaily.Web.Models;
using LeetCodeDaily.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace LeetCodeDaily.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProblemController : ControllerBase
{
    private readonly LeetCodeService _leetCodeService;
    private readonly ILogger<ProblemController> _logger;

    public ProblemController(LeetCodeService leetCodeService, ILogger<ProblemController> logger)
    {
        _leetCodeService = leetCodeService;
        _logger = logger;
    }

    [HttpGet("daily")]
    public async Task<ActionResult<LeetCodeProblem>> GetDailyProblem()
    {
        try
        {
            var problem = await _leetCodeService.GetDailyProblemAsync();
            if (problem == null)
            {
                return NotFound();
            }
            return problem;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting daily problem");
            return StatusCode(500, "Internal server error");
        }
    }
} 