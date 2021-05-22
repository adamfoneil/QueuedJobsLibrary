using AO.Models.Interfaces;
using QueuedJobs.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace QueuedJobs.Library.Abstract
{
    public abstract class JobRepositoryBase<TModel, TKey> where TModel : BackgroundJobInfo<TKey>
    {
        /// <summary>
        /// retrieve job model from store
        /// </summary>
        public abstract Task<TModel> GetAsync(TKey key);

        /// <summary>
        /// persist job model in store
        /// </summary>
        public abstract Task<TModel> SaveAsync(TModel model);

        public abstract Task<IEnumerable<TModel>> ActiveJobsByUserAsync(string userName);

        public async Task ClearJobAsync(TKey key)
        {
            var job = await GetAsync(key);
            job.IsCleared = true;
            await SaveAsync(job);
        }

        /// <summary>
        /// fetch the job model and call UpdatedEvent for that job
        /// </summary>
        public async Task OnUpdatedAsync(TKey key)
        {
            var model = await GetAsync(key);
            Updated?.Invoke(model);
        }

        /// <summary>
        /// invoked within OnUpdatedAsync
        /// </summary>
        public event Action<TModel> Updated;
    }
}
