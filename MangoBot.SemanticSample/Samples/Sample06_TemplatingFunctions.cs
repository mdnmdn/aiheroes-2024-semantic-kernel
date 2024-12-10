using MangoBot.SemanticSample.Plugins;
using MangoBot.SemanticSample.Utils;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.TemplateEngine;

namespace MangoBot.SemanticSample.Samples;

public class Sample06_TemplatingFunctions
{
    public static async Task RunAsync()
    {
        Console.WriteLine("======== TemplateLanguage ========");

        string openAIModelId = Constants.OpenAIChatModel;
        string openAIApiKey = Constants.OpenAIToken;

        if (openAIModelId == null || openAIApiKey == null)
        {
            Console.WriteLine("OpenAI credentials not found. Skipping example.");
            return;
        }

        var builder = Kernel.CreateBuilder();
        
        builder.Services.AddOpenAIChatCompletion(openAIModelId, openAIApiKey);
        builder.Services.AddSingleton<ILoggerFactory>(ConsoleLogger.LoggerFactory);
        var kernel = builder.Build();

        // Load native plugin into the kernel function collection, sharing its functions with prompt templates
        // Functions loaded here are available as "time.*"
        kernel.ImportPluginFromObject(new TimePlugin(), "time");

        // Semantic Function invoking time.Date and time.Time native functions
        const string FunctionDefinition = @"
Today is: {{time.Date}}
Current time is: {{time.Time}}

Answer to the following questions using JSON syntax, including the data used.
Is it morning, afternoon, evening, or night (morning/afternoon/evening/night)?
Is it weekend time (weekend/not weekend)?
";

        // This allows to see the prompt before it's sent to OpenAI
        Console.WriteLine("--- Rendered Prompt");
        var promptTemplateFactory = new KernelPromptTemplateFactory();
        var promptTemplate = promptTemplateFactory.Create(new PromptTemplateConfig(FunctionDefinition) { });
        var renderedPrompt = await promptTemplate.RenderAsync(kernel);
        Console.WriteLine(renderedPrompt);

        // Run the prompt / semantic function
        var kindOfDay =
            kernel.CreateFunctionFromPrompt(FunctionDefinition, new OpenAIPromptExecutionSettings() { MaxTokens = 100 });

        // Show the result
        Console.WriteLine("--- Semantic Function result");
        var result = await kernel.InvokeAsync(kindOfDay);
        Console.WriteLine(result.GetValue<string>());

        /* OUTPUT:

            --- Rendered Prompt

            Today is: Friday, April 28, 2023
            Current time is: 11:04:30 PM

            Answer to the following questions using JSON syntax, including the data used.
            Is it morning, afternoon, evening, or night (morning/afternoon/evening/night)?
            Is it weekend time (weekend/not weekend)?

            --- Semantic Function result

            {
                "date": "Friday, April 28, 2023",
                "time": "11:04:30 PM",
                "period": "night",
                "weekend": "weekend"
            }
         */
    }
}