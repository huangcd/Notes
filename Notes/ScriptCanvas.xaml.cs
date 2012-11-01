using System;
using System.Collections.Generic;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Storage.Streams;
using Windows.UI.Input.Inking;
using Windows.UI.Xaml.Controls;
using Notes.Common;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Shapes;
using Windows.Foundation;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Notes
{
    public sealed partial class ScriptCanvas : Canvas
    {
        readonly DependencyProperty CharacterHeightProperty =
            DependencyProperty.Register("CharacterHeight", typeof(double), typeof(ScriptCanvas), new PropertyMetadata(6));
        readonly DependencyProperty CharacterWidthProperty =
             DependencyProperty.Register("CharacterWidth", typeof(double), typeof(ScriptCanvas), new PropertyMetadata(6));

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

        public ScriptCanvas()
        {
            this.InitializeComponent();
        }
    }
}