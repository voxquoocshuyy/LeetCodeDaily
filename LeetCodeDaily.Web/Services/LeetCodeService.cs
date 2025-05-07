using System.Text.Json;
using LeetCodeDaily.Web.Models;
using Polly;
using Polly.Retry;

namespace LeetCodeDaily.Web.Services;

public class LeetCodeService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<LeetCodeService> _logger;
    private readonly string _solutionsPath;
    private readonly AsyncRetryPolicy _retryPolicy;

    public LeetCodeService(HttpClient httpClient, ILogger<LeetCodeService> logger, IWebHostEnvironment env)
    {
        _httpClient = httpClient;
        _logger = logger;
        _solutionsPath = Path.Combine(env.ContentRootPath, "Solutions");
        
        // Configure retry policy
        _retryPolicy = Policy
            .Handle<HttpRequestException>()
            .Or<TimeoutException>()
            .WaitAndRetryAsync(3, retryAttempt => 
                TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));

        // Configure timeout
        _httpClient.Timeout = TimeSpan.FromSeconds(30);
        
        if (!Directory.Exists(_solutionsPath))
        {
            Directory.CreateDirectory(_solutionsPath);
        }
    }

    public async Task<LeetCodeProblem?> GetDailyProblemAsync()
    {
        try
        {
            return await _retryPolicy.ExecuteAsync(async () =>
            {
                var query = @"
                    query questionOfToday {
                        activeDailyCodingChallengeQuestion {
                            date
                            userStatus
                            link
                            question {
                                acRate
                                difficulty
                                freqBar
                                frontendQuestionId: questionFrontendId
                                isFavor
                                paidOnly: isPaidOnly
                                status
                                title
                                titleSlug
                                hasVideoSolution
                                hasSolution
                                topicTags {
                                    name
                                    id
                                    slug
                                }
                            }
                        }
                    }";

                var response = await _httpClient.PostAsJsonAsync("https://leetcode.com/graphql", new { query });
                response.EnsureSuccessStatusCode();

                var result = await response.Content.ReadFromJsonAsync<JsonElement>();
                var question = result.GetProperty("data")
                                   .GetProperty("activeDailyCodingChallengeQuestion")
                                   .GetProperty("question");

                var titleSlug = question.GetProperty("titleSlug").GetString();
                if (string.IsNullOrEmpty(titleSlug))
                {
                    _logger.LogError("Failed to get titleSlug from daily problem");
                    return null;
                }

                var problem = await GetProblemDetailsAsync(titleSlug);
                if (problem != null)
                {
                    await SaveProblemAsync(problem);
                }

                return problem;
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting daily problem after retries");
            return null;
        }
    }

    public async Task<LeetCodeProblem?> GetProblemDetailsAsync(string titleSlug)
    {
        if (string.IsNullOrWhiteSpace(titleSlug))
        {
            throw new ArgumentException("Title slug cannot be empty", nameof(titleSlug));
        }

        try
        {
            return await _retryPolicy.ExecuteAsync(async () =>
            {
                var query = @"
                    query questionContent($titleSlug: String!) {
                        question(titleSlug: $titleSlug) {
                            questionId
                            questionFrontendId
                            title
                            titleSlug
                            content
                            translatedTitle
                            translatedContent
                            difficulty
                            categoryTitle
                            exampleTestcases
                            topicTags {
                                name
                                slug
                            }
                            codeSnippets {
                                lang
                                langSlug
                                code
                            }
                            hints
                            solution {
                                id
                                canSeeDetail
                                paidOnly
                                hasVideoSolution
                                paidOnlyVideo
                            }
                            status
                            sampleTestCase
                            metaData
                            judgerAvailable
                            judgeType
                            mysqlSchemas
                            enableRunCode
                            enableTestMode
                            enableDebugger
                            libraryUrl
                            adminUrl
                            challengeQuestion {
                                id
                                date
                                incompleteChallengeCount
                                streakCount
                                type
                            }
                            note
                        }
                    }";

                var response = await _httpClient.PostAsJsonAsync("https://leetcode.com/graphql", 
                    new { query, variables = new { titleSlug } });
                response.EnsureSuccessStatusCode();

                var result = await response.Content.ReadFromJsonAsync<JsonElement>();
                var question = result.GetProperty("data").GetProperty("question");

                var problem = new LeetCodeProblem
                {
                    Title = question.GetProperty("title").GetString() ?? string.Empty,
                    TitleSlug = question.GetProperty("titleSlug").GetString() ?? string.Empty,
                    QuestionFrontendId = question.GetProperty("questionFrontendId").GetString() ?? string.Empty,
                    Difficulty = question.GetProperty("difficulty").GetString() ?? string.Empty,
                    CategoryTitle = question.GetProperty("categoryTitle").GetString() ?? string.Empty,
                    Content = question.GetProperty("content").GetString() ?? string.Empty,
                    Description = question.GetProperty("content").GetString() ?? string.Empty,
                    ExampleTestcases = question.GetProperty("exampleTestcases").GetString() ?? string.Empty,
                    Example = question.GetProperty("exampleTestcases").GetString() ?? string.Empty,
                    TopicTags = question.GetProperty("topicTags").EnumerateArray()
                        .Select(tag => new TopicTag
                        {
                            Name = tag.GetProperty("name").GetString() ?? string.Empty,
                            Slug = tag.GetProperty("slug").GetString() ?? string.Empty
                        })
                        .ToList()
                };

                // Parse content to extract constraints
                if (!string.IsNullOrEmpty(problem.Content))
                {
                    var contentLines = problem.Content.Split('\n');
                    var currentSection = "";
                    var sections = new Dictionary<string, List<string>>();

                    foreach (var line in contentLines)
                    {
                        if (line.StartsWith("Example"))
                        {
                            currentSection = "Example";
                            sections[currentSection] = new List<string>();
                            sections[currentSection].Add(line);
                        }
                        else if (line.StartsWith("Constraints:"))
                        {
                            currentSection = "Constraints";
                            sections[currentSection] = new List<string>();
                        }
                        else if (!string.IsNullOrEmpty(currentSection) && !string.IsNullOrEmpty(line))
                        {
                            sections[currentSection].Add(line);
                        }
                    }

                    // Cập nhật các trường dữ liệu
                    if (sections.ContainsKey("Example"))
                    {
                        problem.Example = string.Join("\n", sections["Example"]);
                    }
                    if (sections.ContainsKey("Constraints"))
                    {
                        problem.Constraints = string.Join("\n", sections["Constraints"]);
                    }
                }

                return problem;
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting problem details for {TitleSlug} after retries", titleSlug);
            return null;
        }
    }

    private async Task CreateSampleProblemAsync()
    {
        try
        {
            var sampleProblem = new LeetCodeProblem
            {
                Title = "Build Array from Permutation",
                TitleSlug = "build-array-from-permutation",
                Difficulty = "Easy",
                Description = "Given a zero-based permutation nums (0-indexed), build an array ans of the same length where ans[i] = nums[nums[i]] for each 0 <= i < nums.length and return it.",
                Example = "Input: nums = [0,2,1,5,3,4]\nOutput: [0,1,2,4,5,3]",
                Constraints = "1 <= nums.length <= 1000\n0 <= nums[i] < nums.length\nThe elements in nums are distinct.",
                Solution = "public int[] BuildArray(int[] nums) {\n    int[] ans = new int[nums.Length];\n    for (int i = 0; i < nums.Length; i++) {\n        ans[i] = nums[nums[i]];\n    }\n    return ans;\n}",
                CategoryTitle = "Array"
            };

            var problemDir = Path.Combine(_solutionsPath, $"{DateTime.Now:yyyy-MM-dd}-{sampleProblem.TitleSlug}");
            if (!Directory.Exists(problemDir))
            {
                Directory.CreateDirectory(problemDir);
            }

            var jsonPath = Path.Combine(problemDir, "problem.json");
            var json = JsonSerializer.Serialize(sampleProblem, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(jsonPath, json);

            _logger.LogInformation("Created sample problem at {Path}", jsonPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating sample problem");
        }
    }

    public async Task<List<LeetCodeProblem>> GetAllProblemsAsync()
    {
        var problems = new List<LeetCodeProblem>();
        try
        {
            _logger.LogInformation("Getting all problems from {Path}", _solutionsPath);
            
            if (!Directory.Exists(_solutionsPath))
            {
                _logger.LogWarning("Solutions directory does not exist at {Path}", _solutionsPath);
                Directory.CreateDirectory(_solutionsPath);
                _logger.LogInformation("Created Solutions directory at {Path}", _solutionsPath);
                
                // Create sample problem if directory is empty
                await CreateSampleProblemAsync();
            }

            var directories = Directory.GetDirectories(_solutionsPath);
            _logger.LogInformation("Found {Count} problem directories", directories.Length);

            if (directories.Length == 0)
            {
                // Create sample problem if no problems exist
                await CreateSampleProblemAsync();
                directories = Directory.GetDirectories(_solutionsPath);
            }

            foreach (var dir in directories)
            {
                var jsonPath = Path.Combine(dir, "problem.json");
                _logger.LogInformation("Checking for problem.json in {Path}", dir);
                
                if (File.Exists(jsonPath))
                {
                    _logger.LogInformation("Found problem.json in {Path}", dir);
                    var json = await File.ReadAllTextAsync(jsonPath);
                    var problem = JsonSerializer.Deserialize<LeetCodeProblem>(json);
                    if (problem != null)
                    {
                        problems.Add(problem);
                        _logger.LogInformation("Added problem: {Title}", problem.Title);
                    }
                }
                else
                {
                    _logger.LogWarning("No problem.json found in {Path}", dir);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all problems");
        }
        return problems;
    }

    private async Task SaveProblemAsync(LeetCodeProblem problem)
    {
        try
        {
            var problemDir = Path.Combine(_solutionsPath, $"{DateTime.Now:yyyy-MM-dd}-{problem.TitleSlug}");
            if (!Directory.Exists(problemDir))
            {
                Directory.CreateDirectory(problemDir);
            }

            var jsonPath = Path.Combine(problemDir, "problem.json");
            var json = JsonSerializer.Serialize(problem, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(jsonPath, json);

            _logger.LogInformation("Saved problem to {Path}", jsonPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving problem {TitleSlug}", problem.TitleSlug);
        }
    }

    public async Task<LeetCodeProblem?> GetProblemByTitleSlugAsync(string titleSlug)
    {
        if (string.IsNullOrWhiteSpace(titleSlug))
        {
            throw new ArgumentException("Title slug cannot be empty", nameof(titleSlug));
        }

        try
        {
            var problemsPath = Path.Combine(_solutionsPath);
            var directories = Directory.GetDirectories(problemsPath);
            
            foreach (var dir in directories)
            {
                if (dir.Contains(titleSlug, StringComparison.OrdinalIgnoreCase))
                {
                    var jsonPath = Path.Combine(dir, "problem.json");
                    if (File.Exists(jsonPath))
                    {
                        var json = await File.ReadAllTextAsync(jsonPath);
                        return JsonSerializer.Deserialize<LeetCodeProblem>(json);
                    }
                }
            }

            // If not found locally, fetch from LeetCode API
            return await GetProblemDetailsAsync(titleSlug);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting problem by title slug {TitleSlug}", titleSlug);
            return null;
        }
    }
} 