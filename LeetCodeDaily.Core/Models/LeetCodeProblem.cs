using System.Collections.Generic;

namespace LeetCodeDaily.Core.Models
{
    public class LeetCodeProblem
    {
        public string Id { get; set; } = string.Empty;
        public string TitleSlug { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string TranslatedTitle { get; set; } = string.Empty;
        public string QuestionFrontendId { get; set; } = string.Empty;
        public bool PaidOnly { get; set; }
        public string Difficulty { get; set; } = string.Empty;
        public List<TopicTag> TopicTags { get; set; } = new();
        public string Status { get; set; } = string.Empty;
        public bool IsInMyFavorites { get; set; }
        public double AcRate { get; set; }
        public double Frequency { get; set; }
    }

    public class TopicTag
    {
        public string Name { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string NameTranslated { get; set; } = string.Empty;
    }
} 