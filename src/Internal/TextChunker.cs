namespace OutSystems.EmbeddingService.Internal;

public static class TextChunker
{
    public static List<string> ChunkText(string text, int chunkSize = 1000, int overlap = 150)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return new List<string>();
        }

        text = text.Trim();

        // If chunking is disabled (chunkSize <= 0) or text fits in one chunk, return full text
        if (chunkSize <= 0 || text.Length <= chunkSize)
        {
            return new List<string> { text };
        }

        if (overlap >= chunkSize)
        {
            overlap = chunkSize / 5; // safety check
        }

        var chunks = new List<string>();
        int step = chunkSize - overlap;
        int index = 0;

        while (index < text.Length)
        {
            int length = Math.Min(chunkSize, text.Length - index);
            var chunk = text.Substring(index, length).Trim();
            if (!string.IsNullOrWhiteSpace(chunk))
            {
                chunks.Add(chunk);
            }

            index += step;
        }

        return chunks;
    }
}
