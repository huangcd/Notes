using DrawToNote.Datas;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace DrawToNote.Pages
{
    public sealed partial class LineWidthSelector : UserControl
    {
        public event RangeBaseValueChangedEventHandler ValueChanged;

        public LineWidthSelector()
        {
            this.InitializeComponent();
            LineWidthSlider.Value = DefaultValue.DefaultLineWidth;
            LineWidthSlider.ValueChanged += LineWidthSlider_ValueChanged;
        }

        private void LineWidthSlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            var eventHandler = this.ValueChanged;
            if (eventHandler != null)
            {
                eventHandler(this, e);
            }
        }
    }
}