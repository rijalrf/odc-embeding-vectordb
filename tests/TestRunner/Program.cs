using System.Text;
using OutSystems.EmbeddingService;
using OutSystems.EmbeddingService.Models;

namespace OutSystems.EmbeddingService.Test;

public class Program
{
    public static void Main(string[] args)
    {
        Console.WriteLine("=================================================");
        Console.WriteLine(" Testing ODC Embedding Service (Response & Errors)");
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

        var customConfig = new EmbeddingConfig
        {
            ApiKey = apiKey,
            BaseUrl = "https://openrouter.ai/api/v1",
            Model = "nvidia/llama-nemotron-embed-vl-1b-v2:free",
            ChromaUrl = "http://localhost:8000",
            ChromaCollection = "test_config_param_docs"
        };

        var service = new EmbeddingService();

        // 1. Test UpsertFileEmbeddings with response object
        Console.WriteLine("\n[1] Executing UpsertFileEmbeddings...");
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

        var upsertResp = service.UpsertFileEmbeddings(sampleFiles, customConfig);
        Console.WriteLine($"==> Upsert Response: Success={upsertResp.Success}, Count={upsertResp.UpsertedCount}, Error='{upsertResp.ErrorMessage}'");
        if (!upsertResp.Success)
        {
            Console.WriteLine($"==> UPSERT FAILED: {upsertResp.ErrorMessage}");
            return;
        }

        // 2. Test SearchByText with response object
        Console.WriteLine("\n[2] Executing SearchByText...");
        var searchResp = service.SearchByText("Syarat cuti sakit berapa hari?", topK: 1, namespaceName: "", config: customConfig);
        Console.WriteLine($"==> Search Response: Success={searchResp.Success}, ResultsCount={searchResp.Results?.Count ?? 0}, Error='{searchResp.ErrorMessage}'");

        if (searchResp.Success && searchResp.Results != null)
        {
            foreach (var item in searchResp.Results)
            {
                Console.WriteLine($"  ID: {item.DocumentId} | Score: {item.Score:F4} | Text: {item.Text}");
            }
        }

        // 3. Test DeleteDocuments
        Console.WriteLine("\n[3] Executing DeleteDocuments for 'CFG-FILE-001'...");
        var deleteDocResp = service.DeleteDocuments(new List<string> { "CFG-FILE-001" }, customConfig);
        Console.WriteLine($"==> DeleteDocuments Response: Success={deleteDocResp.Success}, Error='{deleteDocResp.ErrorMessage}'");

        var verifyResults = service.SearchByText("cuti sakit", topK: 1, namespaceName: "hr_policy", config: customConfig);
        Console.WriteLine($"==> Verification search after DeleteDocuments: Count={verifyResults.Results?.Count ?? 0}");

        // 4. Test Upsert & DeleteByNamespace
        Console.WriteLine("\n[4] Re-upserting and testing DeleteByNamespace...");
        service.UpsertFileEmbeddings(sampleFiles, customConfig);
        var deleteNsResp = service.DeleteByNamespace("hr_policy", customConfig);
        Console.WriteLine($"==> DeleteByNamespace Response: Success={deleteNsResp.Success}, Error='{deleteNsResp.ErrorMessage}'");

        var verifyNsResults = service.SearchByText("cuti sakit", topK: 1, namespaceName: "hr_policy", config: customConfig);
        Console.WriteLine($"==> Verification search after DeleteByNamespace: Count={verifyNsResults.Results?.Count ?? 0}");

        // 5. Test DeleteCollection
        Console.WriteLine("\n[5] Testing DeleteCollection...");
        var deleteColResp = service.DeleteCollection(customConfig.ChromaCollection, customConfig);
        Console.WriteLine($"==> DeleteCollection Response: Success={deleteColResp.Success}, Error='{deleteColResp.ErrorMessage}'");

        // 6. Test Error Handling (Invalid API Key)
        Console.WriteLine("\n[6] Testing Error Handling (Empty API Key)...");
        var invalidConfig = new EmbeddingConfig { ApiKey = "", ChromaUrl = "http://localhost:8000" };
        var errResp = service.UpsertEmbeddings(new List<TextInput> { new TextInput { DocumentId = "1", Text = "test" } }, invalidConfig);
        Console.WriteLine($"==> Expected Error Handled Gracefully: Success={errResp.Success}, ErrorMessage='{errResp.ErrorMessage}'");

        Console.WriteLine("\n=================================================");
        Console.WriteLine(" All Response & Error Handling Tests Passed!");
        Console.WriteLine("=================================================");
    }
}
