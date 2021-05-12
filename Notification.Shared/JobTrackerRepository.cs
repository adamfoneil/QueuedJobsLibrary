﻿using Dapper.CX.SqlServer.Extensions.Int;
using Microsoft.Data.SqlClient;
using Notification.Shared.Models;
using QueuedJobs.Library.Interfaces;
using System.Threading.Tasks;

namespace Notification.Shared
{
    public class JobTrackerRepository : IRepository<JobTracker, int>
    {
        private readonly string _connectionString;

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
    }
}
