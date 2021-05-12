using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Notification.Shared;
using Notification.Shared.Models;
using Notification.Shared.Requests;
using Notification.Shared.Responses;
using QueuedJobs.Abstract;
using QueuedJobs.Functions.Extensions;
using QueuedJobs.Models;
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

            protected override Task<ZipResult> OnExecuteAsync(ZipRequest request)
            {
                throw new System.NotImplementedException();
            }

            protected override Task OnCompletedAsync(int id, Status status, ZipResult result)
            {
                throw new System.NotImplementedException();
            }
        }       
    }
}
