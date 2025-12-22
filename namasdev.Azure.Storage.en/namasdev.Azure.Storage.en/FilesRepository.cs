using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;

using namasdev.Core.Validation;

namespace namasdev.Azure.Storage
{
    public class FilesRepository
    {
        private CloudStorageAccount _account;

        public FilesRepository(CloudStorageAccount account)
        {
            Validator.ValidateRequiredArgumentAndThrow(account, nameof(account));

            _account = account;
        }

        private CloudBlobClient _blobClient;
        protected CloudBlobClient BlobClient
        {
            get { return _blobClient ?? (_blobClient = _account.CreateCloudBlobClient()); }
        }

        public string GetFileUrl(string container, string fileName,
            params string[] directories)
        {
            var blob = GetBlobReference(container, fileName, directories);
            return blob.Uri.AbsoluteUri;
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

            var blob = GetBlobReference(container, file.Name, directories);
            await blob.UploadFromByteArrayAsync(file.Content, 0, file.Content.Length);

            return blob.Uri.AbsoluteUri;
        }

        public string Add(string container, Core.IO.File file, 
            params string[] directories)
        {
            Validator.ValidateRequiredArgumentAndThrow(file, nameof(file));

            var blob = GetBlobReference(container, file.Name, directories);
            blob.UploadFromByteArray(file.Content, 0, file.Content.Length);

            return blob.Uri.AbsoluteUri;
        }

        public Core.IO.File Get(string container, string fileName,
            params string[] directories)
        {
            var blob = GetBlobReference(container, fileName, directories);
            return new Core.IO.File
            {
                Name = Path.GetFileName(fileName),
                Content = GetBytes(blob)
            };
        }

        public async Task<Core.IO.File> GetAsync(string container, string fileName,
            params string[] directories)
        {
            var blob = GetBlobReference(container, fileName, directories);
            return new Core.IO.File
            {
                Name = Path.GetFileName(fileName),
                Content = await GetBytesAsync(blob)
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
            var blob = BlobClient.GetBlobReferenceFromServer(uri);

            blob.FetchAttributes();
            var bytes = new byte[blob.Properties.Length];
            blob.DownloadToByteArray(bytes, 0);

            return bytes;
        }

        public async Task<byte[]> GetBytesAsync(Uri uri)
        {
            var blob = BlobClient.GetBlobReferenceFromServer(uri);

            blob.FetchAttributes();
            var bytes = new byte[blob.Properties.Length];
            await blob.DownloadToByteArrayAsync(bytes, 0);

            return bytes;
        }

        private byte[] GetBytes(ICloudBlob blob)
        {
            blob.FetchAttributes();
            var bytes = new byte[blob.Properties.Length];
            blob.DownloadToByteArray(bytes, 0);

            return bytes;
        }

        private async Task<byte[]> GetBytesAsync(ICloudBlob blob)
        {
            blob.FetchAttributes();
            var bytes = new byte[blob.Properties.Length];
            await blob.DownloadToByteArrayAsync(bytes, 0);

            return bytes;
        }

        private CloudBlockBlob GetBlobReference(
            string container, string fileName,
            params string[] directories)
        {
            if (directories == null || !directories.Any())
            {
                return GetContainerReference(container)
                    .GetBlockBlobReference(fileName);
            }
            else
            {
                return GetDirectorioReference(container, directories)
                    .GetBlockBlobReference(fileName);
            }
        }

        public IEnumerable<IListBlobItem> ListBlobs(string container,
            params string[] directories)
        {
            if (directories == null || !directories.Any())
            {
                return GetContainerReference(container)
                    .ListBlobs(useFlatBlobListing: true);
            }
            else
            {
                return GetDirectorioReference(container, directories)
                    .ListBlobs(useFlatBlobListing: true);
            }
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
            var blob = BlobClient.GetBlobReferenceFromServer(uri);
            blob.DownloadToFile(filePath, FileMode.CreateNew);
        }

        public async Task CopyBlobAsync(Uri blobUri, string destinationContainerName,
            params string[] directories)
        {
            var sourceBlob = BlobClient.GetBlobReferenceFromServer(blobUri);
            var sourceBlobBytes = GetBytes(sourceBlob);

            var destinationBlob = GetBlobReference(destinationContainerName, Path.GetFileName(sourceBlob.Name), directories);
            await destinationBlob.UploadFromByteArrayAsync(sourceBlobBytes, 0, sourceBlobBytes.Length);
        }

        public async Task MoveBlobAsync(Uri blobUri, string destinationContainerName,
            params string[] directories)
        {
            await CopyBlobAsync(blobUri, destinationContainerName, directories);
            await DeleteAsync(blobUri);
        }

        public async Task DeleteAsync(Uri blobUri)
        {
            var blob = BlobClient.GetBlobReferenceFromServer(blobUri);
            await blob.DeleteIfExistsAsync();
        }

        public async Task DeleteAsync(string container, string fileName, 
            params string[] directories)
        {
            var blob = GetBlobReference(container, fileName, directories);
            await blob.DeleteIfExistsAsync();
        }

        public void Delete(Uri blobUri)
        {
            var blob = BlobClient.GetBlobReferenceFromServer(blobUri);
            blob.DeleteIfExists();
        }

        public void Delete(string container, string fileName, 
            params string[] directories)
        {
            var blob = GetBlobReference(container, fileName, directories);
            blob.DeleteIfExists();
        }

        private CloudBlobContainer GetContainerReference(string container)
        {
            return BlobClient.GetContainerReference(container);
        }

        private CloudBlobDirectory GetDirectorioReference(string container, string[] directories)
        {
            Validator.ValidateRequiredArgumentAndThrow(directories, nameof(directories));

            return BlobClient
                .GetContainerReference(container)
                .GetDirectoryReference(String.Join("/", directories));
        }
    }
}
