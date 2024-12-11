using MangoBot.Runner.Utils;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.Plugins.Web;

namespace MangoBot.Runner.SK;

/// <summary>
/// Simple bot 
/// </summary>
public class KernelBotOne : BaseKernelBot
{
    private readonly Kernel _kernel;
    private readonly KernelFunction _mainFunction;

    protected override string BotVersion => "v1";

    public KernelBotOne(DiscordEngine engine) : base(engine)
    {
        var builder = Kernel.CreateBuilder();
            
        builder.Services.AddOpenAIChatCompletion(Config.Instance.ChatModel4oMini, Config.Instance.OpenAiToken);
        builder.Services.AddSingleton<ILoggerFactory>(ColorConsole.LoggerFactory());
           
        _kernel  = builder.Build();

        var promptTemplate =
            """
            Your name is MangoBot and you are a discord server bot  
            for the community Chocolate Lovers' Anonymus (CLA)
            be helpful and answer questions about the server and the users.
            Answer the questions as best as you can with a fun a nice mood.
            
            
            ===
            {{$input}}
            """;

        _mainFunction = _kernel.CreateFunctionFromPrompt(promptTemplate,
            new OpenAIPromptExecutionSettings()
            {
                MaxTokens = 200, Temperature = 1, TopP = 1
            });
    }


    protected override async Task OnMessage(ChatMessage message)
    {
        try
        {
            if (message.IsMention)
            {
                await Discord.SetTyping(message.OriginalMessage.Channel);

                
                var result = await _kernel.InvokeAsync(_mainFunction, new KernelArguments() { ["input"] = message.Message });
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
}