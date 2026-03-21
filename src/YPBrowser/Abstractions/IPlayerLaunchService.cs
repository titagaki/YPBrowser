using YPBrowser.Models;

namespace YPBrowser.Abstractions;

public interface IPlayerLaunchService
{
    void Launch(ChannelItem channel, PlayerItem player);
    void LaunchWithDefault(ChannelItem channel);
}
