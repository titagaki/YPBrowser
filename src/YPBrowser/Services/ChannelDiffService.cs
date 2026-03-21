using YPBrowser.Models;

namespace YPBrowser.Services;

public class ChannelDiffService
{
    private static readonly TimeSpan LogExpiry = TimeSpan.FromHours(1);

    // Log channels by YP name: channels that disappeared from the live list
    private readonly Dictionary<string, List<ChannelItem>> _logByYp = [];

    public void ApplyDiff(IList<ChannelItem> oldList, IList<ChannelItem> newList)
    {
        var oldById = oldList
            .GroupBy(c => c.Id)
            .ToDictionary(g => g.Key, g => g.First());

        foreach (var ch in newList)
        {
            if (oldById.TryGetValue(ch.Id, out var prev))
            {
                // Channel existed before
                if (ch.Listeners > prev.Listeners)
                    ch.Diff = ChannelDiff.Up;
                else if (ch.Listeners < prev.Listeners && prev.Listeners >= 0)
                    ch.Diff = ChannelDiff.Down;
                else if (HasChanged(prev, ch))
                    ch.Diff = ChannelDiff.Changed;
                else
                    ch.Diff = ChannelDiff.None;
            }
            else
            {
                // Check log list
                var log = GetLog(ch.YpName);
                var logEntry = log.FirstOrDefault(l => l.Id == ch.Id);
                if (logEntry != null)
                {
                    log.Remove(logEntry);
                    ch.Diff = ChannelDiff.None; // re-appeared from log
                }
                else
                {
                    ch.Diff = ChannelDiff.New;
                }
            }
        }

        // Move disappeared channels to log
        var newIds = newList.Select(c => c.Id).ToHashSet();
        foreach (var prev in oldList.Where(c => !newIds.Contains(c.Id) && c.Diff != ChannelDiff.Log))
        {
            prev.Diff = ChannelDiff.Log;
            prev.LoggedAt = DateTime.Now;
            GetLog(prev.YpName).Add(prev);
        }

        // Prune expired log entries
        foreach (var log in _logByYp.Values)
            log.RemoveAll(c => DateTime.Now - c.LoggedAt > LogExpiry);
    }

    public IReadOnlyList<ChannelItem> GetLogChannels(string ypName)
    {
        var log = GetLog(ypName);
        log.RemoveAll(c => DateTime.Now - c.LoggedAt > LogExpiry);
        return log.AsReadOnly();
    }

    public IReadOnlyList<ChannelItem> GetAllLogChannels()
    {
        var result = new List<ChannelItem>();
        foreach (var log in _logByYp.Values)
        {
            log.RemoveAll(c => DateTime.Now - c.LoggedAt > LogExpiry);
            result.AddRange(log);
        }
        return result;
    }

    private List<ChannelItem> GetLog(string ypName)
    {
        if (!_logByYp.TryGetValue(ypName, out var log))
        {
            log = [];
            _logByYp[ypName] = log;
        }
        return log;
    }

    private static bool HasChanged(ChannelItem prev, ChannelItem next) =>
        prev.Description != next.Description ||
        prev.Genre != next.Genre ||
        prev.ChannelType != next.ChannelType ||
        prev.TrackTitle != next.TrackTitle ||
        prev.TrackArtist != next.TrackArtist;
}
