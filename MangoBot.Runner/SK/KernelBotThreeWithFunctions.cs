using MangoBot.Runner.Utils;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.Embeddings;
using Microsoft.SemanticKernel.Memory;
using Microsoft.SemanticKernel.Plugins.Core;
using Microsoft.SemanticKernel.Plugins.Memory;
using Microsoft.SemanticKernel.Plugins.Web;
using Microsoft.SemanticKernel.Plugins.Web.Bing;
using Microsoft.SemanticKernel.Planning;

namespace MangoBot.Runner.SK;

#pragma warning disable SKEXP0001
#pragma warning disable SKEXP0010
#pragma warning disable SKEXP0050
#pragma warning disable SKEXP0060
public class KernelBotThreeWithFunctions : BaseKernelBot
{
    private readonly Kernel _kernel;
    const string MessageCollectionName = "mango-messages";
    private bool _init = false;
    private SemanticTextMemory? _memory;

    protected override string BotVersion => "v3";

    public KernelBotThreeWithFunctions(DiscordEngine engine) : base(engine)
    {
        var builder = Kernel.CreateBuilder();
            
        builder.Services.AddOpenAIChatCompletion(Config.Instance.ChatModel4oMini, Config.Instance.OpenAiToken);
        builder.Services.AddOpenAITextEmbeddingGeneration(Config.Instance.EmbeddingsModelId, Config.Instance.OpenAiToken);
        builder.Services.AddSingleton<ILoggerFactory>(ColorConsole.LoggerFactory());
           
        _kernel  = builder.Build();
        
        _kernel.FunctionInvocationFilters.Add(new LogginFilter());

  
    }

    public override async Task Init()
    {
        // Setup memory
        var embeddingGenerator = _kernel.GetRequiredService<ITextEmbeddingGenerationService>();

        var redisStore = await RedisMemoryStoreFactory.CreateSampleRedisMemoryStoreAsync();
        _memory = new SemanticTextMemory(redisStore, embeddingGenerator);

        var memoryPlugin = new TextMemoryPlugin(_memory);
        var kernelPlugin = _kernel.CreatePluginFromObject(memoryPlugin);
        
        _kernel.Plugins.Add(kernelPlugin);

        _init = true;
        await base.Init();


        // setup time plugin
        _kernel.ImportPluginFromObject(new TimePlugin(), "TimePlugin");

        // Setup web search
        if (Config.Instance.BingSearchToken.HasValue())
        {
            var bingConnector = new BingConnector(Config.Instance.BingSearchToken);
            var bing = new WebSearchEnginePlugin(bingConnector);
            _kernel.ImportPluginFromObject(bing, "bing");
        }

        // setup discord plugin
        var discordPlugin = new DiscordPlugin(Discord);
        _kernel.ImportPluginFromObject(discordPlugin, "discord");


        // // setup planner
        // var config = new FunctionCallingStepwisePlannerOptions()
        // {
        //     SemanticMemoryConfig = new SemanticMemoryConfig
        //     {
        //         Memory = _memory,
        //     },
        //     MaxIterations = 15,
        //     MinIterationTimeMs = 0,
        //     MaxTokens = 8000,
        // };
        // _planner = new FunctionCallingStepwisePlanner(config);

        _init = true;
        await base.Init();
    }

    protected override async Task OnMessage(ChatMessage message)
    {
        if (!_init) throw new Exception("Init has not been called");
        try
        {
            // save in memory all the messages that are not mentions and are longer than 6 characters
            if (!message.IsMention)
            {
                if (message.Message is { Length: > 6 })
                {
                    var id = $"{DateTime.Now:O}:{message.Sender}";
                    await _memory!.SaveInformationAsync(MessageCollectionName,
                        $"{message.Sender} at {DateTime.Now:O} said {message.Message}", id);
                }
            }
            else
            {
                await Discord.SetTyping(message.OriginalMessage.Channel);

                var input = $"""
                             Your name is MangoBot and you are discord server bot,
                             for the community Chocolate Lovers' Anonymus (CLA),
                             be helpful and answer questions about the server and the users.
                             
                             In order do get information about the user you could use the following functions: Recall
                             with the default collection {MessageCollectionName}
                             The finale answer and the direct messages have to be in the same language of the initial message.
                             If the you are answering to the initial user don't send a direct message but use the final answer.

                             The user sending the message is {message.Sender} and the message is:

                             -----------------
                             {message.Message}
                             -----------------
                             """;

                OpenAIPromptExecutionSettings executionSettings = new() { FunctionChoiceBehavior = FunctionChoiceBehavior.Auto() };
                var result = await _kernel.InvokePromptAsync(input, new ( executionSettings ));
                var response = result.GetValue<string>();
                if (response.HasValue())
                    await Discord.SendMessage(message.ChannelId, response!, message.OriginalMessage.Id);
            }
        }
        catch (Exception e)
        {
            await Discord.SendMessage(message.ChannelId, e.ToString());
            Logger.Error(e);
        }
    }
    
    class LogginFilter : IFunctionInvocationFilter
    {
        public async Task OnFunctionInvocationAsync(FunctionInvocationContext context, Func<FunctionInvocationContext, Task> next)
        {
            var function = $"{context.Function.PluginName ?? "@"}.{context.Function.Name}";
            ColorConsole.WriteLine($"Function {function} is being invoked");

            await next(context);

            
            var metadata = context.Result?.Metadata;
            
            // function = $"{context.Function.PluginName ?? "@"}.{context.Function.Name}";
            ColorConsole.WriteLine($"Function {function} has been invoked");

            // if (metadata is not null && metadata.ContainsKey("Usage"))
            // {
            //     this._output.WriteLine($"Token usage: {metadata["Usage"]?.AsJson()}");
            // }
        }
    }
}