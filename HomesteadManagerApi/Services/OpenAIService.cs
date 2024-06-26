using HomesteadManagerApi.Interfaces;
using HomesteadManagerApi.Models;
using HomesteadManagerApi.Models.OpenAi;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Text;

namespace HomesteadManagerApi.Services;

public class OpenAIService : IOpenAIService
{
    private readonly HttpClient _httpClient;
    private readonly IOptions<OpenAIConfig> _config;
    private static readonly string _systemPrompt = "You are an AI assistant that helps people find information about homesteading. Respond in JSON. Do not pretty-print the JSON.";

    public OpenAIService(IOptions<OpenAIConfig> config, HttpClient httpClient)
    {
        _config = config;
        _httpClient = httpClient;
        InitializeClient();
    }

    private void InitializeClient()
    {
        _httpClient.DefaultRequestHeaders.Add("api-key", _config.Value.ApiKey);
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        _httpClient.Timeout = TimeSpan.FromSeconds(1000);
    }

    public async Task<string> CallEndpointAsync(string prompt)
    {
        var messages = new List<Prompt>
        {
            new Prompt
            {
                Role = PromptType.System,
                Content = _systemPrompt
            },
            new Prompt
            {
                Role = PromptType.User,
                Content = prompt
            }
        };
        var requestBody = new Request
        {
            Messages = messages,
            Model = _config.Value.ModelName,
            // Add additional parameters here if needed (e.g., temperature, max_tokens, etc.)
        };

        var jsonContent = JsonConvert.SerializeObject(requestBody);
        var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");

        try
        {
            var url = new Uri(_config.Value.EndpointUrl);
            var endpoint = new Uri(url, $"/openai/deployments/{_config.Value.ModelName}/chat/completions?api-version=2024-02-15-preview");
            var response = await _httpClient.PostAsync(endpoint.AbsoluteUri, httpContent);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException($"Error calling the Azure OpenAI endpoint: {response.StatusCode} - {errorContent}");
            }

            var resultContent = await response.Content.ReadAsStringAsync();
            return resultContent;
        }
        catch (Exception e)
        {
            throw new Exception("An error occurred while calling the Azure OpenAI endpoint", e);
        }
    }
}
