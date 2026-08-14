using System.Text;
using OutSystems.EmbeddingService;
using OutSystems.EmbeddingService.Models;

namespace OutSystems.EmbeddingService.Test;

public class Program
{
    public static void Main(string[] args)
    {
        Console.WriteLine("================================================================================");
        Console.WriteLine("  ODC EMBEDDING SERVICE COMPREHENSIVE TEST SUITE (.NET 10)");
        Console.WriteLine("  Testing Multi-Format Ingestion (.json, .xml, .csv, .md, .txt, .yaml, .pdf)");
        Console.WriteLine("  Testing All Actions: Upsert, Search, Delete Documents/Namespace/Collection");
        Console.WriteLine("================================================================================\n");

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
            ApiKey = apiKey,
            BaseUrl = Environment.GetEnvironmentVariable("OPENAI_BASE_URL") ?? "https://openrouter.ai/api/v1",
            Model = Environment.GetEnvironmentVariable("OPENAI_MODEL") ?? "nvidia/llama-nemotron-embed-vl-1b-v2:free",
            ChromaUrl = Environment.GetEnvironmentVariable("CHROMA_URL") ?? "http://localhost:8000",
            ChromaCollection = "comprehensive_format_test_collection"
        };

        var service = new EmbeddingService();
        int totalTests = 0;
        int passedTests = 0;

        void AssertTest(string testName, bool condition, string details = "")
        {
            totalTests++;
            if (condition)
            {
                passedTests++;
                Console.WriteLine($"  [PASS] {testName} {(string.IsNullOrEmpty(details) ? "" : $"-> {details}")}");
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"  [FAIL] {testName} {(string.IsNullOrEmpty(details) ? "" : $"-> {details}")}");
                Console.ResetColor();
            }
        }

        // ========================================================================
        // TEST 1: Reset / Clean start
        // ========================================================================
        Console.WriteLine("[1] Preparing clean ChromaDB collection...");
        var initDel = service.DeleteCollection(config.ChromaCollection, config);
        AssertTest("Initial clean collection deletion", initDel.Success, $"Success={initDel.Success}");

        // ========================================================================
        // TEST 2: Prepare Multi-Format Files (.json, .xml, .csv, .md, .txt, .yaml, .pdf)
        // ========================================================================
        Console.WriteLine("\n[2] Preparing multi-format binary files...");

        // A. JSON File
        string jsonContent = @"{
            ""karyawan_id"": ""EMP-1001"",
            ""nama"": ""Budi Santoso"",
            ""departemen"": ""Teknologi Informasi"",
            ""posisi"": ""Senior Solution Architect"",
            ""keahlian"": [""OutSystems ODC"", ""Vector Database"", ""C# .NET 10"", ""Microservices""]
        }";

        // B. XML File
        string xmlContent = @"<?xml version=""1.0"" encoding=""UTF-8""?>
        <katalog_produk>
            <produk id=""PRD-9901"">
                <nama>ODC Vector Intelligence Engine</nama>
                <kategori>Enterprise AI Module</kategori>
                <harga currency=""IDR"">50000000</harga>
                <deskripsi>Pustaka integrasi semantik embedding OutSystems ODC dengan ChromaDB</deskripsi>
            </produk>
        </katalog_produk>";

        // C. CSV File
        string csvContent = "TransactionID,CustomerName,ProductCode,Amount,Status\n" +
                            "TRX-8801,PT Maju Jaya,AI-EMBED-LIC,150000000,PAID\n" +
                            "TRX-8802,CV Bintang Digital,AI-CONSULT,25000000,PAID\n";

        // D. Markdown File
        string mdContent = @"# Panduan Standar Arsitektur RAG di OutSystems ODC

## 1. Konfigurasi Vektor
- Gunakan OpenAI embedding 1536 atau 2048 dimensi.
- Pisahkan dokumen ke dalam namespace spesifik per divisi perusahaan.

## 2. Praktik Terbaik
- Selalu lakukan chunking 500 hingga 1000 karakter.
- Validasi status response `Success` dan `ErrorMessage` pada setiap Server Action.";

        // E. Plain Text (.txt) File
        string txtContent = "SOP Keamanan Data ODC: Seluruh API Key dan kredensial eksternal wajib disimpan di Site Properties atau Key Vault terenkripsi dan tidak boleh dicatat dalam plain text log.";

        // F. YAML File
        string yamlContent = @"app:
  name: odc-vector-service
  version: 1.2.0
  runtime: net10.0
  features:
    - multi_format_ingestion
    - chroma_v2_support
    - graceful_error_handling
";

        // G. Valid In-Memory PDF File
        byte[] pdfBytes = CreateMinimalPdfBytes("Dokumen PDF Resmi: Pedoman Operasional Standar Layanan Cloud OutSystems Developer Cloud Indonesia.");

        var multiFiles = new List<FileInput>
        {
            new FileInput
            {
                DocumentId = "DOC-JSON-001",
                FileName = "karyawan.json",
                FileContent = Encoding.UTF8.GetBytes(jsonContent),
                Namespace = "hr_records",
                ChunkSize = 1000
            },
            new FileInput
            {
                DocumentId = "DOC-XML-001",
                FileName = "katalog.xml",
                FileContent = Encoding.UTF8.GetBytes(xmlContent),
                Namespace = "product_catalog",
                ChunkSize = 1000
            },
            new FileInput
            {
                DocumentId = "DOC-CSV-001",
                FileName = "transaksi.csv",
                FileContent = Encoding.UTF8.GetBytes(csvContent),
                Namespace = "sales_finance",
                ChunkSize = 1000
            },
            new FileInput
            {
                DocumentId = "DOC-MD-001",
                FileName = "panduan_rag.md",
                FileContent = Encoding.UTF8.GetBytes(mdContent),
                Namespace = "tech_docs",
                ChunkSize = 1000
            },
            new FileInput
            {
                DocumentId = "DOC-TXT-001",
                FileName = "sop_keamanan.txt",
                FileContent = Encoding.UTF8.GetBytes(txtContent),
                Namespace = "security_policy",
                ChunkSize = 1000
            },
            new FileInput
            {
                DocumentId = "DOC-YAML-001",
                FileName = "deployment_spec.yaml",
                FileContent = Encoding.UTF8.GetBytes(yamlContent),
                Namespace = "devops_config",
                ChunkSize = 1000
            },
            new FileInput
            {
                DocumentId = "DOC-PDF-001",
                FileName = "pedoman_resmi.pdf",
                FileContent = pdfBytes,
                Namespace = "official_standards",
                ChunkSize = 1000
            }
        };

        // ========================================================================
        // TEST 3: Execute UpsertFileEmbeddings for All Formats
        // ========================================================================
        Console.WriteLine("\n[3] Executing UpsertFileEmbeddings with 7 multi-format files...");
        var upsertAllResp = service.UpsertFileEmbeddings(multiFiles, config);
        AssertTest("UpsertFileEmbeddings across all 7 formats", upsertAllResp.Success, $"Success={upsertAllResp.Success}, UpsertedCount={upsertAllResp.UpsertedCount}, Error='{upsertAllResp.ErrorMessage}'");
        AssertTest("Upserted count equals 7 files", upsertAllResp.UpsertedCount == 7, $"Count={upsertAllResp.UpsertedCount}");

        // ========================================================================
        // TEST 4: Execute UpsertEmbeddings (Raw Text)
        // ========================================================================
        Console.WriteLine("\n[4] Executing UpsertEmbeddings for Raw Text Documents...");
        var rawInputs = new List<TextInput>
        {
            new TextInput
            {
                DocumentId = "DOC-RAW-001",
                Text = "Informasi Tambahan: Layanan Vector Database ChromaDB mendukung metadata filtering berbasis namespace.",
                Source = "manual_entry",
                Namespace = "tech_docs"
            }
        };
        var rawUpsertResp = service.UpsertEmbeddings(rawInputs, config);
        AssertTest("UpsertEmbeddings raw text", rawUpsertResp.Success, $"Success={rawUpsertResp.Success}, Count={rawUpsertResp.UpsertedCount}");

        // ========================================================================
        // TEST 5: Verify Semantic Search Across Each Distinct Format
        // ========================================================================
        Console.WriteLine("\n[5] Executing SearchByText targeted at each ingested format...");

        // 5a. Search JSON
        var jsonSearch = service.SearchByText("Siapa arsitek OutSystems ODC yang ahli vector database?", topK: 1, namespaceName: "hr_records", config: config);
        AssertTest("Search JSON Content (hr_records)", jsonSearch.Success && jsonSearch.Results?.Count > 0, 
            $"Found ID={jsonSearch.Results?[0].DocumentId}, Score={jsonSearch.Results?[0].Score:F4}");

        // 5b. Search XML
        var xmlSearch = service.SearchByText("Berapa harga lisensi Enterprise AI Module ODC?", topK: 1, namespaceName: "product_catalog", config: config);
        AssertTest("Search XML Content (product_catalog)", xmlSearch.Success && xmlSearch.Results?.Count > 0, 
            $"Found ID={xmlSearch.Results?[0].DocumentId}, Score={xmlSearch.Results?[0].Score:F4}");

        // 5c. Search CSV
        var csvSearch = service.SearchByText("Berapa nilai transaksi dari PT Maju Jaya?", topK: 1, namespaceName: "sales_finance", config: config);
        AssertTest("Search CSV Content (sales_finance)", csvSearch.Success && csvSearch.Results?.Count > 0, 
            $"Found ID={csvSearch.Results?[0].DocumentId}, Score={csvSearch.Results?[0].Score:F4}");

        // 5d. Search Markdown
        var mdSearch = service.SearchByText("Berapa ukuran chunking yang disarankan untuk RAG?", topK: 1, namespaceName: "tech_docs", config: config);
        AssertTest("Search Markdown Content (tech_docs)", mdSearch.Success && mdSearch.Results?.Count > 0, 
            $"Found ID={mdSearch.Results?[0].DocumentId}, Score={mdSearch.Results?[0].Score:F4}");

        // 5e. Search Plain Text
        var txtSearch = service.SearchByText("Dimana API Key dan kredensial harus disimpan?", topK: 1, namespaceName: "security_policy", config: config);
        AssertTest("Search Plain Text (.txt) Content (security_policy)", txtSearch.Success && txtSearch.Results?.Count > 0, 
            $"Found ID={txtSearch.Results?[0].DocumentId}, Score={txtSearch.Results?[0].Score:F4}");

        // 5f. Search YAML
        var yamlSearch = service.SearchByText("Apa runtime version dan fitur dari odc-vector-service?", topK: 1, namespaceName: "devops_config", config: config);
        AssertTest("Search YAML Content (devops_config)", yamlSearch.Success && yamlSearch.Results?.Count > 0, 
            $"Found ID={yamlSearch.Results?[0].DocumentId}, Score={yamlSearch.Results?[0].Score:F4}");

        // 5g. Search PDF
        var pdfSearch = service.SearchByText("Pedoman resmi OutSystems Developer Cloud Indonesia", topK: 1, namespaceName: "official_standards", config: config);
        AssertTest("Search PDF Content (official_standards)", pdfSearch.Success && pdfSearch.Results?.Count > 0, 
            $"Found ID={pdfSearch.Results?[0].DocumentId}, Score={pdfSearch.Results?[0].Score:F4}");

        // ========================================================================
        // TEST 6: Test DeleteActions (DeleteDocuments, DeleteByNamespace, DeleteCollection)
        // ========================================================================
        Console.WriteLine("\n[6] Testing Delete Actions...");

        // 6a. Delete Single Document by ID (JSON Doc)
        var delDocResp = service.DeleteDocuments(new List<string> { "DOC-JSON-001" }, config);
        AssertTest("DeleteDocuments for DOC-JSON-001", delDocResp.Success, $"Success={delDocResp.Success}");

        var verifyDocDel = service.SearchByText("Budi Santoso Solution Architect", topK: 1, namespaceName: "hr_records", config: config);
        AssertTest("Verify DOC-JSON-001 is deleted", verifyDocDel.Results?.Count == 0, $"Remaining={verifyDocDel.Results?.Count}");

        // 6b. Delete by Namespace (sales_finance)
        var delNsResp = service.DeleteByNamespace("sales_finance", config);
        AssertTest("DeleteByNamespace for sales_finance", delNsResp.Success, $"Success={delNsResp.Success}");

        var verifyNsDel = service.SearchByText("PT Maju Jaya", topK: 1, namespaceName: "sales_finance", config: config);
        AssertTest("Verify sales_finance namespace is deleted", verifyNsDel.Results?.Count == 0, $"Remaining={verifyNsDel.Results?.Count}");

        // 6c. Delete Collection
        var delColResp = service.DeleteCollection(config.ChromaCollection, config);
        AssertTest("DeleteCollection", delColResp.Success, $"Success={delColResp.Success}");

        // ========================================================================
        // TEST 7: Edge Cases & Graceful Error Handling
        // ========================================================================
        Console.WriteLine("\n[7] Testing Edge Cases & Graceful Error Handling...");

        // 7a. Empty File Content
        var emptyFileResp = service.UpsertFileEmbeddings(new List<FileInput>
        {
            new FileInput { DocumentId = "EMPTY-1", FileName = "kosong.txt", FileContent = Array.Empty<byte>() }
        }, config);
        AssertTest("Empty file content handled safely", !emptyFileResp.Success && !string.IsNullOrEmpty(emptyFileResp.ErrorMessage), $"Success={emptyFileResp.Success}, Error='{emptyFileResp.ErrorMessage}'");

        // 7b. Missing API Key
        var badConfig = new EmbeddingConfig { ApiKey = "", ChromaUrl = config.ChromaUrl };
        var badKeyResp = service.UpsertEmbeddings(rawInputs, badConfig);
        AssertTest("Missing API Key handled safely", !badKeyResp.Success && !string.IsNullOrEmpty(badKeyResp.ErrorMessage), $"Success={badKeyResp.Success}, Error='{badKeyResp.ErrorMessage}'");

        // 7c. Null inputs to UpsertEmbeddings
        var nullInputResp = service.UpsertEmbeddings(new List<TextInput>(), config);
        AssertTest("Empty input list handled safely", nullInputResp.Success && nullInputResp.UpsertedCount == 0, $"Success={nullInputResp.Success}");

        // 7d. Search with empty query
        var emptySearchResp = service.SearchByText("", config: config);
        AssertTest("Empty search query handled safely", emptySearchResp.Success && emptySearchResp.Results?.Count == 0, $"Success={emptySearchResp.Success}");

        // ========================================================================
        // TEST RESULTS SUMMARY
        // ========================================================================
        Console.WriteLine("\n================================================================================");
        Console.WriteLine($"  SUMMARY: {passedTests} of {totalTests} TESTS PASSED ({(passedTests == totalTests ? "100% SUCCESS" : "FAILED")})");
        Console.WriteLine("================================================================================\n");

        if (passedTests < totalTests)
        {
            Environment.Exit(1);
        }
    }

    private static byte[] CreateMinimalPdfBytes(string text)
    {
        // Construct a valid PDF 1.4 binary file with a readable text stream
        string streamContent = $"BT /F1 12 Tf 72 712 Td ({text}) Tj ET";
        int streamLength = Encoding.ASCII.GetByteCount(streamContent);

        var sb = new StringBuilder();
        sb.Append("%PDF-1.4\n");
        sb.Append("1 0 obj\n<< /Type /Catalog /Pages 2 0 R >>\nendobj\n");
        sb.Append("2 0 obj\n<< /Type /Pages /Kids [3 0 R] /Count 1 >>\nendobj\n");
        sb.Append("3 0 obj\n<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Contents 4 0 R /Resources << /Font << /F1 5 0 R >> >> >>\nendobj\n");
        sb.Append($"4 0 obj\n<< /Length {streamLength} >>\nstream\n{streamContent}\nendstream\nendobj\n");
        sb.Append("5 0 obj\n<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>\nendobj\n");
        sb.Append("xref\n0 6\n");
        sb.Append("0000000000 65535 f \n");
        sb.Append("0000000009 00000 n \n");
        sb.Append("0000000058 00000 n \n");
        sb.Append("0000000115 00000 n \n");
        sb.Append("0000000225 00000 n \n");
        sb.Append("0000000330 00000 n \n");
        sb.Append("trailer\n<< /Size 6 /Root 1 0 R >>\nstartxref\n399\n%%EOF");

        return Encoding.UTF8.GetBytes(sb.ToString());
    }
}
