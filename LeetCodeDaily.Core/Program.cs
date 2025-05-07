using System.Reflection;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace LeetCodeDaily.Core
{
    public class Program
    {
        private static readonly HttpClient Client = new HttpClient();
        private static readonly string GraphqlEndpoint = "https://leetcode.com/graphql/";
        private static readonly string SolutionsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Solutions");
        private static readonly string SolutionsPathSave = "D:\\source\\Daily\\LeetCodeDaily.Core\\Solutions";

        public static async Task Main(string[] args)
        {
            try
            {
                // 1. Fetch daily problem
                var dailyProblem = await FetchDailyProblem();
                if (dailyProblem != null)
                {
                    // Create directory with problem code and name
                    var problemDir = Path.Combine(SolutionsPathSave,
                        $"{dailyProblem["questionFrontendId"]}_{dailyProblem["title"].ToString().Replace(" ", "")}");
                    Directory.CreateDirectory(problemDir);

                    // Save problem details to JSON file in the problem directory
                    var jsonPathSave = Path.Combine(problemDir, "problem.json");
                    File.WriteAllText(jsonPathSave, dailyProblem.ToString(Formatting.Indented));
                    Console.WriteLine($"Problem details saved to: {jsonPathSave}");

                    // Copy the problem to the Solutions directory
                    var solutionDir = Path.Combine(SolutionsPath,
                        $"{dailyProblem["questionFrontendId"]}_{dailyProblem["title"].ToString().Replace(" ", "")}");
                    Directory.CreateDirectory(solutionDir);

                    // Copy the template solution file
                    var jsonPath = Path.Combine(solutionDir, "problem.json");
                    File.WriteAllText(jsonPath, dailyProblem.ToString(Formatting.Indented));
                    Console.WriteLine($"Problem details saved to: {jsonPath}");
                }

                // 2. Run all solutions
                await RunAllSolutions();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }

        private static async Task<JObject?> FetchDailyProblem()
        {
            var query = @"
                query questionOfTodayV2 {
                    activeDailyCodingChallengeQuestion {
                        date
                        userStatus
                        link
                        question {
                            id: questionId
                            titleSlug
                            title
                            translatedTitle
                            questionFrontendId
                            paidOnly: isPaidOnly
                            difficulty
                            topicTags {
                                name
                                slug
                                nameTranslated: translatedName
                            }
                            status
                            isInMyFavorites: isFavor
                            acRate
                            frequency: freqBar
                        }
                    }
                }";

            var content = new StringContent(
                JsonConvert.SerializeObject(new { query }),
                Encoding.UTF8,
                "application/json"
            );

            var response = await Client.PostAsync(GraphqlEndpoint, content);
            if (response.IsSuccessStatusCode)
            {
                var jsonResponse = JObject.Parse(await response.Content.ReadAsStringAsync());
                return jsonResponse["data"]?["activeDailyCodingChallengeQuestion"]?["question"] as JObject;
            }

            return null;
        }

        private static async Task RunAllSolutions()
        {
            var solutionDirs =
                Directory.GetDirectories(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Solutions"));

            foreach (var dir in solutionDirs)
            {
                var solutionFiles = Directory.GetFiles(dir, "Solution.cs");
                foreach (var file in solutionFiles)
                {
                    try
                    {
                        // Get the namespace from directory name
                        var dirName = new DirectoryInfo(dir).Name;
                        var namespaceName = $"LeetCodeDaily.Core.Solutions._{dirName}";

                        // Load and create instance of Solution class
                        var assembly = Assembly.GetExecutingAssembly();
                        var type = assembly.GetType($"{namespaceName}.Solution");

                        if (type != null)
                        {
                            var instance = Activator.CreateInstance(type);
                            Console.WriteLine($"\nRunning solution for: {dirName}");

                            // Read problem.json if exists
                            var problemJsonPath = Path.Combine(dir, "problem.json");
                            if (File.Exists(problemJsonPath))
                            {
                                var problemDetails = JObject.Parse(File.ReadAllText(problemJsonPath));
                                Console.WriteLine($"Problem: {problemDetails["title"]}");
                                Console.WriteLine($"Difficulty: {problemDetails["difficulty"]}");
                            }

                            // For demonstration, we'll run the BuildArray method if it exists
                            // In a real implementation, you might want to make this more generic
                            var method = type.GetMethod("BuildArray");
                            if (method != null)
                            {
                                // Example test case for BuildArray
                                var testInput = new int[] { 0, 2, 1, 5, 3, 4 };
                                var result = method.Invoke(instance, new object[] { testInput });

                                Console.WriteLine("Test case:");
                                Console.WriteLine($"Input: [{string.Join(", ", testInput)}]");
                                Console.WriteLine($"Output: [{string.Join(", ", result as int[])}]");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error running solution in {dir}: {ex.Message}");
                    }
                }
            }
        }
    }
}