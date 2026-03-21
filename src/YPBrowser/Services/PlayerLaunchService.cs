using System.Diagnostics;
using System.Text;
using Microsoft.Extensions.Logging;
using YPBrowser.Helpers;
using YPBrowser.Models;

namespace YPBrowser.Services;

public class PlayerLaunchService
{
    private readonly ILogger<PlayerLaunchService> _logger;

    public PlayerLaunchService(ILogger<PlayerLaunchService> logger)
    {
        _logger = logger;
    }

    public void Launch(ChannelItem channel, PlayerItem player)
    {
        try
        {
            var url = channel.StreamUrl;
            string args;

            if (player.UsePlaylistFile)
            {
                var plsPath = WritePlsFile(channel);
                args = player.ArgumentTemplate.Replace("{url}", plsPath).Replace("{file}", plsPath);
            }
            else
            {
                args = player.ArgumentTemplate.Replace("{url}", url);
            }

            _logger.LogInformation("Launching {Player} with args: {Args}", player.Name, args);
            Process.Start(new ProcessStartInfo
            {
                FileName = player.ExecutablePath,
                Arguments = args,
                UseShellExecute = false,
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to launch player {Player}", player.Name);
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

    private static string WritePlsFile(ChannelItem channel)
    {
        var path = Path.Combine(Path.GetTempPath(), $"ypbrowser_{channel.Id}.pls");
        var content = PlaylistWriter.BuildPls(channel);
        File.WriteAllText(path, content, Encoding.UTF8);
        return path;
    }
}
