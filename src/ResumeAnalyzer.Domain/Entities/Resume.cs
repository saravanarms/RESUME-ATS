using System;
using System.Collections.Generic;

namespace ResumeAnalyzer.Domain.Entities
{
    public class Resume
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string UserId { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public string BlobUrl { get; set; } = string.Empty;
        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
        public bool IsDeleted { get; set; } = false;

        // Navigation properties
        public ApplicationUser? User { get; set; }
        public ICollection<AnalysisResult> AnalysisResults { get; set; } = new List<AnalysisResult>();
    }
}
