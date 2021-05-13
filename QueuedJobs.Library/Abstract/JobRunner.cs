using Microsoft.Extensions.Logging;
using QueuedJobs.Library.Interfaces;
using QueuedJobs.Models;
using System;
using System.Text.Json;
using System.Threading.Tasks;

namespace QueuedJobs.Abstract
{
    public abstract class JobRunner<TJob, TKey, TRequest, TResult> where TJob : QueuedJob<TKey>
    {
        private readonly IRepository<TJob, TKey> _repository;
        private readonly ILogger _logger;

        public JobRunner(IRepository<TJob, TKey> repository, ILogger logger)
        {
            _repository = repository;
            _logger = logger;
        }

        /// <summary>
        /// this is where all your work goes
        /// </summary>
        protected abstract Task<TResult> OnExecuteAsync(TRequest request);

        /// <summary>
        /// this gets called on completion of job
        /// </summary>
        protected abstract Task OnCompletedAsync(TKey id, Status status, TResult result);

        /// <summary>
        /// call this in your QueueTrigger Azure Function. The id will be the queue message data
        /// </summary>
        public async Task ExecuteAsync(TKey id)
        {
            var errorContext = "starting";
            try
            {
                var job = await _repository.GetAsync(id);
                if (job == null) throw new Exception($"Job Id {id} not found.");
                if (!job.RequestType.Equals(typeof(TRequest).Name)) throw new Exception($"Job Id {id} request type {job.RequestType} does not match job runner request type {typeof(TRequest).Name}");

                var request = JsonSerializer.Deserialize<TRequest>(job.RequestData);

                TResult result = default;
                try
                {
                    errorContext = "executing";
                    job.RetryCount++;
                    job.ExceptionData = null;
                    job.Status = Status.Running;
                    job.Started = DateTime.UtcNow;
                    await _repository.SaveAsync(job);

                    result = await OnExecuteAsync(request);
                    job.ResultData = JsonSerializer.Serialize(result);
                    job.Status = Status.Succeeded;
                }
                catch (Exception exc)
                {
                    _logger.LogError(exc, $"Job Id {job.Id} failed: {exc.Message}");
                    job.Status = Status.Failed;
                    job.ExceptionData = JsonSerializer.Serialize(exc);
                }
                finally
                {
                    errorContext = "finishing";
                    job.Completed = DateTime.UtcNow;
                    job.Duration = Convert.ToInt32(job.Completed.Value.Subtract(job.Started.Value).TotalSeconds);
                    await _repository.SaveAsync(job);

                    try
                    {
                        await OnCompletedAsync(job.Id, job.Status, result);
                    }
                    catch (Exception exc)
                    {
                        var message = $"Error executing job completion callback for job Id {job.Id}: {exc.Message}";
                        _logger.LogError(message);
                    }
                }
            }
            catch (Exception exc)
            {
                _logger.LogError(exc, $"While {errorContext}: {exc.Message}");
            }
        }
    }
}
