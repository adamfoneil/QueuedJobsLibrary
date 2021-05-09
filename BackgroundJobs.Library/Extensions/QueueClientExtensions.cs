using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;
using System;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace QueuedJobs.Library.Extensions
{
    public static class QueueClientExtensions
    {
        public static async Task<SendReceipt> SendJsonAsync<T>(this QueueClient queueClient, T @object)
        {
            var json = JsonSerializer.Serialize(@object);
            var bytes = Encoding.UTF8.GetBytes(json);
            var base64string = Convert.ToBase64String(bytes);
            return await queueClient.SendMessageAsync(base64string);
        }
    }
}
