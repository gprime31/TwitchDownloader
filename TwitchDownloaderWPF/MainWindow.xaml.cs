﻿using AutoUpdaterDotNET;
using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Windows;
using TwitchDownloaderWPF.Properties;
using Xabe.FFmpeg;
using Xabe.FFmpeg.Downloader;

namespace TwitchDownloaderWPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public static PageVodDownload pageVodDownload = new PageVodDownload();
        public static PageClipDownload pageClipDownload = new PageClipDownload();
        public static PageChatDownload pageChatDownload = new PageChatDownload();
        public static PageChatUpdate pageChatUpdate = new PageChatUpdate();
        public static PageChatRender pageChatRender = new PageChatRender();
        public static PageQueue pageQueue = new PageQueue();

        public MainWindow()
        {
            InitializeComponent();
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
        }

        private void btnVodDownload_Click(object sender, RoutedEventArgs e)
        {
            Main.Content = pageVodDownload;
        }

        private void btnClipDownload_Click(object sender, RoutedEventArgs e)
        {
            Main.Content = pageClipDownload;
        }

        private void btnChatDownload_Click(object sender, RoutedEventArgs e)
        {
            Main.Content = pageChatDownload;
        }

        private void btnChatUpdate_Click(object sender, RoutedEventArgs e)
        {
            Main.Content = pageChatUpdate;
        }

        private void btnChatRender_Click(object sender, RoutedEventArgs e)
        {
            Main.Content = pageChatRender;
        }

        private void btnQueue_Click(object sender, RoutedEventArgs e)
        {
            Main.Content = pageQueue;
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            App.RequestAppThemeChange();

            Main.Content = pageVodDownload;
            if (Settings.Default.UpgradeRequired)
            {
                Settings.Default.Upgrade();
                Settings.Default.UpgradeRequired = false;
                Settings.Default.Save();
            }

            var currentVersion = Version.Parse("1.53.4");
            Title = $"Twitch Downloader v{currentVersion}";

            // TODO: extract FFmpeg handling to a dedicated service
            if (!File.Exists("ffmpeg.exe"))
            {
                var oldTitle = Title;
                try
                {
                    await FFmpegDownloader.GetLatestVersion(FFmpegVersion.Full, new FfmpegDownloadProgress());
                }
                catch (Exception ex)
                {
                    if (MessageBox.Show(string.Format(Translations.Strings.UnableToDownloadFfmpegFull, "https://ffmpeg.org/download.html", Path.Combine(Environment.CurrentDirectory, "ffmpeg.exe")),
                            Translations.Strings.UnableToDownloadFfmpeg, MessageBoxButton.OKCancel, MessageBoxImage.Information) == MessageBoxResult.OK)
                    {
                        Process.Start(new ProcessStartInfo("https://ffmpeg.org/download.html") { UseShellExecute = true });
                    }

                    if (Settings.Default.VerboseErrors)
                    {
                        MessageBox.Show(ex.ToString(), Translations.Strings.VerboseErrorOutput, MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }

                Title = oldTitle;
            }

            AutoUpdater.InstalledVersion = currentVersion;
#if !DEBUG
            if (AppContext.BaseDirectory.StartsWith(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)))
            {
                // If the app is in user profile, the updater probably doesn't need administrator permissions
                AutoUpdater.RunUpdateAsAdmin = false;
            }
            AutoUpdater.Start("https://downloader-update.twitcharchives.workers.dev");
#endif
        }

        private class FfmpegDownloadProgress : IProgress<ProgressInfo>
        {
            private int _lastPercent = -1;

            public void Report(ProgressInfo value)
            {
                var percent = (int)(value.DownloadedBytes / (double)value.TotalBytes * 100);

                if (percent > _lastPercent)
                {
                    var window = Application.Current.MainWindow;
                    if (window is null) return;

                    _lastPercent = percent;

                    var oldTitle = window.Title;
                    if (oldTitle.IndexOf('-') == -1) oldTitle += " -";

                    window.Title = string.Concat(
                        oldTitle.AsSpan(0, oldTitle.IndexOf('-')),
                        "- ",
                        string.Format(Translations.Strings.StatusDownloaderFFmpeg, percent.ToString())
                    );
                }
            }
        }
    }
}
