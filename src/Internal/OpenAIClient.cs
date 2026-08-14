using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace OutSystems.EmbeddingService.Internal;

public class OpenAIClient
{
    private readonly HttpClient _httpClient;
    private readonly string _model;
    private readonly string _embeddingsUrl;

    public OpenAIClient(HttpClient httpClient, string apiKey, string baseUrl, string? model = null)
    {
        _httpClient = httpClient ?? new HttpClient();
        
        var envModel = Environment.GetEnvironmentVariable("OPENAI_MODEL");
        _model = !string.IsNullOrWhiteSpace(model) ? model : (!string.IsNullOrWhiteSpace(envModel) ? envModel : "text-embedding-3-small");

        var cleanUrl = baseUrl.TrimEnd('/');
        if (!cleanUrl.EndsWith("/embeddings", StringComparison.OrdinalIgnoreCase))
        {
            cleanUrl += "/embeddings";
        }
        _embeddingsUrl = cleanUrl;
    }

    public async Task<float[][]> GetEmbeddingsAsync(IEnumerable<string> inputs, string apiKey, int batchSize = 5)
    {
        var inputList = inputs.ToList();
        if (inputList.Count == 0)
        {
            return Array.Empty<float[]>();
        }

        var results = new float[inputList.Count][];

        for (int i = 0; i < inputList.Count; i += batchSize)
        {
            var batch = inputList.Skip(i).Take(batchSize).ToList();
            var requestBody = new
            {
                model = _model,
                input = batch
            };

            var json = JsonSerializer.Serialize(requestBody);
            using var request = new HttpRequestMessage(HttpMethod.Post, _embeddingsUrl);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(request);
            var responseString = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException($"Embedding Provider Error ({response.StatusCode}): {responseString}");
            }

            using var doc = JsonDocument.Parse(responseString);
            var dataArray = doc.RootElement.GetProperty("data");

            int itemIdx = 0;
            foreach (var item in dataArray.EnumerateArray())
            {
                var embeddingArr = item.GetProperty("embedding");
                var vector = new float[embeddingArr.GetArrayLength()];
                int vIdx = 0;
                foreach (var val in embeddingArr.EnumerateArray())
                {
                    vector[vIdx++] = val.GetSingle();
                }

                results[i + itemIdx] = vector;
                itemIdx++;
            }
        }

        return results;
    }
}
