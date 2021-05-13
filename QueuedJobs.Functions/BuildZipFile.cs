using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Notification.Shared;
using Notification.Shared.Models;
using Notification.Shared.Requests;
using Notification.Shared.Responses;
using QueuedJobs.Abstract;
using QueuedJobs.Functions.Extensions;
using QueuedJobs.Models;
using StringIdLibrary;
using System;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;

namespace QueuedJobs.Functions
{
    public static class BuildZipFile
    {
        [FunctionName("BuildZipFile")]
        public static void Run(
            [QueueTrigger("zip-builder", Connection = "ConnectionString")]string data, 
            ILogger log, ExecutionContext context)
        {
            int id = int.Parse(data); // int because JobTracker is QueuedJob<int>
            var config = context.GetConfig();

            var databaseConnection = config["ConnectionStrings:Database"];
            var repo = new JobTrackerRepository(databaseConnection);

            var storageConnection = config["ConnectionStrings:Storage"];

            new ZipFileBuilder(storageConnection, repo, log)
                .ExecuteAsync(id)
                .Wait();
        }

        public class ZipFileBuilder : JobRunner<JobTracker, int, ZipRequest, ZipResult>
        {
            private readonly string _storageConnection;

            public ZipFileBuilder(string storageConnection, JobTrackerRepository repository, ILogger logger) : base(repository, logger)
            {
                _storageConnection = storageConnection;
            }

            protected override async Task<ZipResult> OnExecuteAsync(ZipRequest request)
            {
                var containerClient = new BlobContainerClient(_storageConnection, request.ContainerName);
                var pages = containerClient.GetBlobsAsync(prefix: request.BlobPrefix).AsPages();

                const int maxFiles = 10; // to prevent runaway huge files. remember, this is just a little demo
                int count = 0;

                using (var outputStream = new MemoryStream())
                {
                    using (var outputZip = new ZipArchive(outputStream, ZipArchiveMode.Create))
                    {
                        await foreach (var page in pages)
                        {
                            foreach (var blob in page.Values)
                            {
                                count++;
                                if (count > maxFiles) break;

                                var blobClient = new BlobClient(_storageConnection, request.ContainerName, blob.Name);
                                var entry = outputZip.CreateEntry(blob.Name);
                                using (var entryStream = entry.Open())
                                {
                                    await blobClient.UploadAsync(entryStream);
                                }
                            }
                        }
                    }

                    var outputBlobClient = new BlobClient(_storageConnection, request.ContainerName, StringId.New(8, StringIdRanges.Lower | StringIdRanges.Numeric) + ".zip");
                    await outputBlobClient.UploadAsync(outputStream, new BlobUploadOptions()
                    {
                        HttpHeaders = new BlobHttpHeaders()
                        {
                            ContentType = "application/zip"
                        }
                    });

                    var expirationDate = DateTime.UtcNow.AddDays(3);

                    var sasUri = outputBlobClient.GenerateSasUri(new BlobSasBuilder()
                    {
                        BlobContainerName = request.ContainerName,
                        BlobName = outputBlobClient.Name,                        
                        ExpiresOn = expirationDate
                    });

                    return new ZipResult()
                    {
                        Url = sasUri.ToString(),
                        ExpiresAfter = expirationDate
                    };
                }
            }

            protected override async Task OnCompletedAsync(int id, Status status, ZipResult result)
            {
                // do nothing for now
                await Task.CompletedTask;
            }
        }       
    }
}
