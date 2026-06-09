using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ResumeAnalyzer.Domain.Interfaces;
using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace ResumeAnalyzer.Infrastructure.Services
{
    public class FastAPIClient : IFastAPIClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<FastAPIClient> _logger;

        public FastAPIClient(HttpClient httpClient, IConfiguration configuration, ILogger<FastAPIClient> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
            
            var backendUrl = configuration["BackendService:BaseUrl"] ?? "http://localhost:8000";
            _httpClient.BaseAddress = new Uri(backendUrl);
        }

        private void AddAuthorizationHeader(string token)
        {
            if (!string.IsNullOrEmpty(token))
            {
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
        }

        public async Task<string> ExtractTextAsync(Stream fileStream, string fileName, string token)
        {
            AddAuthorizationHeader(token);
            using var content = new MultipartFormDataContent();
            
            fileStream.Position = 0;
            var streamContent = new StreamContent(fileStream);
            streamContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
            content.Add(streamContent, "file", fileName);

            _logger.LogInformation($"Sending extract-text request for {fileName} to FastAPI.");
            var response = await _httpClient.PostAsync("/api/v1/extract-text", content);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorMsg = await response.Content.ReadAsStringAsync();
                _logger.LogError($"FastAPI error extracting text: {response.StatusCode} - {errorMsg}");
                throw new HttpRequestException($"AI Service error: {response.StatusCode}. Details: {errorMsg}");
            }

            return await response.Content.ReadAsStringAsync();
        }

        public async Task<string> AnalyzeResumeAsync(Stream fileStream, string fileName, string token)
        {
            AddAuthorizationHeader(token);
            using var content = new MultipartFormDataContent();
            
            fileStream.Position = 0;
            var streamContent = new StreamContent(fileStream);
            streamContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
            content.Add(streamContent, "file", fileName);

            _logger.LogInformation($"Sending analyze-resume request for {fileName} to FastAPI.");
            var response = await _httpClient.PostAsync("/api/v1/analyze-resume", content);

            if (!response.IsSuccessStatusCode)
            {
                var errorMsg = await response.Content.ReadAsStringAsync();
                _logger.LogError($"FastAPI error analyzing resume: {response.StatusCode} - {errorMsg}");
                throw new HttpRequestException($"AI Service error: {response.StatusCode}. Details: {errorMsg}");
            }

            return await response.Content.ReadAsStringAsync();
        }

        public async Task<string> CompareJobDescriptionAsync(Stream? fileStream, string? fileName, string? resumeText, string jobDescription, string token)
        {
            AddAuthorizationHeader(token);
            using var content = new MultipartFormDataContent();
            
            // Add job description field (required)
            content.Add(new StringContent(jobDescription), "job_description");

            if (fileStream != null && !string.IsNullOrEmpty(fileName))
            {
                fileStream.Position = 0;
                var streamContent = new StreamContent(fileStream);
                streamContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
                content.Add(streamContent, "file", fileName);
            }
            else if (!string.IsNullOrEmpty(resumeText))
            {
                content.Add(new StringContent(resumeText), "resume_text");
            }
            else
            {
                throw new ArgumentException("Either fileStream or resumeText must be provided.");
            }

            _logger.LogInformation("Sending compare-job-description request to FastAPI.");
            var response = await _httpClient.PostAsync("/api/v1/compare-job-description", content);

            if (!response.IsSuccessStatusCode)
            {
                var errorMsg = await response.Content.ReadAsStringAsync();
                _logger.LogError($"FastAPI error comparing resume to JD: {response.StatusCode} - {errorMsg}");
                throw new HttpRequestException($"AI Service error: {response.StatusCode}. Details: {errorMsg}");
            }

            return await response.Content.ReadAsStringAsync();
        }
    }
}
