using OutSystems.ExternalLibraries.SDK;

namespace OutSystems.EmbeddingService.Models;

[OSStructure(Description = "Response containing delete operation status and error message")]
public struct DeleteResponse
{
    [OSStructureField(Description = "Indicates whether the delete operation succeeded", IsMandatory = true)]
    public bool Success { get; set; }

    [OSStructureField(Description = "Error message if the operation failed, or empty if successful", IsMandatory = false)]
    public string ErrorMessage { get; set; }
}
