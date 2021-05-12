using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;
using Microsoft.Extensions.Logging;
using QueuedJobs.Library.Interfaces;
using System;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace QueuedJobs.Library.Extensions
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

        public static async Task<TKey> QueueJobAsync<TRequest, TResult, TKey>(this QueueClient queueClient, 
            QueuedJobBase<TRequest, TResult, TKey> job, TRequest request,
            IRepository<QueuedJobBase<TRequest, TResult, TKey>, TKey> repository,
            ILogger logger)
        {           
            job.RequestData = JsonSerializer.Serialize(request);
            job.Status = Status.Pending;
            job.Started = null;
            job.Completed = null;

            // this enables visibility of the job over its lifetime
            var key = await repository.SaveAsync(job);
            logger.LogDebug($"Request Id {key} was saved with data {job.RequestData}");

            try
            {
                // this queues the job for execution
                await SendJsonAsync(queueClient, key);
                logger.LogDebug($"Request Id {key} was added to queue {queueClient.Name}");
            }
            catch (Exception exc)
            {
                // if queuing failed, then we need to indicate that in the permanent record
                logger.LogError($"Error queuing request Id {key} on {queueClient.Name}: {exc.Message}");
                job.Status = Status.Aborted;
                await repository.SaveAsync(job);
            }
                
            return key;
        }
    }
}
