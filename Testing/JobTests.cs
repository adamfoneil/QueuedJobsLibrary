using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using QueuedJobs.Library.Interfaces;
using System;
using System.Threading.Tasks;

namespace Testing
{
    [TestClass]
    public class JobTests
    {
        [TestMethod]
        public void QueueSampleJob()
        {
            var repo = new JobRepository();
            var logger = LoggerFactory.Create(config => config.AddDebug()).CreateLogger("testing");

            var job = new SampleJob("adamo", new OcrRequest()
            {
                ContainerName = "whatever",
                BlobName = "whoosiewhatsie.pdf"
            });

            repo.SaveAsync(job).Wait();

            job.ExecuteAsync(repo, OnCompletedAsync, logger).Wait();
        }

        private Task OnCompletedAsync(IResultData<int, OcrResult> arg)
        {
            throw new NotImplementedException();
        }
    }
}
