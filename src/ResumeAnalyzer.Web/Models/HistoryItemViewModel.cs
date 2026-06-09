using System;

namespace ResumeAnalyzer.Web.Models
{
    public class HistoryItemViewModel
    {
        public Guid AnalysisId { get; set; }
        public string ResumeName { get; set; } = string.Empty;
        public string JobTitle { get; set; } = string.Empty;
        public int AtsScore { get; set; }
        public DateTime DateAnalyzed { get; set; }
        public string BlobUrl { get; set; } = string.Empty;
    }
}
