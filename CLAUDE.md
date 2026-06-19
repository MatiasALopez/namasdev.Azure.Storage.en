# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build

This is a .NET Framework 4.5 class library built with MSBuild/Visual Studio.

```
msbuild namasdev.Azure.Storage.en.sln /p:Configuration=Release
```

## NuGet Package Generation and Publishing

From within the `namasdev.Azure.Storage.en/` project directory:

```cmd
nuget pack namasdev.Azure.Storage.en.csproj -Properties Configuration=Release
nuget add namasdev.Azure.Storage.en.1.0.0.nupkg -source \\MATUASUS\nuget
```

The local NuGet feed is hosted at `\\MATUASUS\nuget`.

## Architecture

This is a single-class library (`namasdev.Azure.Storage`) that wraps the `WindowsAzure.Storage` SDK (v9.3.3) to provide a simplified blob storage repository.

**`FilesRepository`** — the only public class. It wraps a `CloudStorageAccount` and exposes operations against Azure Blob Storage:
- Upload (`Add` / `AddAsync`): takes a `namasdev.Core.IO.File` object (Name + Content bytes) and returns the blob URI.
- Download (`Get` / `GetAsync`, `GetBytes` / `GetBytesAsync`): by container+filename+directories or by URI/URL.
- Delete (`Delete` / `DeleteAsync`): by URI or by container+filename+directories.
- Copy / Move blobs between containers (`CopyBlobAsync`, `MoveBlobAsync`).
- List blobs (`ListBlobs`): returns flat listing.
- Utility: `GetFileUrl`, `GetDirectoryName` (static), `GetAsStringAsync`, `SaveToPath`.

**Blob addressing convention**: blobs are addressed by `(container, fileName, ...directories)`. Multiple directories are joined with `/` to form the blob path prefix.

**Dependencies**:
- `namasdev.Core.en` (v1.0.0) — provides `Validator` (argument validation) and `Core.IO.File` (Name + Content).
- `WindowsAzure.Storage` (v9.3.3) — legacy Azure Storage SDK.
- `Newtonsoft.Json` (v10.0.2) and `System.Linq.Dynamic` (v1.0.8) — referenced but not directly used in current code.
