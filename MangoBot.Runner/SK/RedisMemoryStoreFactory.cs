using Microsoft.SemanticKernel.Connectors.Redis;
using Microsoft.SemanticKernel.Memory;
using Microsoft.VisualBasic;
using StackExchange.Redis;

#pragma warning disable SKEXP0001
#pragma warning disable SKEXP0020
namespace MangoBot.Runner.SK;

public class RedisMemoryStoreFactory
{
    public static async Task<IMemoryStore> CreateSampleRedisMemoryStoreAsync()
    {
        var configuration = Config.Instance.RedisConnectionString;
        var connectionMultiplexer = await ConnectionMultiplexer.ConnectAsync(configuration);
        IDatabase database = connectionMultiplexer.GetDatabase();
        IMemoryStore store = new RedisMemoryStore(database, vectorSize: 1536);
        return store;
    }
}