using System;
using System.IO;
using System.Threading.Tasks;

namespace ResumeAnalyzer.Domain.Interfaces
{
    public interface IFastAPIClient
    {
        Task<string> ExtractTextAsync(Stream fileStream, string fileName, string token);
        Task<string> AnalyzeResumeAsync(Stream fileStream, string fileName, string token);
        Task<string> CompareJobDescriptionAsync(Stream? fileStream, string? fileName, string? resumeText, string jobDescription, string token);
    }
}
