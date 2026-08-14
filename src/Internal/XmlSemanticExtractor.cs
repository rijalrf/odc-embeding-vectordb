using System.Xml.Linq;

namespace OutSystems.EmbeddingService.Internal;

public static class XmlSemanticExtractor
{
    public static List<(string Suffix, string Content, string ElementType)> ExtractXmlElements(string xmlText)
    {
        var result = new List<(string Suffix, string Content, string ElementType)>();
        if (string.IsNullOrWhiteSpace(xmlText)) return result;

        try
        {
            var doc = XDocument.Parse(xmlText);
            if (doc.Root == null) return result;

            var elements = doc.Root.Elements().ToList();
            if (elements.Count > 1)
            {
                int index = 1;
                foreach (var el in elements)
                {
                    string name = el.Name.LocalName;
                    result.Add(($"xml-{name}-{index++}", el.ToString(), "XmlRecord"));
                }
            }
            else
            {
                result.Add(("xml-root", doc.ToString(), "XmlDocument"));
            }
        }
        catch
        {
            // Fallback jika XML invalid/malformed
            result.Add(("xml-raw", xmlText, "RawText"));
        }

        return result;
    }
}
