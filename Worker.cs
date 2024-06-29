using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

public class Worker : IHostedService
{
    private readonly ILogger<Worker> _logger;
    private readonly IConfiguration _configuration;
    private DiscordSocketClient _client;
    private CommandService _commands;
    private IServiceProvider _services;

    public Worker(ILogger<Worker> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _client = new DiscordSocketClient();
        _commands = new CommandService();

        _client.Log += LogAsync;
        _commands.Log += LogAsync;

        _client.MessageReceived += HandleCommandAsync;
        _client.Ready += ReadyAsync;

        var token = _configuration["DiscordToken"];

        await _commands.AddModulesAsync(Assembly.GetEntryAssembly(), _services);

        await _client.LoginAsync(TokenType.Bot, token);
        await _client.StartAsync();
    }

    private async Task HandleCommandAsync(SocketMessage messageParam)
    {
        var message = messageParam as SocketUserMessage;
        if (message == null)
        {
            _logger.LogInformation("Message is not a SocketUserMessage.");
            return;
        }

        if (message.Author.IsBot) return;

        _logger.LogInformation("Received message: {0}", message.Content);

        int argPos = 0;
        if (!(message.HasCharPrefix('!', ref argPos) ||
              message.HasMentionPrefix(_client.CurrentUser, ref argPos)))
        {
            _logger.LogInformation("Message does not contain a command prefix.");
            return;
        }

        var context = new SocketCommandContext(_client, message);

        _logger.LogInformation("Executing command: {0}", message.Content);

        var result = await _commands.ExecuteAsync(
            context: context,
            argPos: argPos,
            services: _services);

        if (!result.IsSuccess)
        {
            _logger.LogError("Command execution failed: {0}", result.ErrorReason);
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await _client.StopAsync();
        await _client.LogoutAsync();
    }

    private Task LogAsync(LogMessage logMessage)
    {
        _logger.LogInformation(logMessage.ToString());
        return Task.CompletedTask;
    }

    private Task ReadyAsync()
    {
        _logger.LogInformation("Bot is connected!");
        return Task.CompletedTask;
    }
}
