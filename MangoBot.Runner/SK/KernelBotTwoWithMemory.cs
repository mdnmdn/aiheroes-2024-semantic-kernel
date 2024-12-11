using MangoBot.Runner.Utils;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.Embeddings;
using Microsoft.SemanticKernel.Memory;
using Microsoft.SemanticKernel.Plugins.Memory;

namespace MangoBot.Runner.SK;

#pragma warning disable SKEXP0001
#pragma warning disable SKEXP0010
#pragma warning disable SKEXP0050
public class KernelBotTwoWithMemory : BaseKernelBot
{
    private readonly Kernel _kernel;
    private readonly KernelFunction _mainFunction;
    const string MessageCollectionName = "mango-messages";
    private bool _init = false;
    private SemanticTextMemory? _memory;
    private KernelPlugin _memoryPlugin;

    protected override string BotVersion => "v2";

    public KernelBotTwoWithMemory(DiscordEngine engine) : base(engine)
    {
        var builder = Kernel.CreateBuilder();
            
        builder.Services.AddOpenAIChatCompletion(Config.Instance.ChatModel4oMini, Config.Instance.OpenAiToken);
        builder.Services.AddOpenAITextEmbeddingGeneration(Config.Instance.EmbeddingsModelId, Config.Instance.OpenAiToken);
        builder.Services.AddSingleton<ILoggerFactory>(ColorConsole.LoggerFactory());
           
        _kernel  = builder.Build();
        
        _kernel.FunctionInvocationFilters.Add(new LogginFilter());
       
        var promptTemplate =
            """
            Your name is MangoBot and you are discord server bot,
            for the community Chocolate Lovers' Anonymus (CLA),
            be helpful and answer questions about the server and the users.

            If possible use the relevant messages to answer the question.

            if you mention a user prefix the name with @, as example for user MangoBot use @MangoBot.

            -------  Relevant messages   -----------------
            {{Recall}}
            --------------------------------------

            User input:

            {{$input}}
            """;

        _mainFunction = _kernel.CreateFunctionFromPrompt(promptTemplate,
            new OpenAIPromptExecutionSettings()
            {
                MaxTokens = 200, Temperature = 1, TopP = 1
            });
    }

    public override async Task Init()
    {
        // Setup memory
        var embeddingGenerator = _kernel.GetRequiredService<ITextEmbeddingGenerationService>();

        var redisStore = await RedisMemoryStoreFactory.CreateSampleRedisMemoryStoreAsync();
        _memory = new SemanticTextMemory(redisStore, embeddingGenerator);

        var memoryPlugin = new TextMemoryPlugin(_memory);
        _memoryPlugin = _kernel.CreatePluginFromObject(memoryPlugin);
        
        _kernel.Plugins.Add(_memoryPlugin);

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

                var result = await _kernel.InvokeAsync(_mainFunction, new KernelArguments()
                {
                    [TextMemoryPlugin.CollectionParam] = MessageCollectionName,
                    [TextMemoryPlugin.LimitParam] = "2",
                    [TextMemoryPlugin.RelevanceParam] = "0.79",
                    ["input"] = message.Message
                });
                // var result = await _kernel.InvokeAsync(
                //     new ContextVariables()
                //     {
                //         [TextMemoryPlugin.CollectionParam] = MessageCollectionName,
                //         [TextMemoryPlugin.LimitParam] = "2",
                //         [TextMemoryPlugin.RelevanceParam] = "0.79",
                //         ["input"] = message.Message
                //     },
                //     new KernelFunction[]
                //     {
                //         _mainFunction,
                //     });
                
                
                var response = result.GetValue<string>();
                if (response.HasValue())
                    await Discord.SendMessage(message.ChannelId, response!, message.OriginalMessage.Id);

                //await Discord.SendMessage(message.ChannelId, $"Hi {message.Sender} ");
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
            
            ColorConsole.WriteLine($"Function {function} has been invoked");

            if (metadata is not null && metadata.ContainsKey("Usage"))
            {
                ColorConsole.WriteLine($"Token usage: {metadata["Usage"]?.AsJson()}");
            }
        }
    }
}