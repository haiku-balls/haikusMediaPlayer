using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using DiscordRPC.Logging;
using DiscordRPC;
using Microsoft.UI.Xaml.Media.Imaging;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Storage.FileProperties;
using Windows.Storage.Streams;
using Windows.Storage;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace haikusMediaPlayer
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class homePage : Page
    {

        MediaPlaybackList _mediaPlaybackList;

        public homePage()
        {
            this.InitializeComponent();

            tutTeaching.IsOpen = true;

            // Discord Rich Presence
            initDiscordRPC();
        }

        public DiscordRpcClient client;

        private void initDiscordRPC()
        {
            client = new DiscordRpcClient("1218738545192210553");

            client.Logger = new ConsoleLogger() { Level = LogLevel.Warning };

            client.OnReady += (sender, e) =>
            {
                Console.WriteLine("READY {0}", e.User.Username);
            };

            client.OnPresenceUpdate += (sender, e) =>
            {
                Console.WriteLine("Updated. {0}", e.Presence);
            };

            client.Initialize();

            client.SetPresence(new RichPresence()
            {
                Details = "Playing nothing."
            });
        }

        private async void OpenFile_Click(object sender, RoutedEventArgs e)
        {
            // When the button is clicked, open a file picker and handle the other stuff.
            await SetLocalMedia();
        }

        private async void OpenFolder_Click(object sender, RoutedEventArgs e)
        {
            await SetLocalFolder();
        }

        private void settingsButton_Click(object sender, RoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(settingsPage));
            mainPlayer.MediaPlayer.Pause(); // For some reason "DISPOSE" causes an exception?
        }

        // Folder handling
        async private Task SetLocalFolder()
        {
            loadWheel.IsIndeterminate = true;
            var folderPicker = new Windows.Storage.Pickers.FileOpenPicker();

            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.Window);

            WinRT.Interop.InitializeWithWindow.Initialize(folderPicker, hwnd);

            folderPicker.FileTypeFilter.Add(".mp3");
            folderPicker.FileTypeFilter.Add(".flac");

            var files = await folderPicker.PickMultipleFilesAsync();

            if (files.Count > 0)
            {

                _mediaPlaybackList = new MediaPlaybackList();

                foreach (var file in files)
                {
                    var mediaPlaybackItem = new MediaPlaybackItem(MediaSource.CreateFromStorageFile(file));
                    mediaPlaybackItem.Source.CustomProperties["FilePath"] = file.Path;
                    Debug.WriteLine(file.Path.ToString());
                    _mediaPlaybackList.Items.Add(mediaPlaybackItem);
                }
                /*
                _mediaPlaybackList.CurrentItemChanged += MediaPlaybackList_CurrentItemChanged;
                _mediaPlaybackList.ItemOpened += MediaPlaybackList_ItemOpened;
                _mediaPlaybackList.ItemFailed += MediaPlaybackList_ItemFailed;
                */

                // _mediaPlaybackList.ItemOpened += MediaPlaybackList_ItemOpened;
                _mediaPlaybackList.CurrentItemChanged += MediaPlaybackList_CurrentItemChanged;

                _mediaPlaybackList.MaxPlayedItemsToKeepOpen = 3;
                mainPlayer.Source = _mediaPlaybackList;
            }
            else
            {
                loadWheel.IsIndeterminate = false;
            }
        }

        async private void MediaPlaybackList_CurrentItemChanged(object sender, CurrentMediaPlaybackItemChangedEventArgs e)
        {

            var mediaPlaybackItem = e.NewItem;

            if (mediaPlaybackItem != null)
            {
                if (mediaPlaybackItem.Source.CustomProperties.TryGetValue("FilePath", out var filePathObj) && filePathObj is string filePath)
                {
                    var file = await StorageFile.GetFileFromPathAsync(filePath);
                    if (file != null)
                    {

                        // Gets the metadata about the CURRENT file.
                        // Debug.WriteLine("Grabbed: " + filePath.ToString());
                        MusicProperties fileAttrib = await file.Properties.GetMusicPropertiesAsync();

                        var album = fileAttrib.Album;
                        var trackNumber = fileAttrib.TrackNumber.ToString();
                        var title = fileAttrib.Title;

                        // From stack overflow this function allows me to access the MAIN thread (UI).
                        DispatcherQueue.TryEnqueue(() =>
                        {
                            attribTitle.Text = "#" + trackNumber + " - " + title;
                            attribAlbum.Text = album;

                            // Bitrate (advanced)
                            var convertedBit = Math.Floor(fileAttrib.Bitrate * 0.001);
                            attribBitrate.Text = convertedBit + " kbps";
                            mainPlayer.MediaPlayer.Play();
                        });

                        // Get the album cover (inherited from single file)

                        TagLib.File albumCoverFile = TagLib.File.Create(filePath);
                        TagLib.IPicture picture = albumCoverFile.Tag.Pictures.FirstOrDefault();

                        if (picture != null)
                        {
                            DispatcherQueue.TryEnqueue(async () =>
                            {
                                // To be honest, I don't know what this does.
                                byte[] coverData = picture.Data.Data;

                                // Create a SoftwareBitmap from the cover data
                                using (InMemoryRandomAccessStream stream = new InMemoryRandomAccessStream())
                                {
                                    await stream.WriteAsync(coverData.AsBuffer());
                                    stream.Seek(0);
                                    BitmapImage albumCover = new BitmapImage();
                                    await albumCover.SetSourceAsync(stream);
                                    attribAlbumCover.Source = albumCover;
                                }
                                loadWheel.IsIndeterminate = false;
                            });
                        }
                        else if (picture == null) // In the case of no cover :)
                        {
                            DispatcherQueue.TryEnqueue(() =>
                            {
                                attribAlbumCover.Source = new BitmapImage(new Uri("ms-appx:///Media/defaultCover.png"));
                                loadWheel.IsIndeterminate = false;
                            });
                        }
                    }

                }
            }
            DispatcherQueue.TryEnqueue(() =>
            {
                mainPlayer.MediaPlayer.Play();
            });
        }

        async private Task SetLocalMedia()
        {
            loadWheel.IsIndeterminate = true;
            // Create a new FileOpenPicker
            var openPicker = new Windows.Storage.Pickers.FileOpenPicker();

            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.Window);

            WinRT.Interop.InitializeWithWindow.Initialize(openPicker, hwnd);

            // Add file types to the picker.
            openPicker.FileTypeFilter.Add(".mp3");
            openPicker.FileTypeFilter.Add(".flac");

            // Show the picker (asynchronously)
            var file = await openPicker.PickSingleFileAsync();

            // Checks whether the file is null (otherwise this will throw an exception :p)
            if (file != null)
            {
                // The "TagLib" library is used to get the cover art of the song.
                // I went and used the file's path to get the cover art. (weird)
                TagLib.File albumCoverFile = TagLib.File.Create(file.Path);
                TagLib.IPicture picture = albumCoverFile.Tag.Pictures.FirstOrDefault();
                // Debug.WriteLine("Grabbed (Single): " + file.Path.ToString());

                BitmapImage albumCover = new BitmapImage();

                if (picture != null)
                {
                    // To be honest, I don't know what this does.
                    byte[] coverData = picture.Data.Data;

                    // Create a SoftwareBitmap from the cover data
                    using (InMemoryRandomAccessStream stream = new InMemoryRandomAccessStream())
                    {
                        await stream.WriteAsync(coverData.AsBuffer());
                        stream.Seek(0);

                        await albumCover.SetSourceAsync(stream);

                        attribAlbumCover.Source = albumCover;
                    }
                }
                else if (picture == null) // In the case of no cover :)
                {
                    attribAlbumCover.Source = new BitmapImage(new Uri("ms-appx:///Media/defaultCover.png"));
                }

                // Update metadata.
                MusicProperties fileAttrib = await file.Properties.GetMusicPropertiesAsync();
                mainPlayer.Source = MediaSource.CreateFromStorageFile(file);
                attribTitle.Text = "#" + fileAttrib.TrackNumber + " - " + fileAttrib.Title;
                attribAlbum.Text = fileAttrib.Album;

                // Bitrate (advanced)
                var convertedBit = Math.Floor(fileAttrib.Bitrate * 0.001);
                attribBitrate.Text = convertedBit + " kbps";
                mainPlayer.MediaPlayer.Play();

                // [TODO] The task is complete here.
                loadWheel.IsIndeterminate = false;
            }
            else if (file == null)
            {
                loadWheel.IsIndeterminate = false;
            }
        }
    }
}
