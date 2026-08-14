using System.Text;
using OutSystems.EmbeddingService;
using OutSystems.EmbeddingService.Models;

namespace OutSystems.EmbeddingService.Test;

public class Program
{
    public static void Main(string[] args)
    {
        Console.WriteLine("=================================================");
        Console.WriteLine(" Testing ODC Embedding Service with Config Param");
        Console.WriteLine("=================================================");

        // Define Explicit EmbeddingConfig (Passable directly from ODC Studio / Site Properties)
        var customConfig = new EmbeddingConfig
        {
            ApiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY") ?? "YOUR_OPENAI_OR_OPENROUTER_API_KEY",
            BaseUrl = Environment.GetEnvironmentVariable("OPENAI_BASE_URL") ?? "https://openrouter.ai/api/v1",
            Model = Environment.GetEnvironmentVariable("OPENAI_MODEL") ?? "nvidia/llama-nemotron-embed-vl-1b-v2:free",
            ChromaUrl = Environment.GetEnvironmentVariable("CHROMA_URL") ?? "http://localhost:8000",
            ChromaCollection = "test_config_param_docs"
        };

        var service = new EmbeddingService();

        // 1. Test UpsertFileEmbeddings with explicit config parameter
        Console.WriteLine("\n[1] Executing UpsertFileEmbeddings with explicit EmbeddingConfig...");
        var sampleFiles = new List<FileInput>
        {
            new FileInput
            {
                DocumentId = "CFG-FILE-001",
                FileName = "Kebijakan_Cuti_Sakit.txt",
                FileContent = Encoding.UTF8.GetBytes("SOP Cuti Sakit: Karyawan yang sakit lebih dari 2 hari berturut-turut wajib melampirkan surat keterangan dokter resmi dari rumah sakit atau klinik."),
                Namespace = "hr_policy",
                ChunkSize = 1000
            }
        };

        try
        {
            int chunkCount = service.UpsertFileEmbeddings(sampleFiles, customConfig);
            Console.WriteLine($"==> SUCCESS: Upserted {chunkCount} chunks using explicit EmbeddingConfig!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"==> FILE UPSERT ERROR: {ex.Message}");
            return;
        }

        // 2. Test SearchByText with explicit config parameter
        Console.WriteLine("\n[2] Executing SearchByText with explicit EmbeddingConfig...");
        try
        {
            var results = service.SearchByText("Syarat cuti sakit berapa hari?", topK: 1, namespaceName: "", config: customConfig);
            Console.WriteLine($"==> SUCCESS: Retrieved {results.Count} matching documents:\n");

            foreach (var item in results)
            {
                Console.WriteLine($"Document ID : {item.DocumentId}");
                Console.WriteLine($"Text        : {item.Text}");
                Console.WriteLine($"Source      : {item.Source}");
                Console.WriteLine($"Score       : {item.Score:F4}\n");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"==> SEARCH ERROR: {ex.Message}");
        }

        // 3. Test DeleteDocuments
        Console.WriteLine("\n[3] Executing DeleteDocuments for 'CFG-FILE-001'...");
        try
        {
            bool deleted = service.DeleteDocuments(new List<string> { "CFG-FILE-001" }, customConfig);
            Console.WriteLine($"==> SUCCESS: DeleteDocuments result: {deleted}");

            var verifyResults = service.SearchByText("cuti sakit", topK: 1, namespaceName: "hr_policy", config: customConfig);
            Console.WriteLine($"==> Verification search after DeleteDocuments returned {verifyResults.Count} documents.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"==> DELETE DOCUMENTS ERROR: {ex.Message}");
        }

        // 4. Test Upsert & DeleteByNamespace
        Console.WriteLine("\n[4] Re-upserting and testing DeleteByNamespace...");
        try
        {
            service.UpsertFileEmbeddings(sampleFiles, customConfig);
            bool deletedNs = service.DeleteByNamespace("hr_policy", customConfig);
            Console.WriteLine($"==> SUCCESS: DeleteByNamespace result: {deletedNs}");

            var verifyNsResults = service.SearchByText("cuti sakit", topK: 1, namespaceName: "hr_policy", config: customConfig);
            Console.WriteLine($"==> Verification search after DeleteByNamespace returned {verifyNsResults.Count} documents.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"==> DELETE BY NAMESPACE ERROR: {ex.Message}");
        }

        // 5. Test DeleteCollection
        Console.WriteLine("\n[5] Testing DeleteCollection...");
        try
        {
            bool deletedCol = service.DeleteCollection(customConfig.ChromaCollection, customConfig);
            Console.WriteLine($"==> SUCCESS: DeleteCollection result: {deletedCol}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"==> DELETE COLLECTION ERROR: {ex.Message}");
        }

        Console.WriteLine("=================================================");
        Console.WriteLine(" All Delete & Search Test Runs Completed!");
        Console.WriteLine("=================================================");
    }
}
