using System.Text;
using UglyToad.PdfPig;

namespace OutSystems.EmbeddingService.Internal;

public static class FileTextExtractor
{
    public static string ExtractText(byte[] fileContent, string fileName)
    {
        if (fileContent == null || fileContent.Length == 0)
        {
            throw new ArgumentException("File content (BinaryData) is empty or null.");
        }

        var ext = Path.GetExtension(fileName ?? string.Empty).ToLowerInvariant();

        // Detect PDF by extension OR magic bytes (%PDF)
        if (ext == ".pdf" || IsPdfHeader(fileContent))
        {
            return ExtractTextFromPdf(fileContent);
        }

        // For text files (.txt, .md, .csv, .json, .xml, .yaml, .log, etc.)
        var text = Encoding.UTF8.GetString(fileContent);
        if (string.IsNullOrWhiteSpace(text))
        {
            // Try ASCII/Latin1 fallback
            text = Encoding.Latin1.GetString(fileContent);
        }

        // Strip UTF-8 BOM if present
        return text.TrimStart('\uFEFF');
    }

    private static bool IsPdfHeader(byte[] bytes)
    {
        return bytes.Length >= 4 && bytes[0] == 0x25 && bytes[1] == 0x50 && bytes[2] == 0x44 && bytes[3] == 0x46; // %PDF
    }

    private static string ExtractTextFromPdf(byte[] pdfBytes)
    {
        try
        {
            using var stream = new MemoryStream(pdfBytes);
            using var document = PdfDocument.Open(stream);
            var sb = new StringBuilder();

            foreach (var page in document.GetPages())
            {
                var text = page.Text;
                if (!string.IsNullOrWhiteSpace(text))
                {
                    sb.AppendLine(text);
                }
            }

            var result = sb.ToString().Trim();
            if (string.IsNullOrWhiteSpace(result))
            {
                throw new InvalidOperationException("PDF file contains no readable text layer (it might be scanned image-only PDF).");
            }

            return result;
        }
        catch (Exception ex) when (ex is not InvalidOperationException)
        {
            throw new InvalidOperationException($"PDF parsing error: {ex.Message}", ex);
        }
    }
}
