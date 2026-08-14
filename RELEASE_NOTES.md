# Release Notes — OutSystems.EmbeddingService

Dokumen ini mencatat riwayat perubahan dan versi paket `OutSystems.EmbeddingService.zip` untuk OutSystems ODC External Logic.

## [v1.3.0] - 2026-08-15

### 🧠 Semantic Parsers & Smart Boundary Chunking
- **OutSystems Semantic JSON Parser**:
  - Secara cerdas mengenali struktur ekspor modul OutSystems (`sample.json`).
  - Memecah modul menjadi unit semantik utuh: **Server Actions** (dengan daftar Input & Output Parameters), **Database Entities** (dengan atribut/kolom), **Structures / DTOs**, **Service Actions / APIs**, dan **BPT Processes**.
  - Mengeliminasi noise internal (GUID keys, koordinat canvas `X, Y`, connector visual) sehingga menghemat $>60\%$ token embedding dan melipatgandakan relevansi pencarian semantik (RAG).
- **XML Semantic Extractor**:
  - Mengekstrak record XML secara utuh per elemen menggunakan `XDocument` agar tag pembuka dan penutup (`<tag>...</tag>`) tidak terpotong sembarangan.
- **Smart Boundary TextChunker (Markdown & Plain Text)**:
  - Meningkatkan algoritma chunking agar memotong teks pada batas alami: Heading Markdown (`#`), jeda paragraf (`\n\n`), akhir baris (`\n`), akhir kalimat (`. `, `? `, `! `), dan spasi kata.
  - Menghilangkan masalah kalimat atau kata terpotong di tengah jalan pada file `.md`, `.txt`, `.csv`, dan `.yaml`.

### 📦 Informasi Paket
- **Nama File**: `OutSystems.EmbeddingService.zip`
- **Target Framework**: `.NET 10.0`
- **Konfigurasi Build**: `Release`
- **Struktur Arsip**: Flat structure (siap upload ke ODC External Logic)

---

## [v1.2.1] - 2026-08-15

### ⚡ Kompatibilitas Cloudflare Tunnel & HTTPS
- **Penambahan Default User-Agent**: Menambahkan User-Agent header browser standar pada `HttpClient` agar request dari OutSystems ODC ke endpoint yang diproteksi Cloudflare WAF/Bot Protection (seperti domain tunnel `chromedbwsl.opendv.xyz`) tidak diblokir dengan status `403 Forbidden`.
- **TLS/SSL Handshake Handler**: Mengonfigurasi `HttpClientHandler` untuk mendukung sertifikat SSL proxy/tunnel secara aman dan fleksibel.

### 📦 Informasi Paket
- **Nama File**: `OutSystems.EmbeddingService.zip`
- **Target Framework**: `.NET 10.0`
- **Konfigurasi Build**: `Release`
- **Struktur Arsip**: Flat structure

---

## [v1.2.0] - 2026-08-15

### 🚀 Standarisasi Response & Error Handling
- **Struktur Response Komprehensif**: Semua Server Action kini mengembalikan record response yang memuat status operasi (`Success: Boolean`) dan pesan kesalahan (`ErrorMessage: Text`).
- **Graceful Error Handling**: Kegagalan (misalnya API key tidak valid, jaringan error, parsing gagal) tidak lagi me-raise unhandled exception yang membuat aplikasi crash, melainkan ditangkap secara aman dan dikembalikan melalui `Success = False` beserta deskripsi pesan di `ErrorMessage`.
- **Response Structures Baru**:
  - `UpsertResponse`: Memuat `Success`, `ErrorMessage`, dan `UpsertedCount`.
  - `SearchResponse`: Memuat `Success`, `ErrorMessage`, dan `Results` (List of `SearchResult`).
  - `DeleteResponse`: Memuat `Success` dan `ErrorMessage`.

### 📦 Informasi Paket
- **Nama File**: `OutSystems.EmbeddingService.zip`
- **Target Framework**: `.NET 10.0`
- **Konfigurasi Build**: `Release`
- **Struktur Arsip**: Flat structure

---

## [v1.1.0] - 2026-08-15

### 🚀 Fitur Baru (New Actions)
- **`DeleteDocuments`**: Menghapus satu atau beberapa dokumen dari ChromaDB berdasarkan daftar `DocumentId`.
- **`DeleteByNamespace`**: Menghapus seluruh dokumen yang terikat pada metadata `namespace` tertentu.
- **`DeleteCollection`**: Menghapus seluruh koleksi data vektor dari ChromaDB (mendukung ChromaDB v2 dan fallback v1).

### ⚡ Pembaruan & Peningkatan
- **Upgrade ke .NET 10 (`net10.0`)**: Seluruh library dan dependensi di-build serta di-package menggunakan target runtime .NET 10.
- **Dukungan Format File Lengkap**: Memvalidasi dan mendukung ingest file teks biner seperti `.json`, `.xml`, `.csv`, `.md`, `.txt`, `.yaml` (via UTF-8/Latin-1) dan file `.pdf` (via `PdfPig`).
- **Peningkatan ChromaDB Client**: Penanganan endpoint ChromaDB v2 API yang lebih baik untuk operasi Upsert, Query, dan Delete dengan fallback otomatis ke v1.

### 📦 Informasi Paket
- **Nama File**: `OutSystems.EmbeddingService.zip`
- **Target Framework**: `.NET 10.0`
- **Konfigurasi Build**: `Release`
- **Struktur Arsip**: Flat structure (siap langsung diunggah ke ODC Portal External Logic)

---

## [v1.0.0] - 2026-08-11

### 🚀 Inisialisasi Fitur
- **`UpsertEmbeddings`**: Pembuatan embedding dari teks mentah dan penyimpanan ke ChromaDB.
- **`UpsertFileEmbeddings`**: Ekstraksi teks dari file (PDF & TXT), pemecahan ke dalam chunk, dan penyimpanan ke ChromaDB.
- **`SearchByText`**: Pencarian vektor semantik (top-K) dengan filter namespace opsional.
- Konfigurasi parameter fleksibel via `EmbeddingConfig` (ApiKey, BaseUrl, Model, ChromaUrl, ChromaCollection).
