using AO.Models.Interfaces;
using System.Threading.Tasks;

namespace QueuedJobs.Library.Interfaces
{
    public interface IRepository<TModel, TKey> where TModel : IModel<TKey>
    {
        Task<TModel> GetAsync(TKey key);
        Task<TKey> SaveAsync(TModel model);        
    }
}
