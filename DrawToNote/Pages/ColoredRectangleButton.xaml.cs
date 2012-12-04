using System;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace DrawToNote.Pages
{
    public sealed partial class ColoredRectangleButton : UserControl
    {
        public event DependencyPropertyChangedEventHandler Check;

        public DependencyProperty ColorProperty = DependencyProperty.Register(
            "ButtonColor",
            typeof(SolidColorBrush),
            typeof(ColoredRectangleButton),
            new PropertyMetadata("Black", (obj, evt) =>
            {
                ColoredRectangleButton owner = (ColoredRectangleButton)obj;
                owner.InnerRect.Fill = (SolidColorBrush)evt.NewValue;
            }));

        public DependencyProperty BorderWidthProperty = DependencyProperty.Register(
            "BorderWidth",
            typeof(double),
            typeof(ColoredRectangleButton),
            new PropertyMetadata(5, (obj, evt) =>
            {
                ColoredRectangleButton owner = (ColoredRectangleButton)obj;
                owner.InnerRect.Margin = new Thickness((double)evt.NewValue);
            }));

        public DependencyProperty BorderColorProperty = DependencyProperty.Register(
            "BorderColor",
            typeof(Brush),
            typeof(ColoredRectangleButton),
            new PropertyMetadata("White", (obj, evt) =>
            {
                ColoredRectangleButton owner = (ColoredRectangleButton)obj;
                owner.OuterGrid.Background = (Brush)evt.NewValue;
            }));

        public DependencyProperty CheckedProperty = DependencyProperty.Register(
            "Checked",
            typeof(bool),
            typeof(ColoredRectangleButton),
            new PropertyMetadata(false, (obj, evt) =>
            {
                ColoredRectangleButton owner = (ColoredRectangleButton)obj;
                bool isChecked = (bool)evt.NewValue;
                if (isChecked)
                {
                    owner.BorderColor = new SolidColorBrush(Colors.Gold);
                    if (owner.Check != null)
                    {
                        owner.Check(owner, evt);
                    }
                }
                else
                {
                    owner.BorderColor = new SolidColorBrush(Colors.White);
                }
            }));

        public Boolean Checked
        {
            get
            {
                return (Boolean)GetValue(CheckedProperty);
            }
            set
            {
                SetValue(CheckedProperty, value);
            }
        }

        public Brush BorderColor
        {
            get
            {
                return (Brush)GetValue(BorderColorProperty);
            }
            set
            {
                SetValue(BorderColorProperty, value);
            }
        }

        public SolidColorBrush ButtonColor
        {
            get
            {
                return GetValue(ColorProperty) as SolidColorBrush;
            }
            set
            {
                SetValue(ColorProperty, value);
            }
        }

        public Double BorderWidth
        {
            get
            {
                return (double)GetValue(BorderWidthProperty);
            }
            set
            {
                SetValue(BorderWidthProperty, value);
            }
        }

        public ColoredRectangleButton()
        {
            this.InitializeComponent();
        }

        private void Grid_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            Checked = !Checked;
        }
    }
}