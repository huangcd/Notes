using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using DrawToNote.Common;
using DrawToNote.Datas;

// The Items Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234233

namespace DrawToNote.Pages
{
    /// <summary>
    /// A page that displays a collection of item previews.  In the Split Application this page
    /// is used to display and select one of the available groups.
    /// </summary>
    public sealed partial class NotesPage : LayoutAwarePage
    {
        ScriptManager scriptManager = ScriptManager.Instance;
        public NotesPage()
        {
            this.InitializeComponent();
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
        protected async override void LoadState(Object navigationParameter, Dictionary<String, Object> pageState)
        {
            this.DefaultViewModel["Scripts"] = await scriptManager.LoadScriptsAsync();
            // TODO: Assign a bindable collection of items to this.DefaultViewModel["Items"]
        }

        private void ItemView_ItemClick(object sender, ItemClickEventArgs e)
        {

        }

        private void Header_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
