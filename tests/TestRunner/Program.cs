using System.Text;
using OutSystems.EmbeddingService;
using OutSystems.EmbeddingService.Internal;
using OutSystems.EmbeddingService.Models;

namespace OutSystems.EmbeddingService.Test;

public class Program
{
    public static void Main(string[] args)
    {
        Console.WriteLine("=================================================");
        Console.WriteLine(" Test Suite: Semantic Parsers & TextChunker");
        Console.WriteLine("=================================================\n");

        // 1. Test OutSystems JSON Parsing on sample.json
        if (File.Exists("sample.json"))
        {
            Console.WriteLine("[TEST 1] Testing OutSystemsJsonParser on sample.json...");
            string sampleJson = File.ReadAllText("sample.json");
            bool isOutSystems = OutSystemsJsonParser.IsOutSystemsJson(sampleJson);
            Console.WriteLine($"  Is OutSystems JSON: {isOutSystems}");

            var chunks = OutSystemsJsonParser.ParseToSemanticChunks(sampleJson);
            Console.WriteLine($"  Total Semantic Chunks Extracted: {chunks.Count}");

            var actionChunks = chunks.Where(c => c.ElementType == "ServerAction").ToList();
            var entityChunks = chunks.Where(c => c.ElementType == "Entity").ToList();
            var structChunks = chunks.Where(c => c.ElementType == "Structure").ToList();
            var apiChunks = chunks.Where(c => c.ElementType == "ServiceAPI").ToList();
            var processChunks = chunks.Where(c => c.ElementType == "Process").ToList();

            Console.WriteLine($"  - Server Actions : {actionChunks.Count}");
            Console.WriteLine($"  - Entities       : {entityChunks.Count}");
            Console.WriteLine($"  - Structures     : {structChunks.Count}");
            Console.WriteLine($"  - Service APIs   : {apiChunks.Count}");
            Console.WriteLine($"  - BPT Processes  : {processChunks.Count}");

            var sampleAction = actionChunks.FirstOrDefault(c => c.DocumentSuffix.Contains("Logic_UpdateDeadlineChangesByPIC_V2"));
            if (sampleAction.DocumentSuffix != null)
            {
                Console.WriteLine("\n--- Sample Formatted Server Action ---");
                Console.WriteLine($"ID Suffix: {sampleAction.DocumentSuffix}");
                Console.WriteLine(sampleAction.FormattedText);
                Console.WriteLine("---------------------------------------\n");
            }
        }

        // 2. Test Smart Boundary Chunker on Markdown (.md)
        Console.WriteLine("[TEST 2] Testing Smart Boundary TextChunker on Markdown text...");
        string sampleMarkdown = @"# Bab 1: Pengantar Sistem External Logic
OutSystems ODC memungkinkan integrasi pustaka .NET 10 ke dalam cloud runtime.

## 1.1 Persyaratan Arsitektur
Semua library wajib di-compile dengan target framework net10.0 dan menggunakan arsitektur modular yang rapi.

## 1.2 Format File
Sistem mendukung file .pdf, .json, .xml, .md, dan .txt dengan chunking semantik cerdas. Seluruh dokumen dijamin tidak terpotong di tengah kata atau kalimat.";

        var mdChunks = TextChunker.ChunkText(sampleMarkdown, chunkSize: 200, overlap: 30);
        Console.WriteLine($"  Original Length: {sampleMarkdown.Length} chars");
        Console.WriteLine($"  Generated Chunks: {mdChunks.Count}");
        for (int i = 0; i < mdChunks.Count; i++)
        {
            Console.WriteLine($"  Chunk #{i + 1} ({mdChunks[i].Length} chars):");
            Console.WriteLine($"    \"" + mdChunks[i].Replace("\n", " ") + "\"");
        }

        // 3. Test XML Semantic Extractor
        Console.WriteLine("\n[TEST 3] Testing XmlSemanticExtractor...");
        string sampleXml = @"<Catalog>
  <Book id=""bk101"">
    <Author>Gambardella, Matthew</Author>
    <Title>XML Developer's Guide</Title>
    <Price>44.95</Price>
  </Book>
  <Book id=""bk102"">
    <Author>Ralls, Kim</Author>
    <Title>Midnight Rain</Title>
    <Price>5.95</Price>
  </Book>
</Catalog>";
        var xmlElements = XmlSemanticExtractor.ExtractXmlElements(sampleXml);
        Console.WriteLine($"  Extracted XML Records: {xmlElements.Count}");
        foreach (var el in xmlElements)
        {
            Console.WriteLine($"  - Suffix: {el.Suffix}, ElementType: {el.ElementType}");
        }

        // 4. Test Ingest & Search with ChromaDB
        Console.WriteLine("\n=================================================");
        Console.WriteLine(" [TEST 4] Testing Ingest & Search with ChromaDB");
        Console.WriteLine("=================================================");

        string apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY") ?? string.Empty;
        if (string.IsNullOrWhiteSpace(apiKey) && File.Exists("credential.txt"))
        {
            var lines = File.ReadAllLines("credential.txt");
            foreach (var line in lines)
            {
                if (line.StartsWith("apikey :"))
                {
                    apiKey = line.Replace("apikey :", "").Trim();
                }
            }
        }

        var config = new EmbeddingConfig
        {
            ApiKey = !string.IsNullOrWhiteSpace(apiKey) ? apiKey : "YOUR_API_KEY",
            BaseUrl = "https://openrouter.ai/api/v1",
            Model = "nvidia/llama-nemotron-embed-vl-1b-v2:free",
            ChromaUrl = "https://chromedbwsl.opendv.xyz",
            ChromaCollection = "odc_semantic_test"
        };

        var service = new EmbeddingService();

        // Ingest sample markdown file
        var files = new List<FileInput>
        {
            new FileInput
            {
                DocumentId = "DOC-MD-01",
                FileName = "arsitektur_odc.md",
                FileContent = Encoding.UTF8.GetBytes(sampleMarkdown),
                Namespace = "docs",
                ChunkSize = 250
            }
        };

        Console.WriteLine("Upserting Markdown file to ChromaDB...");
        var upsertResp = service.UpsertFileEmbeddings(files, config);
        Console.WriteLine($"==> Upsert Result: Success={upsertResp.Success}, Count={upsertResp.UpsertedCount}, Error='{upsertResp.ErrorMessage}'");

        if (upsertResp.Success)
        {
            Console.WriteLine("\nSearching for 'target framework net10.0'...");
            var searchResp = service.SearchByText("target framework apa yang digunakan?", topK: 2, namespaceName: "", config: config);
            Console.WriteLine($"==> Search Result: Success={searchResp.Success}, Count={searchResp.Results?.Count ?? 0}, Error='{searchResp.ErrorMessage}'");

            if (searchResp.Results != null)
            {
                foreach (var r in searchResp.Results)
                {
                    Console.WriteLine($"\nFound Document:");
                    Console.WriteLine($"  ID        : {r.DocumentId}");
                    Console.WriteLine($"  Source    : {r.Source}");
                    Console.WriteLine($"  Score     : {r.Score:F4}");
                    Console.WriteLine($"  Text      : {r.Text}");
                }
            }
        }

        Console.WriteLine("\nAll tests completed successfully!");
    }
}
