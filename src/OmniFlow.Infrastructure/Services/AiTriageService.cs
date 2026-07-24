using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OmniFlow.Application.Interfaces;
using OmniFlow.Domain.Entities;

namespace OmniFlow.Infrastructure.Services
{
    public class AiTriageService : IAiTriageService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AiTriageService> _logger;

        public AiTriageService(HttpClient httpClient, IConfiguration configuration, ILogger<AiTriageService> logger)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<TicketAiPrediction> PredictTicketAsync(string title, string description, CancellationToken cancellationToken)
        {
            var apiKey = _configuration["Gemini:ApiKey"];
            var model = _configuration["Gemini:Model"] ?? "gemini-2.5-flash";

            if (string.IsNullOrEmpty(apiKey))
            {
                _logger.LogWarning("Gemini API Key is not configured. Falling back to mock prediction.");
                return GenerateMockPrediction();
            }

            try
            {
                var prompt = $@"
Analyze the following IT support ticket and predict its Category and Priority. 
Return only a JSON object with keys: ""Category"" (string), ""Priority"" (string: Critical, High, Medium, Low), ""Confidence"" (number 0-1), ""Reasoning"" (string).

Ticket Title: {title}
Ticket Description: {description}
";
                var requestBody = new
                {
                    contents = new[]
                    {
                        new { parts = new[] { new { text = prompt } } }
                    }
                };

                var jsonContent = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
                var url = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={apiKey}";
                
                var response = await _httpClient.PostAsync(url, jsonContent, cancellationToken);
                response.EnsureSuccessStatusCode();

                var responseString = await response.Content.ReadAsStringAsync(cancellationToken);
                
                // Extremely basic parsing for the JSON inside the response
                using var document = JsonDocument.Parse(responseString);
                var textPart = document.RootElement
                    .GetProperty("candidates")[0]
                    .GetProperty("content")
                    .GetProperty("parts")[0]
                    .GetProperty("text").GetString();

                // Clean the markdown JSON block
                var cleanJson = textPart?.Replace("```json", "").Replace("```", "").Trim();
                
                if (cleanJson != null)
                {
                    using var resultDoc = JsonDocument.Parse(cleanJson);
                    return new TicketAiPrediction
                    {
                        PredictedCategory = resultDoc.RootElement.TryGetProperty("Category", out var cat) ? cat.GetString() ?? "Unknown" : "Unknown",
                        PredictedPriority = resultDoc.RootElement.TryGetProperty("Priority", out var prio) ? prio.GetString() ?? "Medium" : "Medium",
                        ConfidenceScore = resultDoc.RootElement.TryGetProperty("Confidence", out var conf) ? conf.GetDouble() : 0.8,
                        Reasoning = resultDoc.RootElement.TryGetProperty("Reasoning", out var rsn) ? rsn.GetString() ?? "" : ""
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to call Gemini API for Ticket Triaging. Falling back to mock prediction.");
            }

            return GenerateMockPrediction();
        }

        private TicketAiPrediction GenerateMockPrediction()
        {
            return new TicketAiPrediction
            {
                PredictedCategory = "Hardware",
                PredictedPriority = "Medium",
                ConfidenceScore = 0.85,
                Reasoning = "Based on standard mock analysis."
            };
        }
    }
}
