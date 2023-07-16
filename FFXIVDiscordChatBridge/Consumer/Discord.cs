using Discord.WebSocket;
using FFXIVDiscordChatBridge.Producer;
using NLog;

namespace FFXIVDiscordChatBridge.Consumer;

internal class Discord
{
    private readonly string _discordChannelId;
    private readonly Logger _logger = LogManager.GetCurrentClassLogger();

    public Discord(DiscordClientWrapper discordWrapper)
    {
        _discordChannelId = discordWrapper.Channel!.Id.ToString();
        discordWrapper.Client.MessageReceived += ClientOnMessageReceived;
    }
    
    private Task ClientOnMessageReceived(SocketMessage socketMessage)
    {
        if (socketMessage.Channel.Id.ToString() != _discordChannelId)
        {
            return Task.CompletedTask;
        }

        var message = socketMessage.Content;
        _logger.Info($"Received message from Discord: {message}");
        return Task.CompletedTask;
    }
}