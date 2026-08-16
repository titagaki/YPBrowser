using System.Diagnostics;
using Microsoft.Extensions.Logging;
using YPBrowser.Abstractions;
using YPBrowser.Models;
using YPBrowser.Settings;

namespace YPBrowser.Services;

public class PlayerLaunchService : IPlayerLaunchService
{
    private readonly ILogger<PlayerLaunchService> _logger;

    public PlayerLaunchService(ILogger<PlayerLaunchService> logger)
    {
        _logger = logger;
    }

    public void Launch(ChannelItem channel, PlayerSettings player)
    {
        try
        {
            var args = PlayerPlaceholders.Expand(player.ArgumentTemplate, channel);
            _logger.LogInformation("Launching {Player} with args: {Args}", player.ExecutableFileName, args);
            Process.Start(new ProcessStartInfo
            {
                FileName = player.ExecutablePath,
                Arguments = args,
                UseShellExecute = false,
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to launch player {Player}", player.ExecutablePath);
            throw;
        }
    }

    public void LaunchWithDefault(ChannelItem channel)
    {
        // Open stream URL with default system handler
        try
        {
            Process.Start(new ProcessStartInfo(channel.StreamUrl) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to open stream URL");
            throw;
        }
    }

}
