using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.Foundation;
using Windows.Storage;
using Windows.Storage.AccessCache;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace kioskimages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public partial class MainPage : Page
    {
        private StreamUriWinRTRResolver resolver;
        private DispatcherTimer commandBarTimer;
        private bool keepCommandBarOpen = true;
        private StorageFolder imageFolder;
        private DispatcherTimer trackerTimer;
        private IList<StorageFile> images = new List<StorageFile>();

        public MainPage()
        {
            commandBarTimer = new DispatcherTimer();
            commandBarTimer.Tick += OnTimer;
            commandBarTimer.Interval = new TimeSpan(0, 0, 10);
            trackerTimer = new DispatcherTimer();
            trackerTimer.Tick += OnTrackerTimer;
            trackerTimer.Interval = new TimeSpan(0, 0, 5);
            resolver = new StreamUriWinRTRResolver(LoadData);
            this.InitializeComponent();
        }

        private void WebViewControl_Loaded(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            Uri url = WebViewControl.BuildLocalStreamUri("index", "/index.html");
            WebViewControl.NavigateToLocalStreamUri(url, resolver);
        }

        protected async Task<IBuffer> LoadData(string path)
        {
            if (path.StartsWith("/images/"))
            {
                string name = path.Substring(8);
                Debug.WriteLine($"loading image: {name}");
                foreach (var image in images)
                {
                    if (image.Name == name)
                    {
                        return await FileIO.ReadBufferAsync(image);
                    }
                }
                throw new Exception($"image {name} does not exist");
            }
            else
            {
                StorageFile f = await StorageFile.GetFileFromApplicationUriAsync(new Uri($"ms-appx:///html/{path}"));
                return await FileIO.ReadBufferAsync(f);
            }
        }

        public sealed class StreamUriWinRTRResolver : Windows.Web.IUriToStreamResolver
        {
            private Func<string, Task<IBuffer>> loadData;
            public StreamUriWinRTRResolver(Func<string, Task<IBuffer>> loader)
            {
                this.loadData = loader;
            }

            public IAsyncOperation<IInputStream> UriToStreamAsync(Uri uri)
            {
                return GetContentAsync(uri).AsAsyncOperation();
            }

            private async Task<IInputStream> GetContentAsync(Uri uri)
            {
                Debug.WriteLine($"GetContentAsync {uri.AbsolutePath}");
                var buffer = await this.loadData(uri.AbsolutePath);
                var stream = new InMemoryRandomAccessStream();
                await stream.WriteAsync(buffer);
                return stream.GetInputStreamAt(0);
            }
        }

        private void OnTimer(object sender, object e)
        {
            Debug.WriteLine("command bar timeout");
            keepCommandBarOpen = false;
            CommandBar.IsOpen = false;
        }

        private void Page_Tapped(object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e)
        {
            Debug.WriteLine("page tapped");
            if (CommandBar.IsOpen)
            {
                keepCommandBarOpen = false;
                CommandBar.IsOpen = false;
                commandBarTimer.Stop();
            }
            else
            {
                keepCommandBarOpen = true;
                CommandBar.IsOpen = true;
                commandBarTimer.Stop();
            }
        }

        private void CommandBar_Closed(object sender, object e)
        {
            commandBarTimer.Stop();
        }

        private void CommandBar_Opened(object sender, object e)
        {
            commandBarTimer.Start();
        }

        private void resetTimer()
        {
            if (CommandBar.IsOpen)
            {
                commandBarTimer.Stop();
                commandBarTimer.Start();
            }
        }

        private void CommandBar_PointerMoved(object sender, Windows.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            resetTimer();
        }

        private void CommandBar_KeyDown(object sender, Windows.UI.Xaml.Input.KeyRoutedEventArgs e)
        {
            resetTimer();
        }

        private void EnableChangeTracker()
        {
            StorageLibraryChangeTracker tracker = imageFolder.TryGetChangeTracker();
            if (tracker != null)
            {
                tracker.Enable();
                trackerTimer.Start();
            }
            else
            {
                Debug.WriteLine("Couldn't get change tracker");
            }
        }

        private async void OnTrackerTimer(object sender, object e)
        {
            StorageLibraryChangeTracker tracker = imageFolder.TryGetChangeTracker();
            if (tracker != null)
            {
                tracker.Enable();
                StorageLibraryChangeReader reader = tracker.GetChangeReader();
                var changeset = await reader.ReadBatchAsync();

                foreach (var change in changeset)
                {
                    if (change.ChangeType == StorageLibraryChangeType.ChangeTrackingLost)
                    {
                        Debug.WriteLine("changes lost, resetting tracker");
                        tracker.Reset();
                    }
                    else
                    {
                        bool found = false;
                        foreach (var image in images)
                        {
                            if (image.Path == change.Path)
                            {
                                found = true;
                            }
                        }
                        if (found)
                        {
                            if (change.ChangeType == StorageLibraryChangeType.ContentsChanged)
                            {
                                Debug.WriteLine($"{change.Path} changed");
                            }
                            else if (change.ChangeType == StorageLibraryChangeType.MovedOutOfLibrary ||
                                     change.ChangeType == StorageLibraryChangeType.Deleted)
                            {
                                Debug.WriteLine($"{change.Path} moved or deleted");
                            }
                        }
                        else if (change.ChangeType == StorageLibraryChangeType.Created)
                        {
                            Debug.WriteLine($"{change.Path} created");
                        }
                    }
                }
                await reader.AcceptChangesAsync();
            }
        }

        private async void InitializeImageDisplay()
        {
            var inames = "";
            foreach (var image in images)
            {
                if (inames.Length > 0)
                {
                    inames += ";";
                }
                inames += image.Name;
            }
            Debug.WriteLine($"images: {inames}");
            try
            {
                var args = new List<string> { inames };
                await WebViewControl.InvokeScriptAsync("initializeSlides", args);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"exception executing script: {ex.Message}");
            }
        }

        private async void openImageBtn_Click(object sender, RoutedEventArgs e)
        {
            FileOpenPicker picker = new FileOpenPicker();
            picker.ViewMode = PickerViewMode.Thumbnail;
            picker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;
            picker.FileTypeFilter.Add(".jpg");
            picker.FileTypeFilter.Add(".jpeg");
            picker.FileTypeFilter.Add(".png");
            picker.FileTypeFilter.Add(".gif");
            picker.FileTypeFilter.Add(".tif");
            picker.FileTypeFilter.Add(".tiff");
            picker.FileTypeFilter.Add(".svg");
            picker.FileTypeFilter.Add(".bmp");
            StorageFile file = await picker.PickSingleFileAsync();
            if (file != null)
            {
                Debug.WriteLine($"image chosen: {file.Path}");
                imageFolder = await file.GetParentAsync();
                images.Clear();
                images.Add(file);
                EnableChangeTracker();
                InitializeImageDisplay();
            }
            else
            {
            }
        }

        private async void openFolderBtn_Click(object sender, RoutedEventArgs e)
        {
            FolderPicker picker = new FolderPicker();
            picker.SuggestedStartLocation = PickerLocationId.HomeGroup;
            StorageFolder folder = await picker.PickSingleFolderAsync();
            if (folder != null)
            {
                // The StorageFolder has read/write access to all contents in the picked folder (including other sub-folder contents).
                // See the FileAccess sample for code that obtains a StorageFile from a StorageFolder to read and write.
                StorageApplicationPermissions.FutureAccessList.AddOrReplace("PickedFolderToken", folder);
                Debug.WriteLine($"folder chosen: {folder.Path}");
                imageFolder = folder;
                images.Clear();
                var fileList = await folder.GetFilesAsync();
                foreach (var file in fileList)
                {
                    images.Add(file);
                }
                EnableChangeTracker();
                InitializeImageDisplay();
            }
            else
            {
            }
        }

        private async void invokeScript(string function)
        {
            try
            {
                await WebViewControl.InvokeScriptAsync(function, null);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"exception executing script: {ex.Message}");
            }
        }

        private void prevBtn_Click(object sender, RoutedEventArgs e)
        {
            invokeScript("prevSlide");
        }

        private void nextBtn_Click(object sender, RoutedEventArgs e)
        {
            invokeScript("nextSlide");
        }

        private void playBtn_Click(object sender, RoutedEventArgs e)
        {
            invokeScript("startSlideShow");
        }

        private void stopBtn_Click(object sender, RoutedEventArgs e)
        {
            invokeScript("stopSlideShow");
        }

        private void toggleFullScreen()
        {
            var view = ApplicationView.GetForCurrentView();
            if (view.IsFullScreenMode)
            {
                view.ExitFullScreenMode();
                closeBtn.Visibility = Visibility.Collapsed;
                fullScreenBtn.Icon = new SymbolIcon(Symbol.FullScreen);
                fullScreenBtn.Label = "Full Screen";
            }
            else
            {
                if (view.TryEnterFullScreenMode())
                {
                    fullScreenBtn.Icon = new SymbolIcon(Symbol.BackToWindow);
                    fullScreenBtn.Label = "Restore";
                    closeBtn.Visibility= Visibility.Visible;
                }
            }
        }

        private void fullScreenBtn_Click(object sender, RoutedEventArgs e)
        {
            toggleFullScreen();
        }

        private void CommandBar_Closing(object sender, object e)
        {
            if (keepCommandBarOpen)
            {
                ((CommandBar)sender).IsOpen = true;
            }
        }

        private void closeBtn_Click(object sender, RoutedEventArgs e)
        {
            CoreApplication.Exit();
        }

        private void KeyboardAccelerator_Invoked(Windows.UI.Xaml.Input.KeyboardAccelerator sender, Windows.UI.Xaml.Input.KeyboardAcceleratorInvokedEventArgs args)
        {
            toggleFullScreen();
        }
    }
}
