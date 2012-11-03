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

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Notes
{
    public sealed partial class ScrollScriptView : UserControl
    {
        readonly DependencyProperty CharacterHeightProperty =
            DependencyProperty.Register("CharacterHeight", typeof(double), typeof(ScrollScriptView), new PropertyMetadata(40));
        readonly DependencyProperty CharacterWidthProperty =
             DependencyProperty.Register("CharacterWidth", typeof(double), typeof(ScrollScriptView), new PropertyMetadata(40));
        readonly DependencyProperty CanvasBackgroundProperty =
             DependencyProperty.Register("CanvasBackground", typeof(Brush), typeof(ScrollScriptView), new PropertyMetadata(null));

        public ScrollScriptView()
        {
            this.InitializeComponent();
        }

        public double CharacterHeight
        {
            get
            {
                return (double)GetValue(CharacterHeightProperty);
            }
            set
            {
                SetValue(CharacterHeightProperty, value);
            }
        }

        public new Size RenderSize
        {
            get
            {
                Size size = base.RenderSize;
                ScrollCanvas.Width = size.Width;
                return size;
            }
        }

        public Brush CanvasBackground
        {
            get
            {
                return (Brush)GetValue(CanvasBackgroundProperty);
            }
            set
            {
                SetValue(CanvasBackgroundProperty, value);
            }
        }

        public double CharacterWidth
        {
            get
            {
                return (double)GetValue(CharacterWidthProperty);
            }
            set
            {
                SetValue(CharacterWidthProperty, value);
            }
        }

        public Size CharacterSize
        {
            get
            {
                return new Size(CharacterWidth, CharacterHeight);
            }
        }

        public void AddChild(UIElement child)
        {
            ScrollCanvas.Children.Add(child);
        }
    }
}