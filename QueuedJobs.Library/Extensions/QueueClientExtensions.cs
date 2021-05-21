using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;
using Microsoft.Extensions.Logging;
using QueuedJobs.Library.Interfaces;
using QueuedJobs.Models;
using System;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace QueuedJobs.Extensions
{
    public static class QueueClientExtensions
    {
        public static async Task<SendReceipt> SendJsonAsync<T>(this QueueClient queueClient, T @object)
        {
            var json = JsonSerializer.Serialize(@object);
            var bytes = Encoding.UTF8.GetBytes(json);
            var base64string = Convert.ToBase64String(bytes);
            return await queueClient.SendMessageAsync(base64string);
        }

        /// <summary>
        /// call this from your client projects to queue a job
        /// </summary>
        public static async Task<TKey> QueueJobAsync<TJob, TRequest, TKey>(this QueueClient queueClient,
            string userName, TRequest request,
            IRepository<TJob, TKey> repository,
            ILogger logger) where TJob : BackgroundJobInfo<TKey>, new()
        {
            // this enables visibility of the job over its lifetime (enabling dashboards, notifications, retry and tracking features)
            var job = await SaveJobAsync(repository, userName, request);
            logger.LogDebug($"Job Id {job.Id} was saved with request data {job.RequestData}");

            try
            {
                // this queues the job for execution
                await SendJsonAsync(queueClient, job.Id);
                logger.LogDebug($"Job Id {job.Id} was added to queue {queueClient.Name}");
            }
            catch (Exception exc)
            {
                // if queuing failed, then we need to indicate that in the stored record
                logger.LogError($"Error queuing Job Id {job.Id} on {queueClient.Name}: {exc.Message}");
                job.Status = Status.Aborted;
                await repository.SaveAsync(job);
            }

            return job.Id;
        }

        public static async Task<TJob> SaveJobAsync<TJob, TRequest, TKey>(
            this IRepository<TJob, TKey> repository, string userName, TRequest request)
            where TJob : BackgroundJobInfo<TKey>, new()
        {
            var job = new TJob();
            job.UserName = userName;
            job.RequestType = typeof(TRequest).Name;
            job.RequestData = JsonSerializer.Serialize(request);
            job.Status = Status.Pending;
            job.Started = null;
            job.Completed = null;
            job.Created = DateTime.UtcNow;

            return await repository.SaveAsync(job);
        }
    }
}
