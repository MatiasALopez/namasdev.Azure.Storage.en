using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

using namasdev.Core.Validation;

namespace namasdev.Azure.Storage
{
    public class FilesRepository
    {
        private readonly BlobServiceClient _blobServiceClient;

        public FilesRepository(string connectionString)
        {
            Validator.ValidateRequiredArgumentAndThrow(connectionString, nameof(connectionString));

            _blobServiceClient = new BlobServiceClient(connectionString);
        }

        public string GetFileUrl(string container, string fileName,
            params string[] directories)
        {
            return GetBlobClient(container, fileName, directories).Uri.AbsoluteUri;
        }

        public static string GetDirectoryName(string url)
        {
            if (String.IsNullOrWhiteSpace(url))
            {
                return url;
            }

            var parts = Path.GetDirectoryName(new Uri(url).PathAndQuery)
                .Substring(1)
                .Split('\\');

            return parts.Length > 1
                ? String.Join("/", parts.Skip(1))
                : String.Empty;
        }

        public async Task<string> AddAsync(string container, Core.IO.File file,
            params string[] directories)
        {
            Validator.ValidateRequiredArgumentAndThrow(file, nameof(file));

            var blobClient = GetBlobClient(container, file.Name, directories);
            await blobClient.UploadAsync(BinaryData.FromBytes(file.Content), overwrite: true);
            return blobClient.Uri.AbsoluteUri;
        }

        public string Add(string container, Core.IO.File file,
            params string[] directories)
        {
            Validator.ValidateRequiredArgumentAndThrow(file, nameof(file));

            var blobClient = GetBlobClient(container, file.Name, directories);
            blobClient.Upload(BinaryData.FromBytes(file.Content), overwrite: true);
            return blobClient.Uri.AbsoluteUri;
        }

        public Core.IO.File Get(string container, string fileName,
            params string[] directories)
        {
            var blobClient = GetBlobClient(container, fileName, directories);
            return new Core.IO.File
            {
                Name = Path.GetFileName(fileName),
                Content = DownloadBytes(blobClient)
            };
        }

        public async Task<Core.IO.File> GetAsync(string container, string fileName,
            params string[] directories)
        {
            var blobClient = GetBlobClient(container, fileName, directories);
            return new Core.IO.File
            {
                Name = Path.GetFileName(fileName),
                Content = await DownloadBytesAsync(blobClient)
            };
        }

        public Core.IO.File Get(string url)
        {
            return new Core.IO.File
            {
                Name = Path.GetFileName(url),
                Content = GetBytes(url),
            };
        }

        public async Task<Core.IO.File> GetAsync(string url)
        {
            return new Core.IO.File
            {
                Name = Path.GetFileName(url),
                Content = await GetBytesAsync(url),
            };
        }

        public byte[] GetBytes(string url)
        {
            return GetBytes(new Uri(url));
        }

        public async Task<byte[]> GetBytesAsync(string url)
        {
            return await GetBytesAsync(new Uri(url));
        }

        public byte[] GetBytes(Uri uri)
        {
            return DownloadBytes(GetBlobClient(uri));
        }

        public async Task<byte[]> GetBytesAsync(Uri uri)
        {
            return await DownloadBytesAsync(GetBlobClient(uri));
        }

        private byte[] DownloadBytes(BlobClient blobClient)
        {
            return blobClient.DownloadContent().Value.Content.ToArray();
        }

        private async Task<byte[]> DownloadBytesAsync(BlobClient blobClient)
        {
            var response = await blobClient.DownloadContentAsync();
            return response.Value.Content.ToArray();
        }

        public IEnumerable<BlobItem> ListBlobs(string container,
            params string[] directories)
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(container);
            var prefix = directories != null && directories.Any()
                ? String.Join("/", directories) + "/"
                : null;
            return containerClient.GetBlobs(prefix: prefix);
        }

        public async Task<string> GetAsStringAsync(Uri uri)
        {
            var bytes = await GetBytesAsync(uri);
            return Encoding.UTF8.GetString(bytes);
        }

        public void SaveToPath(string url, string filePath)
        {
            SaveToPath(new Uri(url), filePath);
        }

        public void SaveToPath(Uri uri, string filePath)
        {
            GetBlobClient(uri).DownloadTo(filePath);
        }

        public async Task CopyBlobAsync(Uri blobUri, string destinationContainerName,
            params string[] directories)
        {
            var sourceClient = GetBlobClient(blobUri);
            var bytes = await DownloadBytesAsync(sourceClient);
            var destClient = GetBlobClient(destinationContainerName, Path.GetFileName(sourceClient.Name), directories);
            await destClient.UploadAsync(BinaryData.FromBytes(bytes), overwrite: true);
        }

        public async Task MoveBlobAsync(Uri blobUri, string destinationContainerName,
            params string[] directories)
        {
            await CopyBlobAsync(blobUri, destinationContainerName, directories);
            await DeleteAsync(blobUri);
        }

        public async Task DeleteAsync(Uri blobUri)
        {
            await GetBlobClient(blobUri).DeleteIfExistsAsync();
        }

        public async Task DeleteAsync(string container, string fileName,
            params string[] directories)
        {
            await GetBlobClient(container, fileName, directories).DeleteIfExistsAsync();
        }

        public void Delete(Uri blobUri)
        {
            GetBlobClient(blobUri).DeleteIfExists();
        }

        public void Delete(string container, string fileName,
            params string[] directories)
        {
            GetBlobClient(container, fileName, directories).DeleteIfExists();
        }

        private BlobClient GetBlobClient(string container, string fileName,
            params string[] directories)
        {
            var blobName = directories != null && directories.Any()
                ? String.Join("/", directories) + "/" + fileName
                : fileName;
            return _blobServiceClient.GetBlobContainerClient(container).GetBlobClient(blobName);
        }

        private BlobClient GetBlobClient(Uri blobUri)
        {
            var builder = new BlobUriBuilder(blobUri);
            return _blobServiceClient
                .GetBlobContainerClient(builder.BlobContainerName)
                .GetBlobClient(builder.BlobName);
        }
    }
}
