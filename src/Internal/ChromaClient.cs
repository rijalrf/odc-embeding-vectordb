using System.Text;
using System.Text.Json;
using OutSystems.EmbeddingService.Models;

namespace OutSystems.EmbeddingService.Internal;

public class ChromaClient
{
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;
    private readonly string _tenant;
    private readonly string _database;
    private string? _collectionsBasePath;

    public ChromaClient(HttpClient httpClient, string baseUrl, string tenant = "default_tenant", string database = "default_database")
    {
        _httpClient = httpClient ?? new HttpClient();
        _baseUrl = baseUrl.TrimEnd('/');
        _tenant = tenant;
        _database = database;
    }

    private string GetCollectionsBasePath()
    {
        return _collectionsBasePath ?? $"{_baseUrl}/api/v2/tenants/{_tenant}/databases/{_database}/collections";
    }

    public async Task<string> GetOrCreateCollectionIdAsync(string collectionName)
    {
        // 1. Try v2 API first
        var v2Path = $"{_baseUrl}/api/v2/tenants/{_tenant}/databases/{_database}/collections";
        var requestBody = new
        {
            name = collectionName,
            get_or_create = true
        };

        var json = JsonSerializer.Serialize(requestBody);
        using var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync(v2Path, content);
        var responseString = await response.Content.ReadAsStringAsync();

        if (response.IsSuccessStatusCode)
        {
            _collectionsBasePath = v2Path;
            using var doc = JsonDocument.Parse(responseString);
            if (doc.RootElement.TryGetProperty("id", out var idProp))
            {
                return idProp.GetString() ?? throw new InvalidOperationException("Collection ID is null.");
            }
        }

        // 2. Fallback to v1 API if v2 returned 404 or unsupported
        var v1Path = $"{_baseUrl}/api/v1/collections";
        using var v1Content = new StringContent(json, Encoding.UTF8, "application/json");
        var v1Response = await _httpClient.PostAsync(v1Path, v1Content);
        var v1ResponseString = await v1Response.Content.ReadAsStringAsync();

        if (v1Response.IsSuccessStatusCode)
        {
            _collectionsBasePath = v1Path;
            using var doc = JsonDocument.Parse(v1ResponseString);
            if (doc.RootElement.TryGetProperty("id", out var idProp))
            {
                return idProp.GetString() ?? throw new InvalidOperationException("Collection ID is null.");
            }
        }

        throw new InvalidOperationException($"Failed to get or create Chroma collection '{collectionName}'. v2 error: {responseString} | v1 error: {v1ResponseString}");
    }

    public async Task UpsertAsync(string collectionId, List<TextInput> inputs, float[][] embeddings)
    {
        var basePath = GetCollectionsBasePath();
        var ids = inputs.Select(x => x.DocumentId).ToList();
        var documents = inputs.Select(x => x.Text).ToList();
        var metadatas = inputs.Select(x => new Dictionary<string, object>
        {
            { "source", x.Source ?? string.Empty },
            { "namespace", x.Namespace ?? string.Empty }
        }).ToList();

        var requestBody = new
        {
            ids = ids,
            embeddings = embeddings,
            documents = documents,
            metadatas = metadatas
        };

        var json = JsonSerializer.Serialize(requestBody);
        using var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync($"{basePath}/{collectionId}/upsert", content);
        var responseString = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"ChromaDB Upsert Error ({response.StatusCode}): {responseString}");
        }
    }

    public async Task<List<SearchResult>> QueryAsync(string collectionId, float[] queryEmbedding, int topK, string? namespaceFilter = null)
    {
        var basePath = GetCollectionsBasePath();
        var requestBodyDict = new Dictionary<string, object>
        {
            { "query_embeddings", new float[][] { queryEmbedding } },
            { "n_results", topK },
            { "include", new string[] { "documents", "metadatas", "distances" } }
        };

        if (!string.IsNullOrWhiteSpace(namespaceFilter))
        {
            requestBodyDict["where"] = new Dictionary<string, string>
            {
                { "namespace", namespaceFilter }
            };
        }

        var json = JsonSerializer.Serialize(requestBodyDict);
        using var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync($"{basePath}/{collectionId}/query", content);
        var responseString = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"ChromaDB Query Error ({response.StatusCode}): {responseString}");
        }

        var resultsList = new List<SearchResult>();
        using var doc = JsonDocument.Parse(responseString);
        var root = doc.RootElement;

        if (!root.TryGetProperty("ids", out var idsArray) || idsArray.GetArrayLength() == 0)
        {
            return resultsList;
        }

        var firstIds = idsArray[0];
        var firstDocs = root.GetProperty("documents")[0];
        var firstMetas = root.GetProperty("metadatas")[0];
        var firstDistances = root.GetProperty("distances")[0];

        int count = firstIds.GetArrayLength();
        for (int i = 0; i < count; i++)
        {
            var id = firstIds[i].GetString() ?? string.Empty;
            var text = firstDocs[i].GetString() ?? string.Empty;
            var distanceVal = (decimal)firstDistances[i].GetSingle();

            string source = string.Empty;
            string ns = string.Empty;

            if (firstMetas[i].ValueKind == JsonValueKind.Object)
            {
                var metaObj = firstMetas[i];
                if (metaObj.TryGetProperty("source", out var srcProp) && srcProp.ValueKind == JsonValueKind.String)
                {
                    source = srcProp.GetString() ?? string.Empty;
                }
                if (metaObj.TryGetProperty("namespace", out var nsProp) && nsProp.ValueKind == JsonValueKind.String)
                {
                    ns = nsProp.GetString() ?? string.Empty;
                }
            }

            // Score calculated as 1 - distance
            decimal score = Math.Max(0m, 1m - distanceVal);

            resultsList.Add(new SearchResult
            {
                DocumentId = id,
                Text = text,
                Source = source,
                Namespace = ns,
                Distance = distanceVal,
                Score = score
            });
        }

        return resultsList;
    }

    public async Task DeleteDocumentsAsync(string collectionId, List<string> documentIds)
    {
        if (documentIds == null || documentIds.Count == 0)
        {
            return;
        }

        var basePath = GetCollectionsBasePath();
        var requestBody = new
        {
            ids = documentIds.Where(id => !string.IsNullOrWhiteSpace(id)).ToList()
        };

        if (requestBody.ids.Count == 0)
        {
            return;
        }

        var json = JsonSerializer.Serialize(requestBody);
        using var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync($"{basePath}/{collectionId}/delete", content);
        var responseString = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"ChromaDB Delete Documents Error ({response.StatusCode}): {responseString}");
        }
    }

    public async Task DeleteByNamespaceAsync(string collectionId, string namespaceName)
    {
        if (string.IsNullOrWhiteSpace(namespaceName))
        {
            throw new ArgumentException("Namespace name cannot be empty when deleting by namespace.", nameof(namespaceName));
        }

        var basePath = GetCollectionsBasePath();
        var requestBody = new
        {
            where = new Dictionary<string, string>
            {
                { "namespace", namespaceName }
            }
        };

        var json = JsonSerializer.Serialize(requestBody);
        using var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync($"{basePath}/{collectionId}/delete", content);
        var responseString = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"ChromaDB Delete By Namespace Error ({response.StatusCode}): {responseString}");
        }
    }

    public async Task DeleteCollectionAsync(string collectionName)
    {
        if (string.IsNullOrWhiteSpace(collectionName))
        {
            throw new ArgumentException("Collection name cannot be empty when deleting a collection.", nameof(collectionName));
        }

        // 1. Try v2 API first
        var v2Path = $"{_baseUrl}/api/v2/tenants/{_tenant}/databases/{_database}/collections/{collectionName}";
        var response = await _httpClient.DeleteAsync(v2Path);

        if (response.IsSuccessStatusCode || response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return;
        }

        // 2. Fallback to v1 API
        var v1Path = $"{_baseUrl}/api/v1/collections/{collectionName}";
        var v1Response = await _httpClient.DeleteAsync(v1Path);

        if (v1Response.IsSuccessStatusCode || v1Response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return;
        }

        var responseString = await v1Response.Content.ReadAsStringAsync();
        throw new InvalidOperationException($"Failed to delete Chroma collection '{collectionName}': {responseString}");
    }
}
