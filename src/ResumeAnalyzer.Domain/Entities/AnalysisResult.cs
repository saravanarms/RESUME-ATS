using System;

namespace ResumeAnalyzer.Domain.Entities
{
    public class AnalysisResult
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid ResumeId { get; set; }
        
        public string? JobTitle { get; set; }
        public string JobDescription { get; set; } = string.Empty;
        
        public int AtsScore { get; set; }
        public int KeywordMatchPercentage { get; set; }
        
        // Stored as JSON strings in SQL Server
        public string MissingSkillsJson { get; set; } = "[]";
        public string RecommendationsJson { get; set; } = "[]";
        public string FormattingIssuesJson { get; set; } = "[]";
        public string GrammarSuggestionsJson { get; set; } = "[]";
        
        public string? ResumeSummary { get; set; }
        public DateTime AnalyzedAt { get; set; } = DateTime.UtcNow;

        // Navigation property
        public Resume? Resume { get; set; }
    }
}
