using OutSystems.ExternalLibraries.SDK;

namespace OutSystems.EmbeddingService.Models;

[OSStructure(Description = "Input model for document text embedding upsert")]
public struct TextInput
{
    [OSStructureField(Description = "Unique document identifier (mandatory)", IsMandatory = true)]
    public string DocumentId { get; set; }

    [OSStructureField(Description = "Text content to embed and index (mandatory)", IsMandatory = true)]
    public string Text { get; set; }

    [OSStructureField(Description = "Source location or origin of document (e.g. filename, URL)", IsMandatory = false)]
    public string Source { get; set; }

    [OSStructureField(Description = "Namespace or category tag for grouping documents", IsMandatory = false)]
    public string Namespace { get; set; }
}
