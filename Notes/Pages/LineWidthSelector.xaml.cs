using System;
using System.Collections.Generic;
using System.ComponentModel;
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

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace DrawToNote.Pages
{
    public sealed partial class LineWidthSelector : UserControl
    {
        public event RangeBaseValueChangedEventHandler ValueChanged; 

        public LineWidthSelector()
        {
            this.InitializeComponent();
            LineWidthSlider.ValueChanged += LineWidthSlider_ValueChanged;
        }

        void LineWidthSlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            var eventHandler = this.ValueChanged;
            if (eventHandler != null)
            {
                eventHandler(this, e);
            }
        }
    }
}
