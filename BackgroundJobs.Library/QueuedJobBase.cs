﻿using AO.Models.Interfaces;
using Microsoft.Extensions.Logging;
using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;

namespace BackgroundJobs
{
    public enum Status
    {
        Pending,
        Running,
        Succeeded,
        Failed
    }

    public abstract class QueuedJobBase<TRequest, TResult, TKey> : IModel<TKey>
    {
        private static HttpClient _client = new HttpClient();

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

        public async Task ExecuteAsync(IRepository<QueuedJobBase<TRequest, TResult, TKey>, TKey, IUserBase> repository, string callbackUrl, ILogger logger = null)
        {
            var request = JsonSerializer.Deserialize<TRequest>(RequestData);
            TResult result = default;

            try
            {                
                await UpdateAsync(repository, () => 
                { 
                    RetryCount++;
                    ExceptionData = default;
                    Status = Status.Running;
                    Started = DateTime.UtcNow;
                });
                
                result = await OnExecuteAsync(request);

                await UpdateAsync(repository, () =>
                {
                    Completed = DateTime.UtcNow;
                    Status = Status.Succeeded;
                    ResultData = JsonSerializer.Serialize(result);
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
                    request,
                    result,
                    exception = exc
                });

                logger?.LogError(exceptionInfo);
            }
            finally
            {
                try
                {
                    var response = await _client.PostAsync(callbackUrl, JsonContent.Create(this));
                    response.EnsureSuccessStatusCode();
                }
                catch (Exception exc)
                {
                    var message = $"Error executing job completion callback: {callbackUrl}: {exc.Message}";
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
        private async Task UpdateAsync(IRepository<QueuedJobBase<TRequest, TResult, TKey>, TKey, IUserBase> repository, Action updateAction = null)
        {
            updateAction?.Invoke();
            await repository.SaveAsync(GetUser(), this);            
        }      

        protected abstract IUserBase GetUser();
    }
}
