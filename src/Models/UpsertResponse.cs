using OutSystems.ExternalLibraries.SDK;

namespace OutSystems.EmbeddingService.Models;

[OSStructure(Description = "Response containing operation status, error message, and total number of upserted items")]
public struct UpsertResponse
{
    [OSStructureField(Description = "Indicates whether the upsert operation succeeded", IsMandatory = true)]
    public bool Success { get; set; }

    [OSStructureField(Description = "Error message if the operation failed, or empty if successful", IsMandatory = false)]
    public string ErrorMessage { get; set; }

    [OSStructureField(Description = "Total number of documents or chunks successfully upserted", IsMandatory = true)]
    public int UpsertedCount { get; set; }
}
