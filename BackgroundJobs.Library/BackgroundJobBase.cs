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

    public abstract class BackgroundJobBase<TResult, TException>
    {
        public BackgroundJobBase(string description)
        {            
            Status = Status.Pending;
            Description = description;
        }

        public string UserName { get; }
        public DateTime? Started { get; private set; }
        public DateTime? Completed { get; private set; }
        public string Description { get; }     
        public Status Status { get; private set; }
        public bool IsCleared { get; private set; }
        public TResult Result { get; private set; }
        public TException Exception { get; private set; }
        public int RetryCount { get; private set; }

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
                    Exception = default;
                    Status = Status.Running;
                    Started = DateTime.UtcNow;
                });
                
                var result = await OnExecuteAsync();

                await StoreAsync(() =>
                {
                    Completed = DateTime.UtcNow;
                    Status = Status.Succeeded;
                    Result = result;
                });                
            }
            catch (Exception exc)
            {
                await StoreAsync(() =>
                {
                    Status = Status.Failed;
                    Completed = DateTime.UtcNow;
                    Exception = ConvertException(exc);
                });                
            }
        }

        /// <summary>
        /// all your work goes here
        /// </summary>
        protected abstract Task<TResult> OnExecuteAsync();

        /// <summary>
        /// typically executes SQL updates to the job itself
        /// </summary>
        public abstract Task StoreAsync(Action updateAction);

        /// <summary>
        /// how do we convert exceptions into an easily storable format?
        /// </summary>
        protected abstract TException ConvertException(Exception exception);
    }
}
