using System.Threading.Tasks;
using Discord.Commands;

public class PingModule : ModuleBase<SocketCommandContext>
{
    [Command("ping")]
    public async Task PingAsync()
    {
        await ReplyAsync("Pong!");
    }
}
