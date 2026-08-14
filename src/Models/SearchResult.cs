using OutSystems.ExternalLibraries.SDK;

namespace OutSystems.EmbeddingService.Models;

[OSStructure(Description = "SearchResult model containing retrieved document text, metadata and relevance score for RAG")]
public struct SearchResult
{
    [OSStructureField(Description = "Unique document identifier", IsMandatory = true)]
    public string DocumentId { get; set; }

    [OSStructureField(Description = "Original document text content", IsMandatory = true)]
    public string Text { get; set; }

    [OSStructureField(Description = "Source location or origin of document", IsMandatory = false)]
    public string Source { get; set; }

    [OSStructureField(Description = "Namespace or category of document", IsMandatory = false)]
    public string Namespace { get; set; }

    [OSStructureField(Description = "Similarity score (0.0 to 1.0, higher means more relevant)", IsMandatory = true)]
    public decimal Score { get; set; }

    [OSStructureField(Description = "Vector distance (lower means closer match)", IsMandatory = true)]
    public decimal Distance { get; set; }
}
