using OutSystems.ExternalLibraries.SDK;

namespace OutSystems.EmbeddingService.Models;

[OSStructure(Description = "Response containing search operation status, error message, and matching documents")]
public struct SearchResponse
{
    [OSStructureField(Description = "Indicates whether the search operation succeeded", IsMandatory = true)]
    public bool Success { get; set; }

    [OSStructureField(Description = "Error message if the operation failed, or empty if successful", IsMandatory = false)]
    public string ErrorMessage { get; set; }

    [OSStructureField(Description = "List of semantically similar documents retrieved from ChromaDB", IsMandatory = true)]
    public List<SearchResult> Results { get; set; }
}
