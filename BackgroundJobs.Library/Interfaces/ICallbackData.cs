namespace QueuedJobs.Library.Interfaces
{
    public interface ICallbackData<TKey, TResult>
    {
        TKey Id { get; }        
        TResult Result { get; }        
    }
}
