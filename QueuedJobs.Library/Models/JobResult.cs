namespace QueuedJobs.Models
{
    public class JobResult<TKey, TResult>
    {
        public JobResult(TKey id, TResult result)
        {
            Id = id;
            Result = result;
        }

        public TKey Id { get; }
        public TResult Result { get; }
    }
}
