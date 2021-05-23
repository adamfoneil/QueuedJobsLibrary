using AO.Models.Models;
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
            [QueueTrigger(JobTracker.ZipBuilderQueue, Connection = "ConnectionStrings:Storage")] string data,
            ILogger log, ExecutionContext context)
        {
            int id = int.Parse(data); // int because JobTracker is QueuedJob<int>
            var config = context.GetConfig();

            var databaseConnection = config["ConnectionStrings:Database"];
            log.LogDebug($"Database connection = {databaseConnection}");
            var repo = new JobTrackerRepository(databaseConnection);

            var storageConnection = config["ConnectionStrings:Storage"];
            log.LogDebug($"Storage connection = {storageConnection}");

            var statusUpdateUrl = config["StatusUpdateUrl"];
            new ZipFileBuilder(storageConnection, (id) => $"{statusUpdateUrl}/{id}", repo, log)
                .ExecuteAsync(id)
                .Wait();
        }

        public class ZipFileBuilder : BackgroundJobBase<JobTracker, int, ZipRequest, ZipResult>
        {
            private readonly string _storageConnection;
            private readonly Func<int, string> _endpointBuilder;

            public ZipFileBuilder(string storageConnection, Func<int, string> endpointBuilder, JobTrackerRepository repository, ILogger logger) : base(repository, logger)
            {
                _storageConnection = storageConnection;
                _endpointBuilder = endpointBuilder;
            }

            protected override async Task<ZipResult> OnExecuteAsync(ZipRequest request)
            {
                var containerClient = new BlobContainerClient(_storageConnection, request.ContainerName);
                var pages = containerClient.GetBlobsAsync(prefix: request.BlobPrefix).AsPages();

                const int maxFiles = 10; // to prevent runaway huge files. remember, this is just a little demo
                int count = 0;

                using (var outputStream = new MemoryStream())
                {
                    using (var outputZip = new ZipArchive(outputStream, ZipArchiveMode.Create, true))
                    {
                        await foreach (var page in pages)
                        {
                            foreach (var blob in page.Values)
                            {
                                if (blob.Name.EndsWith(".zip")) continue; // don't include other zip files
                                count++;
                                if (count > maxFiles) break;

                                var blobClient = new BlobClient(_storageConnection, request.ContainerName, blob.Name);
                                using (var inputStream = await blobClient.OpenReadAsync())
                                {
                                    var entry = outputZip.CreateEntry(blob.Name);
                                    using (var entryStream = entry.Open())
                                    {
                                        await inputStream.CopyToAsync(entryStream);
                                    }
                                }
                            }
                        }
                    }

                    var outputBlobClient = new BlobClient(_storageConnection, request.ContainerName, StringId.New(8, StringIdRanges.Lower | StringIdRanges.Numeric) + ".zip");
                    outputStream.Position = 0;
                    await outputBlobClient.UploadAsync(outputStream, new BlobUploadOptions()
                    {
                        HttpHeaders = new BlobHttpHeaders()
                        {
                            ContentType = "application/zip"
                        }
                    });

                    var expirationDate = DateTime.UtcNow.AddDays(3);

                    var sasUri = outputBlobClient.GenerateSasUri(BlobSasPermissions.Read, expirationDate);

                    return new ZipResult()
                    {
                        Url = sasUri.ToString(),
                        ExpiresAfter = expirationDate,
                        BlobName = outputBlobClient.Name
                    };
                }
            }            

            protected override async Task OnStatusUpdatedAsync(int id, JobStatus status) => 
                await PostStatusUpdateAsync(id, _endpointBuilder.Invoke(id));            
        }
    }
}
