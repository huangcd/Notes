using System;
using System.Diagnostics.Tracing;
using System.Threading.Tasks;
using DrawToNote.Common;
using DrawToNote.Datas;
using DrawToNote.Pages;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Storage;
using Windows.UI.ApplicationSettings;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Media.Animation;

// The Blank Application template is documented at http://go.microsoft.com/fwlink/?LinkId=234227

namespace DrawToNote
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    sealed partial class App : Application
    {
        private Frame _rootFrame;

        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            this.InitializeComponent();
            this.Suspending += OnSuspending;
        }

        private static void InitEventHandler()
        {
            // First time execution, initialize the logger 
            EventListener verboseListener = new StorageFileEventListener("Log");
            EventListener informationListener = new StorageFileEventListener("Warning");

            verboseListener.EnableEvents(MetroEventSource.Instance, EventLevel.Verbose);
            informationListener.EnableEvents(MetroEventSource.Instance, EventLevel.Informational);
        }

        /// <summary>
        /// Invoked when the application is launched normally by the end user.  Other entry points
        /// will be used when the application is launched to open a specific file, to display
        /// search results, and so forth.
        /// </summary>
        /// <param name="args">Details about the launch request and process.</param>
        protected override async void OnLaunched(LaunchActivatedEventArgs args)
        {
            //SettingsPane.GetForCurrentView().CommandsRequested += SettingPaneCommandsRequested;
            InitEventHandler();

            _rootFrame = Window.Current.Content as Frame;
            if (_rootFrame == null)
            {
                _rootFrame = new Frame();
                if (args.PreviousExecutionState != ApplicationExecutionState.Running)
                {
                    bool loadState = args.PreviousExecutionState == ApplicationExecutionState.Terminated;
                    _rootFrame.Navigate(typeof(Splash), args.SplashScreen);
                    Window.Current.Content = _rootFrame;
                    Window.Current.Activate();
                    await PrepareData();
                }
                else
                {
                    if (args.PreviousExecutionState == ApplicationExecutionState.Terminated)
                    {
                        //TODO: Load state from previously suspended application
                    }

                    // Place the frame in the current Window
                    Window.Current.Content = _rootFrame;
                    if (_rootFrame.Content == null)
                    {
                        // When the navigation stack isn't restored navigate to the first page,
                        // configuring the new page by passing required information as a navigation
                        // parameter
                        if (!_rootFrame.Navigate(typeof(NotesPage), args.Arguments))
                        {
                            throw new Exception("Failed to create initial page");
                        }
                    }
                    Window.Current.Activate();
                }
            }
            else
            {
                // Ensure the current window is active
                Window.Current.Activate();
            }
        }

        void SettingPaneCommandsRequested(SettingsPane sender, SettingsPaneCommandsRequestedEventArgs args)
        {
            SettingsCommand command = new SettingsCommand("Setting", StringProvider.GetValue("SettingTextBlock.Text"), handler =>
            {
                Popup popup = BuildSettingsItem(new SettingPage(), 646);
                popup.IsOpen = true;
            });
            args.Request.ApplicationCommands.Add(command);
        }

        private Popup BuildSettingsItem(UserControl userControl, int width)
        {
            Popup p = new Popup();
            p.IsLightDismissEnabled = true;
            p.ChildTransitions = new TransitionCollection();
            p.ChildTransitions.Add(new PaneThemeTransition
            {
                Edge = (SettingsPane.Edge == SettingsEdgeLocation.Right) ? EdgeTransitionLocation.Right : EdgeTransitionLocation.Left
            });
            userControl.Width = width;
            userControl.Height = Window.Current.Bounds.Height;
            p.Child = userControl;
            p.SetValue(Canvas.LeftProperty, SettingsPane.Edge == SettingsEdgeLocation.Right ? (Window.Current.Bounds.Width - width) : 0);
            p.SetValue(Canvas.TopProperty, 0);
            return p;
        }

        private void RemoveSplash()
        {
            if (_rootFrame != null)
            {
                _rootFrame.Navigate(typeof(NotesPage));
            }
        }

        private async Task PrepareData()
        {
            var setting = ApplicationData.Current.LocalSettings;
            Object obj = setting.Values["AppHasBeenStarted"];
            if (obj == null)
            {
                // First Time
                setting.Values["AppHasBeenStarted"] = "AppHasBeenStarted";
                // ScriptManager.Instance.Add(Script.LoadAsync(HowToUse.Data));
            }
            await ScriptManager.Instance.LoadScriptsAsync();
            RemoveSplash();
        }

        /// <summary>
        /// Invoked when application execution is being suspended.  Application state is saved
        /// without knowing whether the application will be terminated or resumed with the contents
        /// of memory still intact.
        /// </summary>
        /// <param name="sender">The source of the suspend request.</param>
        /// <param name="e">Details about the suspend request.</param>
        private void OnSuspending(object sender, SuspendingEventArgs e)
        {
            var deferral = e.SuspendingOperation.GetDeferral();

            //TODO: Save application state and stop any background activity
            if (ScriptManager.Instance.CurrentScript != null)
            {
                ScriptManager.Instance.CurrentScript.Save();
            }

            deferral.Complete();
        }

        protected override void OnActivated(IActivatedEventArgs args)
        {
            base.OnActivated(args);
        }

        /// <summary>
        /// Invoked when the application is activated to display search results.
        /// </summary>
        /// <param name="args">Details about the activation request.</param>
        protected async override void OnSearchActivated(SearchActivatedEventArgs args)
        {
            // TODO: Register the Windows.ApplicationModel.Search.SearchPane.GetForCurrentView().QuerySubmitted
            // event in OnWindowCreated to speed up searches once the application is already running

            // If the Window isn't already using Frame navigation, insert our own Frame
            var previousContent = Window.Current.Content;
            var frame = previousContent as Frame;

            // If the app does not contain a top-level frame, it is possible that this
            // is the initial launch of the app. Typically this method and OnLaunched
            // in App.xaml.cs can call a common method.
            if (frame == null)
            {
                // Create a Frame to act as the navigation context and associate it with
                // a SuspensionManager key
                frame = new Frame();
                SuspensionManager.RegisterFrame(frame, "AppFrame");

                if (args.PreviousExecutionState == ApplicationExecutionState.Terminated)
                {
                    // Restore the saved session state only when appropriate
                    try
                    {
                        await SuspensionManager.RestoreAsync();
                    }
                    catch (SuspensionManagerException)
                    {
                        //Something went wrong restoring state.
                        //Assume there is no state and continue
                    }
                }
            }

            frame.Navigate(typeof(ScriptSearchResultPage), args.QueryText);
            Window.Current.Content = frame;

            // Ensure the current window is active
            Window.Current.Activate();
        }
    }
}