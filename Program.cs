using Discord;
using Discord.WebSocket;
using System;
using Discord.Rest;
using System.Threading.Tasks;

class Program
{
    private DiscordSocketClient _client;

    public static void Main(string[] args)
    {
        Console.WriteLine("Please enter your bot token:");
        string token = Console.ReadLine();

        new Program().MainAsync(token).GetAwaiter().GetResult();
    }

    public async Task MainAsync(string token)
    {
        _client = new DiscordSocketClient();
        _client.UserBanned += UserBannedAsync;

        _client.Log += LogAsync;

        await _client.LoginAsync(TokenType.Bot, token);
        await _client.StartAsync();


        _client.Ready += async () =>
        {
            int guildCount = _client.Guilds.Count;
            var application = await _client.GetApplicationInfoAsync();
            Console.WriteLine($"");
            Console.WriteLine($"===========================================");
            Console.WriteLine($"Bot is connected to {guildCount} servers.");
            Console.WriteLine($"Bot owner: {application.Owner.Username}");
            Console.WriteLine($"===========================================");
            Console.WriteLine($"");

            while (true)
            {
                Console.WriteLine("Enter a command ('exit' to quit, 'help' for command list):");
                string input = Console.ReadLine();

                if (input == "help")
                {
                    Console.WriteLine($"Command 'servers' - list all servers");
                    Console.WriteLine($"Command 'leave' - leave all servers");
                    Console.WriteLine($"Command 'send message' - send youre message to all servers.");
                    Console.WriteLine($"Command 'list channels <ID>' - Check server channels.");
                    Console.WriteLine($"Command 'logger <ID>' - instaling to channel logger.");
                    Console.WriteLine($"Command 'set status <link>' - Change status bot.");
                    Console.WriteLine($"Command 'set avatar <link>' - Change avatar bot.");
                    Console.WriteLine($"Command 'send channels <ID>' - send to all channels message.");
                    Console.WriteLine($"Command 'send message <ID>' - send to channel message.");
                    Console.WriteLine($"Command 'rename all channels' - rename all chanels :).");
                }
                else if (input == "servers")
                {
                    Console.WriteLine();
                    foreach (var guild in _client.Guilds)
                    {
                        var owner = guild.Owner;
                        Console.WriteLine($"Server Name: {guild.Name} (ID: {guild.Id})");
                    }
                    Console.WriteLine();
                }
                else if (input == "leave")
                {
                    foreach (var guild in _client.Guilds)
                    {
                        await guild.LeaveAsync();
                        Console.WriteLine($"Left server: {guild.Name}");
                    }
                }
                else if (input == "send message")
                {
                    Console.WriteLine("Enter the message to send:");
                    string messageToSend = Console.ReadLine();
                    foreach (var guild in _client.Guilds)
                    {
                        var channels = guild.TextChannels;

                        foreach (var channel in channels)
                        {
                            try
                            {
                                await channel.SendMessageAsync(messageToSend);
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Error sending message in server '{guild.Name}', channel '{channel.Name}': {ex.Message}");

                                if (ex.Message.Contains("Missing Permissions"))
                                {
                                    Console.WriteLine("Bot does not have permissions to send messages in this channel.");
                                }
                            }
                        }

                        Console.WriteLine($"Message sent to all channels in server: {guild.Name}");
                    }
                }
                else if (input.StartsWith("list channels"))
                {
                    string[] parts = input.Split(' ');
                    if (parts.Length != 3)
                    {
                        Console.WriteLine("Invalid command format. Please use 'list channels {guild_id}'.");
                        continue;
                    }

                    ulong guildId;
                    if (!ulong.TryParse(parts[2], out guildId))
                    {
                        Console.WriteLine("Invalid guild ID. Please provide a valid guild ID.");
                        continue;
                    }

                    var guild = _client.GetGuild(guildId);
                    if (guild == null)
                    {
                        Console.WriteLine("Guild not found. Make sure the bot is part of the specified guild.");
                        continue;
                    }

                    Console.WriteLine($"Server Name: {guild.Name} (ID: {guild.Id})");
                    Console.WriteLine("Channels:");
                    Console.WriteLine();

                    foreach (var channel in guild.Channels)
                    {
                        Console.WriteLine($"- {channel.Name} (ID: {channel.Id})");
                    }
                    Console.WriteLine();
                }
                else if (input.StartsWith("logger "))
                {
                    string[] parts = input.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length != 2 || !ulong.TryParse(parts[1], out ulong channelId))
                    {
                        Console.WriteLine("Invalid command format. Please use 'logger {channel_id}'.");
                        continue;
                    }

                    var channel = _client.GetChannel(channelId) as SocketTextChannel;
                    if (channel == null)
                    {
                        Console.WriteLine("Channel not found. Make sure the bot can access the specified channel.");
                        continue;
                    }

                    _client.MessageReceived += async (msg) =>
                    {
                        if (msg.Channel.Id == channelId)
                        {

                            Console.ForegroundColor = ConsoleColor.Yellow;
                            string authorInfo = $"{msg.Author.Username}: {msg.Content}";
                            Console.Write(authorInfo);

                            Console.ResetColor();
                        }
                    };

                    Console.WriteLine($"Logging enabled for channel '{channel.Name}' to console including player messages.");
                }



                else if (input.StartsWith("set status"))
                {
                    string[] parts = input.Split(' ');
                    if (parts.Length < 3)
                    {
                        Console.WriteLine("Invalid command format. Please use 'set status {status}'.");
                        continue;
                    }

                    string status = string.Join(" ", parts.Skip(2)); // Собираем все слова после "set status" в строку

                    await _client.SetGameAsync(status); // Устанавливаем статус бота

                    Console.WriteLine($"Bot status set to: {status}");
                }
                else if (input.StartsWith("set avatar"))
                {
                    string[] parts = input.Split(' ');
                    if (parts.Length < 3)
                    {
                        Console.WriteLine("Invalid command format. Please use 'set avatar {avatar_url}'.");
                        continue;
                    }

                    string avatarUrl = parts[2];

                    using (var http = new HttpClient())
                    {
                        var avatarImage = await http.GetByteArrayAsync(avatarUrl);
                        using (var stream = new MemoryStream(avatarImage))
                        {
                            await _client.CurrentUser.ModifyAsync(properties =>
                            {
                                properties.Avatar = new Image(stream);
                            });
                        }
                    }

                    Console.WriteLine("Bot avatar updated.");
                }
                else if (input.StartsWith("rename all channel"))
                {
                    string[] parts = input.Split(' ');
                    if (parts.Length < 4)
                    {
                        Console.WriteLine("Invalid command format. Please use 'rename all channel {new_channel_name}'.");
                        continue;
                    }

                    string newName = string.Join(" ", parts.Skip(3));

                    foreach (var guild in _client.Guilds)
                    {
                        var textChannels = guild.TextChannels;

                        foreach (var channel in textChannels)
                        {
                            try
                            {
                                await channel.ModifyAsync(properties =>
                                {
                                    properties.Name = newName;
                                });
                                Console.WriteLine($"Channel renamed: {channel.Name} to {newName}");
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Error renaming channel '{channel.Name}': {ex.Message}");
                            }
                        }
                    }

                    Console.WriteLine($"All text channels renamed to: {newName}");
                }

                else if (input.StartsWith("send channels"))
                {
                    string[] parts = input.Split(' ');
                    if (parts.Length != 3 || !ulong.TryParse(parts[2], out ulong serverId))
                    {
                        Console.WriteLine("Invalid command format. Please use 'send channel {server_id}'.");
                        continue;
                    }

                    var server = _client.GetGuild(serverId);
                    if (server == null)
                    {
                        Console.WriteLine("Server not found. Make sure the bot is in the specified server.");
                        continue;
                    }

                    var channels = server.TextChannels;

                    Console.WriteLine("Enter the message to send to all channels:");
                    string messageToSend = Console.ReadLine();

                    foreach (var channel in channels)
                    {
                        try
                        {
                            await channel.SendMessageAsync(messageToSend);
                            Console.WriteLine($"Message sent to channel '{channel.Name}' in server '{server.Name}'.");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error sending message to channel '{channel.Name}': {ex.Message}");

                            if (ex.Message.Contains("Missing Permissions"))
                            {
                                Console.WriteLine("Bot does not have permissions to send messages in this channel.");
                            }
                        }
                    }

                    Console.WriteLine($"Message sent to all channels on server '{server.Name}'.");
                }



                else if (input == "exit")
                {
                    break;
                }
            }
        };

        await Task.Delay(-1);
    }

    private async Task UserBannedAsync(SocketUser user, SocketGuild guild)
    {
        Console.WriteLine($"{user.Username} был забанен на сервере {guild.Name}!");
    }

    private async Task UserLeftAsync(SocketGuildUser user)
    {
        Console.WriteLine($"User {user.Username} has left server {user.Guild.Name}.");
        Console.WriteLine($"{user.Username} was kicked from server {user.Guild.Name}!");
    }


    private Task LogAsync(LogMessage log)
    {
        Console.WriteLine(log.ToString());
        return Task.CompletedTask;
    }
}
