using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace DrawToNote.Pages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class Splash : Page
    {
        public Splash()
        {
            this.InitializeComponent();
        }

        /// <summary>
        /// Invoked when this page is about to be displayed in a Frame.
        /// </summary>
        /// <param name="e">Event data that describes how this page was reached.  The Parameter
        /// property is typically used to configure the page.</param>
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            SplashScreen splashScreen = e.Parameter as SplashScreen;
            Rect rect = splashScreen.ImageLocation;

            //StartupSplashImage.SetValue(Canvas.TopProperty, rect.Y);
            //StartupSplashImage.SetValue(Canvas.LeftProperty, rect.X);
            //StartupSplashImage.Height = rect.Height;
            //StartupSplashImage.Width = rect.Width;

            StartupProgress.Margin = new Thickness(0, rect.Height + 100, 0, 0);
            StartupProgress.Height = 60;
            StartupProgress.Width = 60;

            //StartupSplashImage.Visibility = Visibility.Visible;
            StartupProgress.Visibility = Visibility.Visible;
        }
    }
}