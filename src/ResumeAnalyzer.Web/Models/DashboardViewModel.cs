using System;
using System.Collections.Generic;

namespace ResumeAnalyzer.Web.Models
{
    public class DashboardViewModel
    {
        public int TotalResumes { get; set; }
        public int AverageAtsScore { get; set; }
        public List<RecentAnalysisViewModel> RecentAnalyses { get; set; } = new();
        public List<AtsTrendPoint> TrendPoints { get; set; } = new();
    }

    public class RecentAnalysisViewModel
    {
        public Guid AnalysisId { get; set; }
        public string ResumeName { get; set; } = string.Empty;
        public string JobTitle { get; set; } = string.Empty;
        public int AtsScore { get; set; }
        public DateTime DateAnalyzed { get; set; }
    }

    public class AtsTrendPoint
    {
        public string DateLabel { get; set; } = string.Empty;
        public int Score { get; set; }
    }
}
