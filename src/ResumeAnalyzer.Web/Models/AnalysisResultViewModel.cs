using System;

namespace ResumeAnalyzer.Web.Models
{
    public class AnalysisResultViewModel
    {
        public Guid AnalysisId { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string BlobUrl { get; set; } = string.Empty;
        public string JobTitle { get; set; } = string.Empty;
        public string JobDescription { get; set; } = string.Empty;
        
        public int AtsScore { get; set; }
        public int KeywordMatchPercentage { get; set; }
        
        public string[] MissingSkills { get; set; } = Array.Empty<string>();
        public string[] Recommendations { get; set; } = Array.Empty<string>();
        public string[] FormattingIssues { get; set; } = Array.Empty<string>();
        public string[] GrammarSuggestions { get; set; } = Array.Empty<string>();
        
        public string ResumeSummary { get; set; } = string.Empty;
        public DateTime AnalyzedAt { get; set; }
    }
}
