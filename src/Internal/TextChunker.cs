namespace OutSystems.EmbeddingService.Internal;

public static class TextChunker
{
    /// <summary>
    /// Memotong teks panjang ke dalam chunk dengan batasan semantik cerdas (Paragraph, Markdown Header, Sentence, Word).
    /// </summary>
    public static List<string> ChunkText(string text, int chunkSize = 1000, int overlap = 150)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return new List<string>();
        }

        text = text.Trim();

        // Jika chunking dimatikan atau teks muat dalam 1 chunk
        if (chunkSize <= 0 || text.Length <= chunkSize)
        {
            return new List<string> { text };
        }

        if (overlap >= chunkSize)
        {
            overlap = chunkSize / 5; // Safety check
        }

        var chunks = new List<string>();
        int index = 0;

        while (index < text.Length)
        {
            int remaining = text.Length - index;
            if (remaining <= chunkSize)
            {
                var finalChunk = text.Substring(index).Trim();
                if (!string.IsNullOrWhiteSpace(finalChunk))
                {
                    chunks.Add(finalChunk);
                }
                break;
            }

            // Target split point maksimal
            int targetLength = chunkSize;
            int splitPoint = -1;

            // Cari boundary alami terbaik dari batas mundur (minimal 60% dari chunkSize agar chunk tidak terlalu kerdil)
            int minSearchIndex = index + (int)(chunkSize * 0.6);
            int maxSearchIndex = index + targetLength;

            // 1. Boundary: Markdown Heading ("\n#") atau Paragraf ("\n\n")
            for (int i = maxSearchIndex; i >= minSearchIndex; i--)
            {
                if (i + 1 < text.Length && text[i] == '\n' && (text[i + 1] == '\n' || text[i + 1] == '#'))
                {
                    splitPoint = i;
                    break;
                }
            }

            // 2. Boundary: Akhir baris tunggal ("\n")
            if (splitPoint == -1)
            {
                for (int i = maxSearchIndex; i >= minSearchIndex; i--)
                {
                    if (text[i] == '\n')
                    {
                        splitPoint = i;
                        break;
                    }
                }
            }

            // 3. Boundary: Akhir kalimat (". ", "? ", "! ")
            if (splitPoint == -1)
            {
                for (int i = maxSearchIndex; i >= minSearchIndex; i--)
                {
                    if ((text[i] == '.' || text[i] == '?' || text[i] == '!') &&
                        (i + 1 == text.Length || char.IsWhiteSpace(text[i + 1])))
                    {
                        splitPoint = i + 1;
                        break;
                    }
                }
            }

            // 4. Boundary: Spasi kata (' ')
            if (splitPoint == -1)
            {
                for (int i = maxSearchIndex; i >= minSearchIndex; i--)
                {
                    if (char.IsWhiteSpace(text[i]))
                    {
                        splitPoint = i;
                        break;
                    }
                }
            }

            // 5. Fallback hard cut jika tidak ada spasi sama sekali (misal teks kontinu)
            if (splitPoint == -1)
            {
                splitPoint = index + targetLength;
            }

            var chunkText = text.Substring(index, splitPoint - index).Trim();
            if (!string.IsNullOrWhiteSpace(chunkText))
            {
                chunks.Add(chunkText);
            }

            // Maju ke index berikutnya dengan memperhitungkan overlap
            int nextIndex = splitPoint - overlap;
            if (nextIndex <= index)
            {
                nextIndex = splitPoint; // Hindari infinite loop
            }

            index = nextIndex;
        }

        return chunks;
    }
}
