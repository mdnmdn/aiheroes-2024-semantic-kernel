using MangoBot.Runner.Utils;
using System.ComponentModel;
using Microsoft.SemanticKernel;

namespace MangoBot.Runner.SK;

public class DiscordPlugin(DiscordEngine discordEngine)
{
    [KernelFunction, Description("List all the groups of the current discord server in json format")]
    public string ListGroups()
    {
        Console.WriteLine("DiscordPlugin.ListGroups");
        var groups = discordEngine.ListGroups().AsJson();
        return groups;
    }

    [KernelFunction, Description("List all the user of the current discord server in json format")]
    public async Task<string> ListUsers()
    {
        Console.WriteLine("DiscordPlugin.ListUsers");
        var users = await discordEngine.ListUsers();
        return users.AsJson();
    }


    [KernelFunction, Description("Given a user an a message, send a message to the user")]
    public async Task<string> SendMessage(
        [Description("The username of the recipient of the message")]
        string username,
        [Description("The message body")] string message)
    {
        ColorConsole.WriteInfo($"DiscordPlugin.SendMessage {username}");
        if (discordEngine.GetUser(username) == null)
        {
            ColorConsole.WriteWarning($"user ${username} not found");
            return $"user ${username} not found, use ListUsers to get the list of users";
        }

        await discordEngine.SendMessageToUser(username, message);
        ColorConsole.WriteSuccess($"  => message sent to user: {username}: {message}");
        return "message sent";
    }


    [KernelFunction, Description("Given a user an a message, send a message to the user after an amount of time")]
    public async Task<string> SendMessageDelayed(
        [Description("The username of the recipient of the message")]
        string username,
        [Description("The message body")] string message,
        [Description("Delay in second for sending the message")] int delayInSeconds)

    {
        ColorConsole.WriteInfo($"DiscordPlugin.SendMessageDelayed {username}");
        if (discordEngine.GetUser(username) == null)
        {
            ColorConsole.WriteWarning($"user ${username} not found");
            return $"user ${username} not found, use ListUsers to get the list of users";
        }

        // that's ok, we don't need to await this task, but don't do it in production
        Task.Run(async () =>
        {
            await Task.Delay(TimeSpan.FromSeconds(delayInSeconds));
            ColorConsole.WriteSuccess($"  => message sent delayed of {delayInSeconds}s to user: {username}: {message}");
            //discordEngine.SetTyping()
            await discordEngine.SendMessageToUser(username, message);
        });

        ColorConsole.WriteSuccess($"  => message scheduled for user: {username}");
        return "message scheduled";
    }
}