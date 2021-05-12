namespace QueuedJobs.Library.Interfaces
{
    public interface IResultData<TKey, TResult>
    {
        TKey Id { get; }        
        TResult Result { get; }        
    }
}
