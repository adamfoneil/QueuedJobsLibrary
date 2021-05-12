using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using QueuedJobs.Extensions;

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
       
    }
}
