using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Navigation;
using YPBrowser.Models;

namespace YPBrowser.Views.Controls;

public partial class ChannelDetailPanel : UserControl
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
            ContactLink.Inlines.Clear();
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

        ContactLink.Inlines.Clear();
        if (!string.IsNullOrEmpty(ch.ContactUrl) && Uri.TryCreate(ch.ContactUrl, UriKind.Absolute, out var uri))
        {
            ContactLink.Inlines.Add(new Run(ch.ContactUrl));
            ContactLink.NavigateUri = uri;
        }
        else if (!string.IsNullOrEmpty(ch.ContactUrl))
        {
            ContactLink.Inlines.Add(new Run(ch.ContactUrl));
        }
    }

    private void ContactLink_RequestNavigate(object sender, RequestNavigateEventArgs e)
    {
        try { Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true }); }
        catch { }
        e.Handled = true;
    }
}
