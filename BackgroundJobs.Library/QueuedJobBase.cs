using AO.Models.Interfaces;
using Microsoft.Extensions.Logging;
using QueuedJobs.Library.Interfaces;
using System;
using System.Text.Json;
using System.Threading.Tasks;

namespace QueuedJobs.Library
{
    public enum Status
    {
        Pending, // job queued, but not sstarted yet
        Running, // job is currently running
        Succeeded, // no errors in QueuedJobBase.OnExecuteAsync
        Failed, // error in QueuedJobBase.OnExecuteAsync
        Aborted // job faied to queue (bad Azure connection string?)
    }

    public abstract class QueuedJobBase<TRequest, TResult, TKey> : IModel<TKey>, IResultData<TKey, TResult>
    {
        public QueuedJobBase(TRequest request)
        {            
            Status = Status.Pending;
            RequestData = JsonSerializer.Serialize(request);
        }

        public TKey Id { get; set; }
        public string UserName { get; set; }
        public string RequestData { get; set; }
        public DateTime Created { get; set; } = DateTime.UtcNow;
        public DateTime? Started { get; set; }
        public DateTime? Completed { get; set; }        
        public Status Status { get; set; }
        public bool IsCleared { get; set; }
        public string ResultData { get; set; }
        public string ExceptionData { get; set; }
        public int RetryCount { get; set; }

        public TimeSpan? Duration =>
            (!Started.HasValue) ? default :
            (Started.HasValue && !Completed.HasValue) ? DateTime.UtcNow.Subtract(Started.Value) :
            (Started.HasValue && Completed.HasValue) ? Completed.Value.Subtract(Started.Value) :
            default;

        public bool IsRunning => Status == Status.Running;
        
        public TRequest Request { get; private set; }
        public TResult Result { get; private set; }

        public async Task ExecuteAsync(
            IRepository<QueuedJobBase<TRequest, TResult, TKey>, TKey> repository, 
            Func<IResultData<TKey, TResult>, Task> callback, 
            ILogger logger = null)
        {
            Request = JsonSerializer.Deserialize<TRequest>(RequestData);            

            try
            {                
                await UpdateAsync(repository, () => 
                { 
                    RetryCount++;
                    ExceptionData = default;
                    Status = Status.Running;
                    Started = DateTime.UtcNow;
                });
                
                Result = await OnExecuteAsync(Request);
                ResultData = JsonSerializer.Serialize(Result);

                await UpdateAsync(repository, () =>
                {
                    Completed = DateTime.UtcNow;
                    Status = Status.Succeeded;                    
                });
            }
            catch (Exception exc)
            {
                await UpdateAsync(repository, () =>
                {
                    Status = Status.Failed;
                    Completed = DateTime.UtcNow;
                    ExceptionData = JsonSerializer.Serialize(exc);
                });

                var exceptionInfo = JsonSerializer.Serialize(new
                {
                    request = Request,
                    result = Result,
                    exception = exc
                });

                logger?.LogError(exceptionInfo);
            }
            finally
            {
                try
                {                    
                    await callback.Invoke(this);
                }
                catch (Exception exc)
                {
                    var message = $"Error executing job completion callback for job Id {Id}: {exc.Message}";
                    logger?.LogError(message);
                }                
            }
        }

        /// <summary>
        /// all your work goes here
        /// </summary>
        protected abstract Task<TResult> OnExecuteAsync(TRequest request);

        /// <summary>
        /// typically executes SQL updates to the job itself
        /// </summary>
        private async Task UpdateAsync(IRepository<QueuedJobBase<TRequest, TResult, TKey>, TKey> repository, Action updateAction = null)
        {
            updateAction?.Invoke();
            await repository.SaveAsync(this);            
        }      
    }
}
