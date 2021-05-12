using Dapper.CX.SqlServer.Extensions.Int;
using QueuedJobs.Library;
using QueuedJobs.Library.Interfaces;
using SqlServer.LocalDb;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Testing
{
    /// <summary>
    /// request to do something with a blob
    /// </summary>
    public class OcrRequest
    {
        public string ContainerName { get; set; }
        public string BlobName { get; set; }        
    }

    /// <summary>
    /// result of some activity
    /// </summary>
    public class OcrResult
    {
        public IEnumerable<string> Words { get; set; }
    }

    public class SampleJob : QueuedJobBase<OcrRequest, OcrResult, int>
    {
        public SampleJob(string userName, OcrRequest request) : base(userName, request)
        {
        }

        protected override async Task<OcrResult> OnExecuteAsync(OcrRequest request)
        {
            Thread.Sleep(3000);

            return await Task.FromResult(new OcrResult()
            {
                Words = new string[]
                {
                    "this",
                    "that",
                    "other"
                }
            });
        }
    }

    public class JobRepository : IRepository<SampleJob, int>
    {
        private const string DbName = "QueuedJobs";

        public async Task<SampleJob> GetAsync(int key)
        {
            using (var cn = LocalDb.GetConnection(DbName))
            {
                return await cn.GetAsync<SampleJob>(key);
            }
        }

        public async Task<int> SaveAsync(SampleJob model)
        {
            using (var cn = LocalDb.GetConnection(DbName))
            {
                return await cn.SaveAsync(model);
            }
        }
    }
}
