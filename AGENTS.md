# OutSystems ODC Embedding Service Library

Proyek ini adalah **OutSystems ODC External Logic Library (C# / .NET 10)** yang menyediakan fungsionalitas pembuatan vector embedding teks & file biner via API OpenAI-compatible (misal: OpenRouter / OpenAI) serta pengindeksan dan pencarian semantik (RAG) pada **ChromaDB**.

---

## ⚙️ Spesifikasi & Aturan Teknis (Rules for AI Agents)

1. **Target Framework**:
   * **Wajib selalu menggunakan .NET 10 (`net10.0`)**.
   * Jangan downgrade ke versi .NET lainnya kecuali diminta secara eksplisit oleh user.

2. **Build & Intermediate Directory**:
   * Folder default `src/bin` dan `src/obj` dimiliki oleh root (`permission denied` jika ditulis user normal).
   * Gunakan selalu `Directory.Build.props` yang mengarahkan output build ke `.artifacts/bin` dan `.artifacts/obj`.
   * Proyek csproj memiliki rule `DefaultItemExcludes` untuk mengabaikan folder `bin/**` dan `obj/**`.

3. **Cara Build & Packaging untuk OutSystems ODC**:
   * **Build / Publish**:
     ```bash
     export PATH="$HOME/.dotnet:$PATH"
     export DOTNET_ROOT="$HOME/.dotnet"
     dotnet publish src/OutSystems.EmbeddingService.csproj -c Release -f net10.0 -o /tmp/odc_publish
     ```
   * **Packaging ZIP (`OutSystems.EmbeddingService.zip`)**:
     File zip harus memiliki **flat structure** (seluruh DLL dan `.deps.json` diletakkan langsung di root ZIP tanpa subfolder pembungkus).
     ```bash
     python3 -c "import zipfile, os; zf = zipfile.ZipFile('/home/rijal/projects/odc/embed_odc_lib/OutSystems.EmbeddingService.zip', 'w', zipfile.ZIP_DEFLATED); [zf.write(os.path.join('/tmp/odc_publish', f), f) for f in sorted(os.listdir('/tmp/odc_publish'))]; zf.close()"
     rm -rf /tmp/odc_publish
     ```

4. **Cara Menjalankan Test Runner**:
   ```bash
   export PATH="$HOME/.dotnet:$PATH"
   export DOTNET_ROOT="$HOME/.dotnet"
   dotnet run --project tests/TestRunner/TestRunner.csproj
   ```

5. **Kewajiban Release Notes**:
   * **Setiap kali membuat atau memperbarui file `OutSystems.EmbeddingService.zip`**, agen **wajib** mencatat perubahannya ke dalam file `RELEASE_NOTES.md` dan menyertakan ringkasan release notes kepada user.

6. **Kewajiban GitHub Release**:
   * File `.zip` diabaikan oleh `.gitignore` dan **tidak boleh di-commit langsung ke git history**.
   * Setiap kali membuat rilis file `.zip` baru, agen **wajib mempublikasikannya ke GitHub Release** dengan melampirkan file `OutSystems.EmbeddingService.zip` sebagai asset release:
     ```bash
     gh release create <tag_version> OutSystems.EmbeddingService.zip --title "<title>" --notes-file RELEASE_NOTES.md
     ```

---

## 📚 Daftar Server Actions (`IEmbeddingService`)

1. **`UpsertEmbeddings`**
   * Menyimpan/memperbarui embedding dari daftar data teks (`TextInput`).
   * Parameter: `List<TextInput> inputs`, `EmbeddingConfig config`
   * Return: `int UpsertedCount`

2. **`UpsertFileEmbeddings`**
   * Mengekstrak teks dari file biner (PDF via `PdfPig`, teks plain seperti `.txt`, `.json`, `.xml`, `.csv`, `.md`, `.yaml` via UTF-8/Latin-1), memecah menjadi chunk, mengenerate embedding, dan menyimpannya ke ChromaDB.
   * Parameter: `List<FileInput> files`, `EmbeddingConfig config`
   * Return: `int UpsertedChunkCount`

3. **`SearchByText`**
   * Melakukan pencarian vektor semantik (top-K) berdasarkan teks query dan filter namespace (opsional).
   * Parameter: `string queryText`, `int topK = 5`, `string namespaceName = ""`, `EmbeddingConfig config`
   * Return: `List<SearchResult>`

4. **`DeleteDocuments`**
   * Menghapus dokumen tertentu dari ChromaDB berdasarkan daftar `DocumentId`.
   * Parameter: `List<string> documentIds`, `EmbeddingConfig config`
   * Return: `bool Success`

5. **`DeleteByNamespace`**
   * Menghapus seluruh dokumen yang memiliki metadata `namespace` tertentu.
   * Parameter: `string namespaceName`, `EmbeddingConfig config`
   * Return: `bool Success`

6. **`DeleteCollection`**
   * Menghapus seluruh koleksi data vektor dari ChromaDB.
   * Parameter: `string collectionName = ""`, `EmbeddingConfig config`
   * Return: `bool Success`

---

## 🏛️ Struktur Arsitektur & Direktori

```text
embed_odc_lib/
├── AGENTS.md                                # Konteks dan pedoman proyek ini
├── Directory.Build.props                    # Konfigurasi redirect build artifacts
├── OutSystems.EmbeddingService.zip          # Paket ZIP siap upload ke OutSystems ODC
├── src/
│   ├── IEmbeddingService.cs                 # Interface [OSInterface] & definisi [OSAction]
│   ├── EmbeddingService.cs                  # Implementasi utama service
│   ├── OutSystems.EmbeddingService.csproj   # Proyek library .NET 10
│   ├── Internal/
│   │   ├── ChromaClient.cs                  # Komunikasi HTTP ChromaDB (v2 API dengan fallback v1)
│   │   ├── OpenAIClient.cs                  # Komunikasi API OpenAI-compatible embeddings
│   │   ├── FileTextExtractor.cs             # Parser PDF & teks (.json, .xml, .txt, dll)
│   │   └── TextChunker.cs                   # Algoritma chunking teks dokumen
│   └── Models/
│       ├── EmbeddingConfig.cs               # Struktur konfigurasi API Key, URL, Model, Collection
│       ├── FileInput.cs                     # Model input file biner (BinaryData)
│       ├── TextInput.cs                     # Model input teks mentah
│       └── SearchResult.cs                  # Model hasil pencarian semantik
└── tests/
    └── TestRunner/                          # Program konsol pengujian integrasi
```
