using System.Text;
using OutSystems.EmbeddingService;
using OutSystems.EmbeddingService.Models;

namespace OutSystems.EmbeddingService.Test;

public class Program
{
    public static void Main(string[] args)
    {
        Console.WriteLine("=================================================");
        Console.WriteLine(" Testing Cloudflare ChromaDB with JSON File");
        Console.WriteLine(" URL: https://chromedbwsl.opendv.xyz");
        Console.WriteLine(" Collection: sop_murah_dev");
        Console.WriteLine("=================================================\n");

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
            ChromaCollection = "sop_murah_dev"
        };

        var service = new EmbeddingService();

        // 1. Ingest JSON
        string jsonText = @"{
            ""dokumen_id"": ""DOC-JSON-SOP-01"",
            ""judul"": ""SOP Pengajuan Cuti MurahDev"",
            ""kategori"": ""HR Policy"",
            ""isi"": ""Seluruh karyawan wajib mengajukan cuti minimal 3 hari kerja sebelum tanggal pelaksanaan melalui portal internal.""
        }";

        var files = new List<FileInput>
        {
            new FileInput
            {
                DocumentId = "DOC-JSON-SOP-01",
                FileName = "sop_cuti.json",
                FileContent = Encoding.UTF8.GetBytes(jsonText),
                Namespace = "hr_policy",
                ChunkSize = 1000
            }
        };

        Console.WriteLine("[1] Upserting JSON file to https://chromedbwsl.opendv.xyz...");
        var upsertResp = service.UpsertFileEmbeddings(files, config);
        Console.WriteLine($"==> Upsert Result: Success={upsertResp.Success}, Count={upsertResp.UpsertedCount}, Error='{upsertResp.ErrorMessage}'");

        if (!upsertResp.Success)
        {
            Console.WriteLine("Upsert failed!");
            return;
        }

        // 2. Search in Cloudflare ChromaDB
        Console.WriteLine("\n[2] Searching for 'cuti minimal berapa hari'...");
        var searchResp = service.SearchByText("cuti minimal berapa hari sebelum pelaksanaan?", topK: 3, namespaceName: "", config: config);
        Console.WriteLine($"==> Search Result: Success={searchResp.Success}, Count={searchResp.Results?.Count ?? 0}, Error='{searchResp.ErrorMessage}'");

        if (searchResp.Results != null)
        {
            foreach (var r in searchResp.Results)
            {
                Console.WriteLine($"\nFound Document:");
                Console.WriteLine($"  ID        : {r.DocumentId}");
                Console.WriteLine($"  Namespace : {r.Namespace}");
                Console.WriteLine($"  Source    : {r.Source}");
                Console.WriteLine($"  Score     : {r.Score:F4}");
                Console.WriteLine($"  Text      : {r.Text}");
            }
        }
    }
}
