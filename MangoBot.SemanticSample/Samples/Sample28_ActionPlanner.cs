// using MangoBot.SemanticSample.Plugins;
// using MangoBot.SemanticSample.Utils;
// using Microsoft.SemanticKernel;
// using Microsoft.SemanticKernel.Plugins.Memory;
// using Microsoft.SemanticKernel.TemplateEngine;
//
// namespace MangoBot.SemanticSample.Samples;
//
// public class Sample28_ActionPlanner
// {
//     public static async Task RunAsync()
//     {
//         Console.WriteLine("======== Action Planner ========");
//         string openAIModelId = Constants.OpenAIChatModel;
//         string openAIApiKey = Constants.OpenAIToken;
//
//         var builder = Kernel.CreateBuilder();
//         builder.AddOpenAIChatCompletion(Constants.OpenAIChatModel, Constants.OpenAIToken);
//         builder.Services.AddSingleton<ILoggerFactory>(ConsoleLogger.LoggerFactory);
//         
//             
//         var kernel = builder.Build();
//
//         string samplesDirectory = RepoFiles.SamplePluginsPath();
//         kernel.CreatePluginFromPromptDirectory(samplesDirectory, "SummarizePlugin");
//         kernel.CreatePluginFromPromptDirectory(samplesDirectory, "WriterPlugin");
//         kernel.CreatePluginFromPromptDirectory(samplesDirectory, "FunPlugin");
//
//         // Create an optional config for the ActionPlanner. Use this to exclude plugins and functions if needed
//         var config = new ActionPlannerConfig();
//         config.ExcludedFunctions.Add("MakeAbstractReadable");
//
//         // Create an instance of ActionPlanner.
//         // The ActionPlanner takes one goal and returns a single function to execute.
//         var planner = new ActionPlanner(kernel, config: config);
//
//         // We're going to ask the planner to find a function to achieve this goal.
//         var goal = "Write a joke about Cleopatra in the style of Hulk Hogan.";
//
//         // The planner returns a plan, consisting of a single function
//         // to execute and achieve the goal requested.
//         var plan = await planner.CreatePlanAsync(goal);
//
//         // Execute the full plan (which is a single function)
//         var result = await plan.InvokeAsync(kernel);
//
//         // Show the result, which should match the given goal
//         Console.WriteLine(result.GetValue<string>());
//
//
//         Console.WriteLine("======== Without Action Planner ========");
//
//         kernel.ImportSemanticFunctionsFromDirectory(samplesDirectory, "MdnPlugin");
//         //var result_no_plan = await kernel.RunAsync(goal,);
//         //Console.WriteLine(result_no_plan.GetValue<string>()); 
//
//         /* Output:
//          *
//          * Cleopatra was a queen
//          * But she didn't act like one
//          * She was more like a teen
//
//          * She was always on the scene
//          * And she loved to be seen
//          * But she didn't have a queenly bone in her body
//          */
//     }
// }