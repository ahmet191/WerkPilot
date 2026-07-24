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
    /// <summary>
    /// Predicts a ticket's category and priority with Google's Gemini model.
    /// When no key is configured, or the call fails (quota, network, bad
    /// response), it degrades to a keyword heuristic so triage always returns a
    /// usable, plausible prediction rather than failing.
    /// </summary>
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
                _logger.LogWarning("Gemini API key is not configured. Using heuristic triage.");
                return Heuristic(title, description, "No AI key configured — heuristic classification.");
            }

            try
            {
                var prompt =
                    "Analyze the following IT support ticket and predict its Category and Priority.\n" +
                    "Return only a JSON object with keys: \"Category\" (string), \"Priority\" (string: Critical, High, Medium, Low), " +
                    "\"Confidence\" (number 0-1), \"Reasoning\" (string).\n\n" +
                    $"Ticket Title: {title}\n" +
                    $"Ticket Description: {description}\n";

                var requestBody = new
                {
                    contents = new[] { new { parts = new[] { new { text = prompt } } } }
                };

                var jsonContent = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
                var url = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent";

                using var httpRequest = new HttpRequestMessage(HttpMethod.Post, url) { Content = jsonContent };
                // Header auth keeps the key out of the URL (and out of request logs).
                httpRequest.Headers.Add("x-goog-api-key", apiKey);

                var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning(
                        "Gemini triage call returned {StatusCode}. Using heuristic triage.", (int)response.StatusCode);
                    return Heuristic(title, description, $"AI unavailable ({(int)response.StatusCode}) — heuristic classification.");
                }

                var responseString = await response.Content.ReadAsStringAsync(cancellationToken);

                using var document = JsonDocument.Parse(responseString);
                var textPart = document.RootElement
                    .GetProperty("candidates")[0]
                    .GetProperty("content")
                    .GetProperty("parts")[0]
                    .GetProperty("text").GetString();

                // Strip a markdown ```json fence if the model added one.
                var cleanJson = textPart?.Replace("```json", "").Replace("```", "").Trim();

                if (!string.IsNullOrEmpty(cleanJson))
                {
                    using var resultDoc = JsonDocument.Parse(cleanJson);
                    var root = resultDoc.RootElement;
                    return new TicketAiPrediction
                    {
                        PredictedCategory = root.TryGetProperty("Category", out var cat) ? cat.GetString() ?? "General" : "General",
                        PredictedPriority = NormalizePriority(root.TryGetProperty("Priority", out var prio) ? prio.GetString() : null),
                        ConfidenceScore = root.TryGetProperty("Confidence", out var conf) && conf.TryGetDouble(out var c) ? c : 0.8,
                        Reasoning = root.TryGetProperty("Reasoning", out var rsn) ? rsn.GetString() ?? "" : ""
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Gemini triage failed. Using heuristic triage.");
            }

            return Heuristic(title, description, "AI call failed — heuristic classification.");
        }

        private static string NormalizePriority(string? value) => value?.Trim().ToLowerInvariant() switch
        {
            "critical" => "Critical",
            "high" => "High",
            "low" => "Low",
            _ => "Medium",
        };

        /// <summary>
        /// A lightweight keyword classifier used whenever the model is
        /// unavailable, so triage still yields a varied, plausible prediction.
        /// </summary>
        private static TicketAiPrediction Heuristic(string title, string description, string reason)
        {
            var text = $"{title} {description}".ToLowerInvariant();

            string category =
                Contains(text, "password", "login", "account", "mfa", "vpn", "access") ? "Access & Identity" :
                Contains(text, "laptop", "monitor", "printer", "keyboard", "hardware", "device", "battery") ? "Hardware" :
                Contains(text, "network", "wifi", "internet", "connection", "slow", "outage") ? "Network" :
                Contains(text, "email", "outlook", "mailbox", "calendar") ? "Email & Collaboration" :
                Contains(text, "install", "software", "license", "update", "application", "app") ? "Software" :
                Contains(text, "leave", "payroll", "onboarding", "benefit", "hr") ? "HR Request" :
                "General";

            string priority =
                Contains(text, "urgent", "critical", "down", "outage", "cannot work", "security", "breach") ? "Critical" :
                Contains(text, "asap", "blocked", "important", "high") ? "High" :
                Contains(text, "whenever", "minor", "cosmetic", "low") ? "Low" :
                "Medium";

            return new TicketAiPrediction
            {
                PredictedCategory = category,
                PredictedPriority = priority,
                ConfidenceScore = 0.55,
                Reasoning = reason
            };
        }

        private static bool Contains(string haystack, params string[] needles)
        {
            foreach (var n in needles)
            {
                if (haystack.Contains(n, StringComparison.Ordinal)) return true;
            }
            return false;
        }
    }
}
