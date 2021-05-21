using Dapper.CX.Extensions;
using Dapper.CX.SqlServer.Extensions.Int;
using Microsoft.Extensions.Logging;
using ModelSync.Models;
using QueuedJobs.Abstract;
using QueuedJobs.Library.Interfaces;
using QueuedJobs.Models;
using SqlServer.LocalDb;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
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

    public class SampleJobRunner : BackgroundJobBase<Job, int, OcrRequest, OcrResult>
    {
        public SampleJobRunner(ILogger logger) : base(new JobRepository(), logger)
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

        protected override async Task OnStatusUpdatedAsync(int id, Status status)
        {
            // do nothing
            await Task.CompletedTask;
        }
    }

    [Table("Job", Schema = "queue")]
    public class Job : BackgroundJobInfo<int>
    {
    }

    public class JobRepository : IRepository<Job, int>
    {
        public const string DbName = "QueuedJobs";

        public async Task<Job> GetAsync(int key)
        {
            using (var cn = LocalDb.GetConnection(DbName))
            {
                return await cn.GetAsync<Job>(key);
            }
        }

        public async Task<Job> SaveAsync(Job model)
        {
            using (var cn = LocalDb.GetConnection(DbName))
            {
                await cn.SaveAsync(model);
                return model;
            }
        }

        public async Task CreateTableIfNotExistsAsync()
        {
            using (var cn = LocalDb.GetConnection(DbName))
            {
                var exists = await cn.TableExistsAsync("queue", "Job");
                if (!exists)
                {
                    await DataModel.CreateTablesAsync(new Type[]
                    {
                        typeof(Job)
                    }, cn);
                }
            }
        }
    }
}
