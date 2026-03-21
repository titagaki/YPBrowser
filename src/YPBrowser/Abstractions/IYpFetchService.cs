using YPBrowser.Models;
using YPBrowser.Settings;

namespace YPBrowser.Abstractions;

public interface IYpFetchService
{
    Task<List<ChannelItem>> FetchAsync(YpServerItem server, NetworkSettings network, CancellationToken ct = default);
}
