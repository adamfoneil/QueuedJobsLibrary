using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Notification.Shared;
using Notification.Shared.Requests;
using QueuedJobs.Extensions;
using static QueuedJobs.Functions.BuildZipFile;

namespace Testing
{
    [TestClass]
    public class JobTests
    {
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
            // since this is testing a real job, I need to pass a real connection string from that project
            var dbConnection = Config["Values:ConnectionStrings:Database"];
            var repo = new JobTrackerRepository(dbConnection);            

            var logger = LoggerFactory.Create(config => config.AddDebug()).CreateLogger("testing");
            var job = QueueClientExtensions.SaveJobAsync(repo, "adamo", new ZipRequest()
            {
                ContainerName = "sample-uploads",
                BlobPrefix = "anonUser"
            }).Result;

            var storageConnection = Config["Values:ConnectionStrings:Storage"];

            var processor = new ZipFileBuilder(storageConnection, repo, logger);
            processor.ExecuteAsync(job.Id).Wait();
        }

        private IConfiguration Config => new ConfigurationBuilder()
            .AddJsonFile("local.settings.json", false)
            .Build();
    }
}
