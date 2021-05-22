using Dapper;
using Dapper.CX.SqlServer.Extensions.Int;
using Microsoft.Data.SqlClient;
using Notification.Shared.Models;
using QueuedJobs.Library.Abstract;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Notification.Shared
{
    public class JobTrackerRepository : JobRepositoryBase<JobTracker, int>
    {
        private readonly string _connectionString;
        
        public JobTrackerRepository(string connectionString)
        {
            _connectionString = connectionString;
        }              

        public override async Task<IEnumerable<JobTracker>> ActiveJobsByUserAsync(string userName)
        {
            using (var cn = new SqlConnection(_connectionString))
            {
                return await cn.QueryAsync<JobTracker>(
                    "SELECT * FROM [queue].[JobTracker] WHERE [UserName]=@userName AND [IsCleared]=0 ORDER BY [Created] ASC",
                    new { userName });
            }
        }

        public async Task ClearNotificationAsync(int id)
        {
            using (var cn = new SqlConnection(_connectionString))
            {
                var job = await cn.GetAsync<JobTracker>(id);
                job.IsCleared = true;
                await cn.UpdateAsync(job, m => m.IsCleared);
            }
        }

        public override async Task<JobTracker> GetAsync(int key)
        {
            using (var cn = new SqlConnection(_connectionString))
            {
                return await cn.GetAsync<JobTracker>(key);
            }
        }

        public override async Task<JobTracker> SaveAsync(JobTracker model)
        {
            using (var cn = new SqlConnection(_connectionString))
            {
                await cn.SaveAsync(model);
                return model;
            }
        }
    }
}
