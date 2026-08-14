using OutSystems.ExternalLibraries.SDK;

namespace OutSystems.EmbeddingService.Models;

[OSStructure(Description = "Configuration settings for OpenAI-compatible API and ChromaDB vector store")]
public struct EmbeddingConfig
{
    [OSStructureField(Description = "API key for OpenAI / OpenRouter (optional if OPENAI_API_KEY env var is set)", IsMandatory = false)]
    public string ApiKey { get; set; }

    [OSStructureField(Description = "Base URL for OpenAI-compatible API (e.g. https://openrouter.ai/api/v1). Default: https://api.openai.com/v1", IsMandatory = false)]
    public string BaseUrl { get; set; }

    [OSStructureField(Description = "Embedding model name (e.g. nvidia/llama-nemotron-embed-vl-1b-v2:free, text-embedding-3-small)", IsMandatory = false)]
    public string Model { get; set; }

    [OSStructureField(Description = "ChromaDB server URL (e.g. http://10.0.0.1:8000). Default: http://localhost:8000", IsMandatory = false)]
    public string ChromaUrl { get; set; }

    [OSStructureField(Description = "ChromaDB collection name. Default: rag_docs", IsMandatory = false)]
    public string ChromaCollection { get; set; }
}
