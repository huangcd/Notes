﻿using System;
using DrawToNote.Common;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace DrawToNote.Pages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class ExtendSplashScreen
    {
        internal Rect splashImageRect; // Rect to store splash screen image coordinates.
        internal bool dismissed = false; // Variable to track splash screen dismissal status.
        internal Frame rootFrame;

        private SplashScreen splash; // Variable to hold the splash screen object.

        public ExtendSplashScreen(SplashScreen splashscreen, bool loadState)
        {
            InitializeComponent();

            LearnMoreButton.Click += new RoutedEventHandler(LearnMoreButton_Click);

            // Listen for window resize events to reposition the extended splash screen image accordingly.
            // This is important to ensure that the extended splash screen is formatted properly in response to snapping, unsnapping, rotation, etc...
            Window.Current.SizeChanged += new WindowSizeChangedEventHandler(ExtendedSplash_OnResize);

            splash = splashscreen;

            if (splash != null)
            {
                // Register an event handler to be executed when the splash screen has been dismissed.
                splash.Dismissed += new TypedEventHandler<SplashScreen, Object>(DismissedEventHandler);

                // Retrieve the window coordinates of the splash screen image.
                splashImageRect = splash.ImageLocation;
                PositionImage();
            }

            // Create a Frame to act as the navigation context
            rootFrame = new Frame();

            // Restore the saved session state if necessary
            RestoreStateAsync(loadState);
        }

        private async void RestoreStateAsync(bool loadState)
        {
            if (loadState)
                await SuspensionManager.RestoreAsync();

            // Normally you should start the time consuming task asynchronously here and
            // dismiss the extended splash screen in the completed handler of that task
            // This sample dismisses extended splash screen  in the handler for "Learn More" button for demonstration
        }

        // Position the extended splash screen image in the same location as the system splash screen image.
        private void PositionImage()
        {
            extendedSplashImage.SetValue(Canvas.LeftProperty, splashImageRect.X);
            extendedSplashImage.SetValue(Canvas.TopProperty, splashImageRect.Y);
            extendedSplashImage.Height = splashImageRect.Height;
            extendedSplashImage.Width = splashImageRect.Width;
        }

        private void ExtendedSplash_OnResize(Object sender, WindowSizeChangedEventArgs e)
        {
            // Safely update the extended splash screen image coordinates. This function will be fired in response to snapping, unsnapping, rotation, etc...
            if (splash != null)
            {
                // Update the coordinates of the splash screen image.
                splashImageRect = splash.ImageLocation;
                PositionImage();
            }
        }

        private void LearnMoreButton_Click(object sender, RoutedEventArgs e)
        {
            // Navigate to mainpage
            rootFrame.Navigate(typeof(NotesPage));

            // Set extended splash info on main page
            //((NotesPage)rootFrame.Content).SetExtendedSplashInfo(splashImageRect, dismissed);

            // Place the frame in the current Window
            Window.Current.Content = rootFrame;
        }

        // Include code to be executed when the system has transitioned from the splash screen to the extended splash screen (application's first view).
        private void DismissedEventHandler(SplashScreen sender, object e)
        {
            dismissed = true;

            // Navigate away from the app's extended splash screen after completing setup operations here...
            // This sample navigates away from the extended splash screen when the "Learn More" button is clicked.
        }
    }
}