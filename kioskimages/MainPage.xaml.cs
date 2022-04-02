using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Security.Cryptography;
using Windows.Storage;
using Windows.Storage.Streams;
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

        public MainPage()
        {
            resolver = new StreamUriWinRTRResolver();
            this.InitializeComponent();
        }

        private void WebViewControl_Loaded(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            Uri url = WebViewControl.BuildLocalStreamUri("index", "/index.html");
            WebViewControl.NavigateToLocalStreamUri(url, resolver);
        }

        protected static async Task<IBuffer> LoadData(string path)
        {
            StorageFile f = await StorageFile.GetFileFromApplicationUriAsync(new Uri($"ms-appx:///html/{path}"));
            return await FileIO.ReadBufferAsync(f);
        }

        public sealed class StreamUriWinRTRResolver : Windows.Web.IUriToStreamResolver
        {
            public IAsyncOperation<IInputStream> UriToStreamAsync(Uri uri)
            {
                return GetContentAsync(uri).AsAsyncOperation();
            }

            private async Task<IInputStream> GetContentAsync(Uri uri)
            {
                Debug.WriteLine($"GetContentAsync {uri.AbsolutePath}");
                var buffer = await LoadData(uri.AbsolutePath);
                var stream = new InMemoryRandomAccessStream();
                await stream.WriteAsync(buffer);
                return stream.GetInputStreamAt(0);
            }
        }
    }
}
