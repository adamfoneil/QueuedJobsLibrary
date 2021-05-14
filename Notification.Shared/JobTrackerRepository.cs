using Dapper;
using Dapper.CX.SqlServer.Extensions.Int;
using Microsoft.Data.SqlClient;
using Notification.Shared.Models;
using QueuedJobs.Library.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Notification.Shared
{
    public class JobTrackerRepository : IRepository<JobTracker, int>
    {
        private readonly string _connectionString;

        public EventHandler<JobTracker> StatusUpdated;

        public JobTrackerRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<JobTracker> GetAsync(int key)
        {
            using (var cn = new SqlConnection(_connectionString))
            {
                return await cn.GetAsync<JobTracker>(key);
            }
        }

        public async Task<JobTracker> SaveAsync(JobTracker model)
        {
            using (var cn = new SqlConnection(_connectionString))
            {
                await cn.SaveAsync(model);
                return model;
            }
        }

        public async Task<IEnumerable<JobTracker>> ActiveJobsByUser(string userName)
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

        public async Task OnStatusUpdatedAsync(int id)
        {
            using (var cn = new SqlConnection(_connectionString))
            {
                var job = await cn.GetAsync<JobTracker>(id);
                StatusUpdated?.Invoke(this, job);
            }
        }
    }
}
