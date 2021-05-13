using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Notification.Shared;
using Notification.Shared.Requests;
using QueuedJobs.Extensions;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using static QueuedJobs.Functions.BuildZipFile;

namespace Testing
{
    [TestClass]
    public class JobTests
    {
        private static HttpClient _client = new HttpClient();

        [TestMethod]
        public void VerySimpleOfflineJob()
        {
            var repo = new JobRepository();
            repo.CreateTableIfNotExistsAsync().Wait();

            var logger = LoggerFactory.Create(config => config.AddDebug()).CreateLogger("testing");

            var job = QueueClientExtensions.SaveJobAsync(repo, "adamo", new OcrRequest() { BlobName = "whatever.pdf", ContainerName = "hello" }).Result;

            var processor = new SampleJobRunner(logger);
            processor.ExecuteAsync(job.Id).Wait();
        }

        [TestMethod]
        public void ZipBuilderJob()
        {
            // since this is testing a real job, I need to pass real connection strings from that project
            var dbConnection = Config["Values:ConnectionStrings:Database"];
            var repo = new JobTrackerRepository(dbConnection);

            var logger = LoggerFactory.Create(config => config.AddDebug()).CreateLogger("testing");

            // I already have some blobs in this location in this account.
            // a more robust test would upload some files first rather than assume these
            var job = QueueClientExtensions.SaveJobAsync(repo, "adamo", new ZipRequest()
            {
                ContainerName = "sample-uploads",
                BlobPrefix = "anonUser"
            }).Result;

            var storageConnection = Config["Values:ConnectionStrings:Storage"];

            var function = new ZipFileBuilder(storageConnection, repo, logger);
            var result = function.ExecuteAsync(job.Id).Result;

            // verify the download works
            var response = _client.GetAsync(result.Url).Result;
            response.EnsureSuccessStatusCode();

            // and is a valid zip file
            var tempFile = Path.Combine(Path.GetTempPath(), Path.GetTempFileName() + ".zip");
            using (var downloadFile = File.Create(tempFile))
            {
                response.Content.CopyToAsync(downloadFile).Wait();
            }

            using (var zipFile = ZipFile.OpenRead(tempFile))
            {
                Assert.IsTrue(zipFile.Entries.Any());
            }

            File.Delete(tempFile);
        }

        private IConfiguration Config => new ConfigurationBuilder()
            .AddJsonFile("local.settings.json", false)
            .Build();
    }
}
