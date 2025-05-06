using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Reflection;
using LeetCodeDaily.Core.Models;

namespace LeetCodeDaily.Core.Services
{
    public class LeetCodeService
    {
        private readonly string _solutionsPath;

        public LeetCodeService()
        {
            // Get the directory where LeetCodeDaily.Core.dll is located
            var assemblyLocation = Assembly.GetExecutingAssembly().Location;
            var assemblyDirectory = Path.GetDirectoryName(assemblyLocation);
            
            // Navigate up to the solution root directory
            var solutionRoot = Path.GetFullPath(Path.Combine(assemblyDirectory ?? "", "..", "..", "..", ".."));
            _solutionsPath = Path.Combine(solutionRoot, "LeetCodeDaily.Core", "Solutions");
            
            // Create the Solutions directory if it doesn't exist
            if (!Directory.Exists(_solutionsPath))
            {
                Directory.CreateDirectory(_solutionsPath);
            }
        }

        public IEnumerable<LeetCodeProblem> GetAllProblems()
        {
            var problems = new List<LeetCodeProblem>();
            
            if (!Directory.Exists(_solutionsPath))
            {
                return problems;
            }

            var solutionDirs = Directory.GetDirectories(_solutionsPath);

            foreach (var dir in solutionDirs)
            {
                var problemJsonPath = Path.Combine(dir, "problem.json");
                if (File.Exists(problemJsonPath))
                {
                    try
                    {
                        var jsonContent = File.ReadAllText(problemJsonPath);
                        var problem = JsonSerializer.Deserialize<LeetCodeProblem>(jsonContent);
                        if (problem != null)
                        {
                            problems.Add(problem);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error reading problem.json in {dir}: {ex.Message}");
                    }
                }
            }

            return problems.OrderByDescending(p => p.QuestionFrontendId);
        }

        public LeetCodeProblem? GetProblemById(string id)
        {
            if (!Directory.Exists(_solutionsPath))
            {
                return null;
            }

            var solutionDirs = Directory.GetDirectories(_solutionsPath);
            foreach (var dir in solutionDirs)
            {
                var problemJsonPath = Path.Combine(dir, "problem.json");
                if (File.Exists(problemJsonPath))
                {
                    try
                    {
                        var jsonContent = File.ReadAllText(problemJsonPath);
                        var problem = JsonSerializer.Deserialize<LeetCodeProblem>(jsonContent);
                        if (problem?.Id == id)
                        {
                            return problem;
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error reading problem.json in {dir}: {ex.Message}");
                    }
                }
            }

            return null;
        }
    }
} 