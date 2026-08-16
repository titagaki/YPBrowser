using YPBrowser.Models;
using YPBrowser.Settings;

namespace YPBrowser.Abstractions;

public interface IPlayerLaunchService
{
    void Launch(ChannelItem channel, PlayerSettings player);
    void LaunchWithDefault(ChannelItem channel);
}
