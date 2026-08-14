using OutSystems.ExternalLibraries.SDK;
using OutSystems.EmbeddingService.Models;

namespace OutSystems.EmbeddingService;

[OSInterface(
    Description = "Provides text & binary file embedding generation via OpenAI-compatible endpoints and vector indexing/search with ChromaDB for RAG applications.")]
public interface IEmbeddingService
{
    [OSAction(
        Description = "Generates embeddings for a list of texts and upserts them into ChromaDB vector database.",
        ReturnName = "UpsertedCount")]
    int UpsertEmbeddings(
        [OSParameter(Description = "List of text documents with DocumentId, Text, Source, and Namespace")]
        List<TextInput> inputs,
        [OSParameter(Description = "Configuration for OpenAI API and ChromaDB (optional, falls back to env vars if empty)")]
        EmbeddingConfig config = default);

    [OSAction(
        Description = "Extracts text from binary files (TXT, PDF, etc.), chunks long documents, generates embeddings, and upserts them into ChromaDB.",
        ReturnName = "UpsertedChunkCount")]
    int UpsertFileEmbeddings(
        [OSParameter(Description = "List of files with FileContent (BinaryData), FileName, DocumentId, Namespace, and ChunkSize")]
        List<FileInput> files,
        [OSParameter(Description = "Configuration for OpenAI API and ChromaDB (optional, falls back to env vars if empty)")]
        EmbeddingConfig config = default);

    [OSAction(
        Description = "Generates an embedding for the query text and retrieves top-K semantically relevant documents from ChromaDB.",
        ReturnName = "Results")]
    List<SearchResult> SearchByText(
        [OSParameter(Description = "Query text to search for semantically similar documents")]
        string queryText,
        [OSParameter(Description = "Number of top matching documents to retrieve (default: 5)")]
        int topK = 5,
        [OSParameter(Description = "Optional namespace filter to scope search to a specific category")]
        string namespaceName = "",
        [OSParameter(Description = "Configuration for OpenAI API and ChromaDB (optional, falls back to env vars if empty)")]
        EmbeddingConfig config = default);

    [OSAction(
        Description = "Deletes documents from ChromaDB by their Document IDs.",
        ReturnName = "Success")]
    bool DeleteDocuments(
        [OSParameter(Description = "List of Document IDs to delete")]
        List<string> documentIds,
        [OSParameter(Description = "Configuration for OpenAI API and ChromaDB (optional, falls back to env vars if empty)")]
        EmbeddingConfig config = default);

    [OSAction(
        Description = "Deletes all documents within a specific namespace from ChromaDB.",
        ReturnName = "Success")]
    bool DeleteByNamespace(
        [OSParameter(Description = "Namespace to delete")]
        string namespaceName,
        [OSParameter(Description = "Configuration for OpenAI API and ChromaDB (optional, falls back to env vars if empty)")]
        EmbeddingConfig config = default);

    [OSAction(
        Description = "Deletes an entire collection from ChromaDB.",
        ReturnName = "Success")]
    bool DeleteCollection(
        [OSParameter(Description = "Name of the collection to delete (optional, defaults to collection specified in config)")]
        string collectionName = "",
        [OSParameter(Description = "Configuration for OpenAI API and ChromaDB (optional, falls back to env vars if empty)")]
        EmbeddingConfig config = default);
}
