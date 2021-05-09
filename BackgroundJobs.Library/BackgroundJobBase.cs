using System;
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

    public abstract class BackgroundJobBase<TRequest, TResult, TKey>
    {
        public BackgroundJobBase(TRequest request)
        {            
            Status = Status.Pending;
            RequestData = Serialize(request);
        }

        public TKey Id { get; set; }
        public string RequestData { get; set; }
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

        public async Task ExecuteAsync()
        {
            try
            {                
                await StoreAsync(() => 
                { 
                    RetryCount++;
                    ExceptionData = default;
                    Status = Status.Running;
                    Started = DateTime.UtcNow;
                });

                var request = Deserialize<TRequest>(RequestData);
                var result = await OnExecuteAsync(request);

                await StoreAsync(() =>
                {
                    Completed = DateTime.UtcNow;
                    Status = Status.Succeeded;
                    ResultData = Serialize(result);
                });                
            }
            catch (Exception exc)
            {
                await StoreAsync(() =>
                {
                    Status = Status.Failed;
                    Completed = DateTime.UtcNow;
                    ExceptionData = Serialize(exc);
                });                
            }
        }

        /// <summary>
        /// all your work goes here
        /// </summary>
        protected abstract Task<TResult> OnExecuteAsync(TRequest request);

        /// <summary>
        /// typically executes SQL updates to the job itself
        /// </summary>
        public abstract Task<TKey> StoreAsync(Action updateAction = null);      

        /// <summary>
        /// how do we perform json serialization?
        /// </summary>
        protected abstract string Serialize(object @object);

        /// <summary>
        /// how do we serialize to json?
        /// </summary>
        protected abstract T Deserialize<T>(string json);
    }
}
