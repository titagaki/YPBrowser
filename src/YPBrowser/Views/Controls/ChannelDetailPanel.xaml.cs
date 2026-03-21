using Microsoft.UI.Xaml.Controls;
using YPBrowser.Models;

namespace YPBrowser.Views.Controls;

public sealed partial class ChannelDetailPanel : UserControl
{
    public ChannelDetailPanel()
    {
        InitializeComponent();
    }

    public void SetChannel(ChannelItem? ch)
    {
        if (ch == null)
        {
            ChannelNameText.Text = "";
            GenreText.Text = "";
            DescText.Text = "";
            CommentText.Text = "";
            ListenersText.Text = "";
            BitrateText.Text = "";
            TrackText.Text = "";
            YpText.Text = "";
            ContactLink.Content = "";
            ContactLink.NavigateUri = null;
            return;
        }

        ChannelNameText.Text = ch.ChannelName;
        GenreText.Text = ch.Genre;
        DescText.Text = ch.Description;
        CommentText.Text = ch.Comment;
        ListenersText.Text = $"{ch.ListenersDisplay}人 / リレー{ch.RelaysDisplay}";
        BitrateText.Text = $"{ch.BitrateDisplay} {ch.ChannelType}";
        TrackText.Text = ch.TrackInfo;
        YpText.Text = ch.YpName;

        if (!string.IsNullOrEmpty(ch.ContactUrl))
        {
            ContactLink.Content = ch.ContactUrl;
            if (Uri.TryCreate(ch.ContactUrl, UriKind.Absolute, out var uri))
                ContactLink.NavigateUri = uri;
        }
        else
        {
            ContactLink.Content = "";
            ContactLink.NavigateUri = null;
        }
    }
}
