using System;
using System.Collections.Generic;
using DrawToNote.Common;
using DrawToNote.Datas;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// The Items Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234233

namespace DrawToNote.Pages
{
    /// <summary>
    /// A page that displays a collection of item previews.  In the Split Application this page
    /// is used to display and select one of the available groups.
    /// </summary>
    public sealed partial class NotesPage : LayoutAwarePage
    {
        private ScriptManager scriptManager = ScriptManager.Instance;

        public NotesPage()
        {
            this.InitializeComponent();

            //this.Background = new ImageBrush
            //{
            //    ImageSource = new BitmapImage(new Uri("ms-appx:///Assets/background.png")),
            //    Stretch = Stretch.UniformToFill
            //};
        }

        /// <summary>
        /// Populates the page with content passed during navigation.  Any saved state is also
        /// provided when recreating a page from a prior session.
        /// </summary>
        /// <param name="navigationParameter">The parameter value passed to
        /// <see cref="Frame.Navigate(Type, Object)"/> when this page was initially requested.
        /// </param>
        /// <param name="pageState">A dictionary of state preserved by this page during an earlier
        /// session.  This will be null the first time a page is visited.</param>
        protected override void LoadState(Object navigationParameter, Dictionary<String, Object> pageState)
        {
            // TODO: If there isn't a file in the localState, add a script to demostrate how to use directly.
            this.DefaultViewModel["ScriptGroup"] = new[]
                {
                    //new { Title = StringProvider.GetValue("RecentScriptString"), Scripts = scriptManager.RecentScripts },
                    new { Title = StringProvider.GetValue("AllScriptString"), Scripts = scriptManager.Scripts }
                };

            // If there is no script, create a new Script and redirect to ScriptPage
            if (scriptManager.Scripts.Count == 0)
            {
                this.Frame.Navigate(typeof(ScriptPage));
            }
        }

        private void ItemView_ItemClick(object sender, ItemClickEventArgs e)
        {
            Script script = e.ClickedItem as Script;
            this.Frame.Navigate(typeof(ScriptPage), script);
        }

        private void Header_Click(object sender, RoutedEventArgs e)
        {
        }

        private HashSet<Script> selectScripts = new HashSet<Script>();

        private void itemGridView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            foreach (var item in e.RemovedItems)
            {
                selectScripts.Remove(item as Script);
            }
            foreach (var item in e.AddedItems)
            {
                selectScripts.Add(item as Script);
            }
        }

        private void AppBarNewScriptButton_Click(object sender, RoutedEventArgs e)
        {
            Script newScript = scriptManager.CreateScript();
            Frame.Navigate(typeof(ScriptPage), newScript);
        }

        private void AppBarDeleteButton_Click(object sender, RoutedEventArgs e)
        {
            List<Script> list = new List<Script>(selectScripts);
            foreach (var item in list)
            {
                scriptManager.Remove(item);
            }
        }
    }
}