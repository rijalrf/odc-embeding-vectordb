using OutSystems.ExternalLibraries.SDK;

namespace OutSystems.EmbeddingService.Models;

[OSStructure(Description = "Input model for document file (TXT, PDF, etc.) embedding upsert")]
public struct FileInput
{
    [OSStructureField(Description = "Unique document identifier (mandatory)", IsMandatory = true)]
    public string DocumentId { get; set; }

    [OSStructureField(Description = "File binary content (BinaryData in OutSystems)", IsMandatory = true)]
    public byte[] FileContent { get; set; }

    [OSStructureField(Description = "File name with extension (e.g. document.pdf, notes.txt)", IsMandatory = true)]
    public string FileName { get; set; }

    [OSStructureField(Description = "Namespace or category tag for grouping documents", IsMandatory = false)]
    public string Namespace { get; set; }

    [OSStructureField(Description = "Chunk size in characters to split document text (default: 1000, 0 for no chunking)", IsMandatory = false)]
    public int ChunkSize { get; set; }
}
