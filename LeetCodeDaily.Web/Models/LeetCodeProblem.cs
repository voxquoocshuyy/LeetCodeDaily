using System.Text.Json.Serialization;

namespace LeetCodeDaily.Web.Models;

public class TopicTag
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("slug")]
    public string Slug { get; set; } = string.Empty;
}

public class LeetCodeProblem
{
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("titleSlug")]
    public string TitleSlug { get; set; } = string.Empty;

    [JsonPropertyName("questionFrontendId")]
    public string QuestionFrontendId { get; set; } = string.Empty;

    [JsonPropertyName("translatedTitle")]
    public string TranslatedTitle { get; set; } = string.Empty;

    [JsonPropertyName("difficulty")]
    public string Difficulty { get; set; } = string.Empty;

    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("example")]
    public string Example { get; set; } = string.Empty;

    [JsonPropertyName("constraints")]
    public string Constraints { get; set; } = string.Empty;

    [JsonPropertyName("solution")]
    public string Solution { get; set; } = string.Empty;

    [JsonPropertyName("explanation")]
    public string Explanation { get; set; } = string.Empty;

    [JsonPropertyName("exampleTestcases")]
    public string ExampleTestcases { get; set; } = string.Empty;

    [JsonPropertyName("categoryTitle")]
    public string CategoryTitle { get; set; } = string.Empty;

    [JsonPropertyName("topicTags")]
    public List<TopicTag> TopicTags { get; set; } = new();

    [JsonPropertyName("acRate")]
    public double AcRate { get; set; }

    [JsonPropertyName("frequency")]
    public double Frequency { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;
} 