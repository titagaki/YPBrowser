using YPBrowser.Settings;

namespace YPBrowser.Abstractions;

public interface ISettingsService
{
    AppSettings Current { get; }
    Task LoadAsync();
    Task SaveAsync();
}
