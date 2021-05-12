using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration;

namespace QueuedJobs.Functions.Extensions
{
    public static class ExecutionContextExtensions
    {
        public static IConfiguration GetConfig(this ExecutionContext context) => new ConfigurationBuilder()
            .AddJsonFile("local.settings.json", true)
            .Build();
    }
}
