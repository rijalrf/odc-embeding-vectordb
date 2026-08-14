using System.Text;
using OutSystems.EmbeddingService;
using OutSystems.EmbeddingService.Models;

namespace OutSystems.EmbeddingService.Test;

public class Program
{
    public static void Main(string[] args)
    {
        string endpoint = "https://9routerwsl.opendv.xyz/v1";
        string apiKey = "sk-04d74d8ede725bb1-ag80qr-b652a49f";
        string model = "openrouter/openai/text-embedding-3-small";

        var config = new EmbeddingConfig
        {
            ApiKey = apiKey,
            BaseUrl = endpoint,
            Model = model,
            ChromaUrl = "https://chromedbwsl.opendv.xyz",
            ChromaCollection = "sop_murah_dev"
        };

        var service = new EmbeddingService();

        string[] queries = new[]
        {
            "saya mau liat entity followupheader",
            "tabel database FollowUpHeader",
            "entity FollowUpHeader beserta kolomnya"
        };

        foreach (var q in queries)
        {
            Console.WriteLine("=================================================");
            Console.WriteLine($"QUERY: \"{q}\"");
            Console.WriteLine("=================================================");
            var resp = service.SearchByText(q, topK: 3, namespaceName: "", config: config);
            if (resp.Results != null)
            {
                int rank = 1;
                foreach (var r in resp.Results)
                {
                    string firstLine = r.Text.Split('\n').FirstOrDefault() ?? "";
                    string nameLine = r.Text.Split('\n').FirstOrDefault(x => x.Contains("Name:")) ?? "";
                    Console.WriteLine($"[Rank #{rank++}] Score: {r.Score:F4} | Type: {firstLine} | {nameLine}");
                }
            }
            Console.WriteLine();
        }
    }
}
