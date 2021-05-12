using Dapper.CX.SqlServer.Extensions.Int;
using Microsoft.Azure.WebJobs;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Notification.Shared;
using Notification.Shared.Models;
using Notification.Shared.Requests;
using Notification.Shared.Responses;
using QueuedJobs.Abstract;
using QueuedJobs.Functions.Extensions;
using QueuedJobs.Library.Interfaces;
using QueuedJobs.Models;
using System.Threading.Tasks;

namespace QueuedJobs.Functions
{
    public static class BuildZipFile
    {
        [FunctionName("BuildZipFile")]
        public static void Run([QueueTrigger("zip-builder", Connection = "ConnectionString")]string data, 
            ILogger log, ExecutionContext context)
        {
            int id = int.Parse(data);
            var config = context.GetConfig();
            new ZipFileBuilder(
                config["ConnectionStrings:Storage"],
                config["ConnectionStrings:Database"], 
                log).ExecuteAsync(id).Wait();
        }


        public class ZipFileBuilder : JobRunner<JobTracker, int, ZipRequest, ZipResult>
        {
            private readonly string _storageConnection;

            public ZipFileBuilder(string storageConnection, string databaseConnection, ILogger logger) : base(new JobTrackerRepository(databaseConnection), logger)
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
