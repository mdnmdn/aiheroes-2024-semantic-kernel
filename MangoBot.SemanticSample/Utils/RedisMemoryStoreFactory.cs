using System.Diagnostics.CodeAnalysis;
using Microsoft.SemanticKernel.Connectors.Redis;
using Microsoft.SemanticKernel.Memory;
using StackExchange.Redis;

namespace MangoBot.SemanticSample.Utils;

public class RedisMemoryStoreFactory
{
    [Experimental("SKEXP0001")]
    public static async Task<IMemoryStore> CreateSampleRedisMemoryStoreAsync()
    {
        string configuration = Constants.RedisConnectionString;
        ConnectionMultiplexer connectionMultiplexer = await ConnectionMultiplexer.ConnectAsync(configuration);
        IDatabase database = connectionMultiplexer.GetDatabase();
        
        IMemoryStore store = new RedisMemoryStore(database, vectorSize: 1536);
        return store;
    }
}