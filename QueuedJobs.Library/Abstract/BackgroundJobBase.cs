using Microsoft.Extensions.Logging;
using QueuedJobs.Extensions;
using QueuedJobs.Library.Interfaces;
using QueuedJobs.Models;
using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace QueuedJobs.Abstract
{
    public abstract class BackgroundJobBase<TJob, TKey, TRequest, TResult> where TJob : BackgroundJobInfo<TKey>
    {
        private static HttpClient _client = new HttpClient();

        private readonly IRepository<TJob, TKey> _repository;

        protected readonly ILogger Logger;

        public bool PostStatusUpdates { get; set; } = true;

        public BackgroundJobBase(IRepository<TJob, TKey> repository, ILogger logger)
        {
            _repository = repository;
            Logger = logger;
        }

        /// <summary>
        /// this is where all your work goes
        /// </summary>
        protected abstract Task<TResult> OnExecuteAsync(TRequest request);

        /// <summary>
        /// this gets called when job status changes
        /// </summary>
        protected abstract Task OnStatusUpdatedAsync(TKey id, Status status);

        /// <summary>
        /// call this in your QueueTrigger Azure Function. The id will be the queue message data
        /// </summary>
        public async Task<TResult> ExecuteAsync(TKey id)
        {
            TResult result = default;
            TJob job = default;

            var errorContext = "starting";

            try
            {
                job = await _repository.GetAsync(id);
                if (job == null) throw new Exception($"Job Id {id} not found.");
                if (!job.RequestType.Equals(typeof(TRequest).Name)) throw new Exception($"Job Id {id} request type {job.RequestType} does not match job runner request type {typeof(TRequest).Name}");

                var request = JsonSerializer.Deserialize<TRequest>(job.RequestData);

                try
                {
                    errorContext = "executing";
                    job.RetryCount++;
                    job.ExceptionData = null;
                    job.Status = Status.Running;
                    job.Started = DateTime.UtcNow;
                    await _repository.SaveAsync(job);
                    if (PostStatusUpdates) await OnStatusUpdatedAsync(id, job.Status);

                    result = await OnExecuteAsync(request);
                    job.ResultData = JsonSerializer.Serialize(result);
                    job.Status = Status.Succeeded;
                }
                catch (Exception exc)
                {
                    errorContext = "failing";
                    Logger.LogError(exc, $"Job Id {job.Id} failed: {exc.Message}");
                    job.Status = Status.Failed;
                    job.ExceptionData = JsonSerializer.Serialize(new
                    {
                        message = exc.FullMessage(),
                        data = exc.Data,
                        stackTrace = exc.StackTrace
                    });
                }
                finally
                {
                    errorContext = "finishing";
                    job.Completed = DateTime.UtcNow;
                    job.Duration = Convert.ToInt32(job.Completed.Value.Subtract(job.Started.Value).TotalSeconds);
                    await _repository.SaveAsync(job);
                    if (PostStatusUpdates) await OnStatusUpdatedAsync(id, job.Status);
                }
            }
            catch (Exception exc)
            {
                if (job != null)
                {
                    job.Status = Status.Aborted;
                    job.ExceptionData = JsonSerializer.Serialize(new
                    {
                        errorContext,
                        message = exc.FullMessage()
                    });
                    await _repository.SaveAsync(job);
                    if (PostStatusUpdates) await OnStatusUpdatedAsync(id, job.Status);
                }

                Logger.LogError(exc, $"While {errorContext}: {exc.Message}");
            }

            return result;
        }

        protected async Task PostStatusUpdateAsync(TKey id, string endpoint)
        {
            try
            {
                var response = await _client.PostAsync(endpoint, null);
                response.EnsureSuccessStatusCode();
                Logger.LogTrace($"Posted status update on job Id {id} at URL {endpoint}");
            }
            catch (Exception exc)
            {
                Logger.LogError(exc, $"Error posting status update for job Id {id} at URL {endpoint}: {exc.Message}");
            }
        }
    }
}
