using OutSystems.EmbeddingService.Internal;
using OutSystems.EmbeddingService.Models;

namespace OutSystems.EmbeddingService;

public class EmbeddingService : IEmbeddingService
{
    private readonly HttpClient _httpClient;

    public EmbeddingService()
    {
        var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
        };
        _httpClient = new HttpClient(handler);
        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) OutSystemsEmbeddingService/1.2.0");
    }

    public EmbeddingService(HttpClient httpClient)
    {
        _httpClient = httpClient ?? new HttpClient();
        if (!_httpClient.DefaultRequestHeaders.UserAgent.Any())
        {
            _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) OutSystemsEmbeddingService/1.2.0");
        }
    }

    private (string apiKey, string baseUrl, string model, string chromaUrl, string chromaCollection) GetConfiguration(EmbeddingConfig config)
    {
        var apiKey = !string.IsNullOrWhiteSpace(config.ApiKey)
            ? config.ApiKey
            : (Environment.GetEnvironmentVariable("OPENAI_API_KEY") ?? string.Empty);

        var baseUrl = !string.IsNullOrWhiteSpace(config.BaseUrl)
            ? config.BaseUrl
            : (Environment.GetEnvironmentVariable("OPENAI_BASE_URL") ?? "https://api.openai.com/v1");

        var model = !string.IsNullOrWhiteSpace(config.Model)
            ? config.Model
            : (Environment.GetEnvironmentVariable("OPENAI_MODEL") ?? "text-embedding-3-small");

        var chromaUrl = !string.IsNullOrWhiteSpace(config.ChromaUrl)
            ? config.ChromaUrl
            : (Environment.GetEnvironmentVariable("CHROMA_URL") ?? "http://localhost:8000");

        var chromaCollection = !string.IsNullOrWhiteSpace(config.ChromaCollection)
            ? config.ChromaCollection
            : (Environment.GetEnvironmentVariable("CHROMA_COLLECTION") ?? "rag_docs");

        return (apiKey, baseUrl, model, chromaUrl, chromaCollection);
    }

    public UpsertResponse UpsertEmbeddings(List<TextInput> inputs, EmbeddingConfig config = default)
    {
        try
        {
            if (inputs == null || inputs.Count == 0)
            {
                return new UpsertResponse { Success = true, ErrorMessage = string.Empty, UpsertedCount = 0 };
            }

            // Validate inputs
            var validInputs = inputs.Where(x => !string.IsNullOrWhiteSpace(x.DocumentId) && !string.IsNullOrWhiteSpace(x.Text)).ToList();
            if (validInputs.Count == 0)
            {
                return new UpsertResponse { Success = true, ErrorMessage = string.Empty, UpsertedCount = 0 };
            }

            var (apiKey, baseUrl, model, chromaUrl, chromaCollection) = GetConfiguration(config);
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                return new UpsertResponse
                {
                    Success = false,
                    ErrorMessage = "API Key is missing. Please provide it in EmbeddingConfig or set OPENAI_API_KEY environment variable.",
                    UpsertedCount = 0
                };
            }

            var openAiClient = new OpenAIClient(_httpClient, apiKey, baseUrl, model);
            var chromaClient = new ChromaClient(_httpClient, chromaUrl);

            int upsertedCount = Task.Run(async () =>
            {
                // 1. Get embeddings for all texts in batch
                var texts = validInputs.Select(x => x.Text).ToList();
                float[][] embeddings = await openAiClient.GetEmbeddingsAsync(texts, apiKey);

                // 2. Ensure Chroma collection exists and get collection ID
                string collectionId = await chromaClient.GetOrCreateCollectionIdAsync(chromaCollection);

                // 3. Upsert into ChromaDB
                await chromaClient.UpsertAsync(collectionId, validInputs, embeddings);

                return validInputs.Count;
            }).GetAwaiter().GetResult();

            return new UpsertResponse
            {
                Success = true,
                ErrorMessage = string.Empty,
                UpsertedCount = upsertedCount
            };
        }
        catch (Exception ex)
        {
            return new UpsertResponse
            {
                Success = false,
                ErrorMessage = ex.Message,
                UpsertedCount = 0
            };
        }
    }

    public UpsertResponse UpsertFileEmbeddings(List<FileInput> files, EmbeddingConfig config = default)
    {
        try
        {
            if (files == null || files.Count == 0)
            {
                return new UpsertResponse
                {
                    Success = false,
                    ErrorMessage = "FileInput list is empty or null.",
                    UpsertedCount = 0
                };
            }

            var textInputs = new List<TextInput>();

            foreach (var file in files)
            {
                if (file.FileContent == null || file.FileContent.Length == 0)
                {
                    return new UpsertResponse
                    {
                        Success = false,
                        ErrorMessage = $"File '{file.FileName}' has empty FileContent (BinaryData).",
                        UpsertedCount = 0
                    };
                }

                string docId = !string.IsNullOrWhiteSpace(file.DocumentId)
                    ? file.DocumentId
                    : (!string.IsNullOrWhiteSpace(file.FileName) ? file.FileName : Guid.NewGuid().ToString("N"));

                string fileName = file.FileName ?? string.Empty;
                string extractedText = FileTextExtractor.ExtractText(file.FileContent, fileName);
                if (string.IsNullOrWhiteSpace(extractedText))
                {
                    return new UpsertResponse
                    {
                        Success = false,
                        ErrorMessage = $"Could not extract any readable text from file '{fileName}'.",
                        UpsertedCount = 0
                    };
                }

                int chunkSize = file.ChunkSize > 0 ? file.ChunkSize : 1000;
                var chunks = TextChunker.ChunkText(extractedText, chunkSize);

                if (chunks.Count == 1)
                {
                    textInputs.Add(new TextInput
                    {
                        DocumentId = docId,
                        Text = chunks[0],
                        Source = fileName,
                        Namespace = file.Namespace ?? string.Empty
                    });
                }
                else
                {
                    for (int i = 0; i < chunks.Count; i++)
                    {
                        textInputs.Add(new TextInput
                        {
                            DocumentId = $"{docId}#chunk-{i + 1}",
                            Text = chunks[i],
                            Source = fileName,
                            Namespace = file.Namespace ?? string.Empty
                        });
                    }
                }
            }

            return UpsertEmbeddings(textInputs, config);
        }
        catch (Exception ex)
        {
            return new UpsertResponse
            {
                Success = false,
                ErrorMessage = ex.Message,
                UpsertedCount = 0
            };
        }
    }

    public SearchResponse SearchByText(string queryText, int topK = 5, string namespaceName = "", EmbeddingConfig config = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(queryText))
            {
                return new SearchResponse
                {
                    Success = true,
                    ErrorMessage = string.Empty,
                    Results = new List<SearchResult>()
                };
            }

            if (topK <= 0)
            {
                topK = 5;
            }

            var (apiKey, baseUrl, model, chromaUrl, chromaCollection) = GetConfiguration(config);
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                return new SearchResponse
                {
                    Success = false,
                    ErrorMessage = "API Key is missing. Please provide it in EmbeddingConfig or set OPENAI_API_KEY environment variable.",
                    Results = new List<SearchResult>()
                };
            }

            var openAiClient = new OpenAIClient(_httpClient, apiKey, baseUrl, model);
            var chromaClient = new ChromaClient(_httpClient, chromaUrl);

            var searchResults = Task.Run(async () =>
            {
                // 1. Generate embedding vector for query text
                float[][] embeddings = await openAiClient.GetEmbeddingsAsync(new[] { queryText }, apiKey);
                if (embeddings.Length == 0 || embeddings[0].Length == 0)
                {
                    return new List<SearchResult>();
                }

                float[] queryVector = embeddings[0];

                // 2. Get ChromaDB collection ID
                string collectionId = await chromaClient.GetOrCreateCollectionIdAsync(chromaCollection);

                // 3. Query ChromaDB for top-K matches
                string? nsFilter = string.IsNullOrWhiteSpace(namespaceName) ? null : namespaceName;
                return await chromaClient.QueryAsync(collectionId, queryVector, topK, nsFilter);
            }).GetAwaiter().GetResult();

            return new SearchResponse
            {
                Success = true,
                ErrorMessage = string.Empty,
                Results = searchResults ?? new List<SearchResult>()
            };
        }
        catch (Exception ex)
        {
            return new SearchResponse
            {
                Success = false,
                ErrorMessage = ex.Message,
                Results = new List<SearchResult>()
            };
        }
    }

    public DeleteResponse DeleteDocuments(List<string> documentIds, EmbeddingConfig config = default)
    {
        try
        {
            if (documentIds == null || documentIds.Count == 0)
            {
                return new DeleteResponse { Success = true, ErrorMessage = string.Empty };
            }

            var (_, _, _, chromaUrl, chromaCollection) = GetConfiguration(config);
            var chromaClient = new ChromaClient(_httpClient, chromaUrl);

            Task.Run(async () =>
            {
                string collectionId = await chromaClient.GetOrCreateCollectionIdAsync(chromaCollection);
                await chromaClient.DeleteDocumentsAsync(collectionId, documentIds);
            }).GetAwaiter().GetResult();

            return new DeleteResponse { Success = true, ErrorMessage = string.Empty };
        }
        catch (Exception ex)
        {
            return new DeleteResponse { Success = false, ErrorMessage = ex.Message };
        }
    }

    public DeleteResponse DeleteByNamespace(string namespaceName, EmbeddingConfig config = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(namespaceName))
            {
                return new DeleteResponse { Success = true, ErrorMessage = string.Empty };
            }

            var (_, _, _, chromaUrl, chromaCollection) = GetConfiguration(config);
            var chromaClient = new ChromaClient(_httpClient, chromaUrl);

            Task.Run(async () =>
            {
                string collectionId = await chromaClient.GetOrCreateCollectionIdAsync(chromaCollection);
                await chromaClient.DeleteByNamespaceAsync(collectionId, namespaceName);
            }).GetAwaiter().GetResult();

            return new DeleteResponse { Success = true, ErrorMessage = string.Empty };
        }
        catch (Exception ex)
        {
            return new DeleteResponse { Success = false, ErrorMessage = ex.Message };
        }
    }

    public DeleteResponse DeleteCollection(string collectionName = "", EmbeddingConfig config = default)
    {
        try
        {
            var (_, _, _, chromaUrl, chromaCollection) = GetConfiguration(config);
            var targetCollection = !string.IsNullOrWhiteSpace(collectionName) ? collectionName : chromaCollection;

            if (string.IsNullOrWhiteSpace(targetCollection))
            {
                return new DeleteResponse { Success = false, ErrorMessage = "Collection name is required to delete a collection." };
            }

            var chromaClient = new ChromaClient(_httpClient, chromaUrl);

            Task.Run(async () =>
            {
                await chromaClient.DeleteCollectionAsync(targetCollection);
            }).GetAwaiter().GetResult();

            return new DeleteResponse { Success = true, ErrorMessage = string.Empty };
        }
        catch (Exception ex)
        {
            return new DeleteResponse { Success = false, ErrorMessage = ex.Message };
        }
    }
}
