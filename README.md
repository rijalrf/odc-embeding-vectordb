# OutSystems ODC Embedding & ChromaDB Vector Search Library

[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![OutSystems ODC](https://img.shields.io/badge/OutSystems-ODC%20External%20Logic-D32F2F?logo=outsystems&logoColor=white)](https://www.outsystems.com/)
[![ChromaDB](https://img.shields.io/badge/VectorDB-ChromaDB-FC521F)](https://www.trychroma.com/)

**OutSystems.EmbeddingService** adalah pustaka *External Logic* untuk **OutSystems Developer Cloud (ODC)** berbasis **C# / .NET 10**. Library ini memfasilitasi pembuatan vector embedding teks & file biner melalui API OpenAI-compatible (seperti OpenAI, OpenRouter, Azure OpenAI, Ollama, vLLM) serta indexing dan pencarian semantik (Retrieval-Augmented Generation / RAG) pada **ChromaDB**.

---

## 🌟 Fitur Utama

- **Ekstraksi & Chunking Multi-Format**:
  - Mendukung file dokumen **PDF** via `PdfPig` (ekstraksi teks per layer halaman).
  - Mendukung file berbasis teks seperti **JSON**, **XML**, **CSV**, **Markdown (.md)**, **Plain Text (.txt)**, **YAML**, dan log (via UTF-8 & Latin-1).
  - Pembagian teks otomatis (*chunking*) berdasarkan `ChunkSize` yang dapat disesuaikan.
- **Dukungan OpenAI-Compatible Embeddings**:
  - Kompatibel dengan OpenAI (`text-embedding-3-small`, `text-embedding-3-large`), OpenRouter, Azure OpenAI, Ollama, atau endpoint lokal lainnya.
- **Integrasi ChromaDB Vektor Database**:
  - Mendukung API ChromaDB **v2** (dengan fallback otomatis ke **v1**).
  - Pembuatan dan pengelolaan Collection otomatis.
  - Pencarian Semantik (Cosine/L2/IP Distance) dengan scoring relevansi.
  - Partisi logis metadata menggunakan `Namespace`.
- **Manajemen Siklus Dokumen (Full CRUD & Deletion)**:
  - Hapus dokumen berdasarkan ID (`DeleteDocuments`).
  - Hapus data berdasarkan kategori/namespace (`DeleteByNamespace`).
  - Hapus atau bersihkan seluruh koleksi (`DeleteCollection`).

---

## 📚 Daftar Server Actions (`IEmbeddingService`)

Semua action mengembalikan structure response yang memuat status `Success` (Boolean) dan `ErrorMessage` (Text) untuk penanganan error yang aman (*graceful error handling*) tanpa throw exception.

| Server Action | Parameter Utama | Output Record | Deskripsi |
|---|---|---|---|
| **`UpsertEmbeddings`** | `List<TextInput> inputs`, `EmbeddingConfig config` | `UpsertResponse` (`Success`, `ErrorMessage`, `UpsertedCount`) | Menyimpan atau memperbarui embedding dari teks mentah ke ChromaDB. |
| **`UpsertFileEmbeddings`** | `List<FileInput> files`, `EmbeddingConfig config` | `UpsertResponse` (`Success`, `ErrorMessage`, `UpsertedCount`) | Ekstrak teks dari file biner (PDF/JSON/XML/TXT), chunking, dan simpan embedding ke ChromaDB. |
| **`SearchByText`** | `string queryText`, `int topK`, `string namespaceName`, `EmbeddingConfig config` | `SearchResponse` (`Success`, `ErrorMessage`, `Results`) | Melakukan pencarian vektor semantik (top-K) berdasarkan query teks. |
| **`DeleteDocuments`** | `List<string> documentIds`, `EmbeddingConfig config` | `DeleteResponse` (`Success`, `ErrorMessage`) | Menghapus dokumen tertentu dari ChromaDB berdasarkan ID. |
| **`DeleteByNamespace`** | `string namespaceName`, `EmbeddingConfig config` | `DeleteResponse` (`Success`, `ErrorMessage`) | Menghapus seluruh dokumen yang berada dalam namespace tertentu. |
| **`DeleteCollection`** | `string collectionName`, `EmbeddingConfig config` | `DeleteResponse` (`Success`, `ErrorMessage`) | Menghapus seluruh koleksi vektor dari ChromaDB. |

---

## 🛠️ Struktur Data & Konfigurasi

### `EmbeddingConfig` (Structure)
| Property | Tipe | Deskripsi |
|---|---|---|
| `ApiKey` | `string` | API Key OpenAI / OpenRouter (Wajib jika tidak diset di Environment Variable). |
| `BaseUrl` | `string` | URL base endpoint API embedding (Default: `https://api.openai.com/v1`). |
| `Model` | `string` | Nama model embedding (Default: `text-embedding-3-small`). |
| `ChromaUrl` | `string` | URL server ChromaDB (Default: `http://localhost:8000`). |
| `ChromaCollection` | `string` | Nama target collection di ChromaDB (Default: `rag_docs`). |

### `FileInput` (Structure)
| Property | Tipe | Deskripsi |
|---|---|---|
| `DocumentId` | `string` | ID unik dokumen (Opsional, jika kosong menggunakan nama file / GUID). |
| `FileName` | `string` | Nama file lengkap beserta ekstensinya (misal: `laporan.pdf`, `data.json`). |
| `FileContent` | `byte[]` / `BinaryData` | Konten file dalam format binary. |
| `Namespace` | `string` | Kategori/partisi metadata dokumen (misal: `hr`, `finance`). |
| `ChunkSize` | `int` | Ukuran maksimal karakter per chunk (Default: `1000`). |

### `SearchResult` (Structure)
| Property | Tipe | Deskripsi |
|---|---|---|
| `DocumentId` | `string` | ID dokumen / chunk yang cocok. |
| `Text` | `string` | Isi teks dokumen / chunk. |
| `Source` | `string` | Sumber dokumen / nama file asal. |
| `Namespace` | `string` | Namespace metadata dokumen. |
| `Distance` | `decimal` | Nilai distance vektor dari ChromaDB. |
| `Score` | `decimal` | Nilai skor relevansi semantik ($1 - \text{Distance}$). |

---

## 🚀 Panduan Deployment ke OutSystems ODC

1. Unduh paket siap pakai **`OutSystems.EmbeddingService.zip`** dari menu [**GitHub Releases**](https://github.com/rijalrf/odc-embeding-vectordb/releases/latest).
2. Buka **OutSystems ODC Portal** $\rightarrow$ Pilih menu **External Logic** / **Libraries**.
3. Klik **Upload library** $\rightarrow$ Pilih file `OutSystems.EmbeddingService.zip`.
4. Tambahkan dependensi `OutSystems.EmbeddingService` ke ODC App Anda di ODC Studio.

---

## 💻 Pengembangan Lokal & Build

### Persyaratan:
- [.NET 10.0 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- Instance [ChromaDB](https://docs.trychroma.com/getting-started) (Lokal / Docker / Cloud)

### Build & Package ZIP:
```bash
# 1. Publish Release build
dotnet publish src/OutSystems.EmbeddingService.csproj -c Release -f net10.0 -o /tmp/odc_publish

# 2. Package flat ZIP untuk ODC
python3 -c "import zipfile, os; zf = zipfile.ZipFile('OutSystems.EmbeddingService.zip', 'w', zipfile.ZIP_DEFLATED); [zf.write(os.path.join('/tmp/odc_publish', f), f) for f in sorted(os.listdir('/tmp/odc_publish'))]; zf.close()"
rm -rf /tmp/odc_publish
```

### Menjalankan Test:
```bash
dotnet run --project tests/TestRunner/TestRunner.csproj
```

---

## 📄 Lisensi
Distributed under the MIT License.
