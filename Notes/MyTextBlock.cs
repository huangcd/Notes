using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// The Templated Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234235

namespace Notes
{
    public sealed class MyTextBlock : Control
    {
        public static readonly DependencyProperty BlinkProperty =
            DependencyProperty.Register(
            "Blink",
            typeof(bool),
            typeof(MyTextBlock),
            new PropertyMetadata(false, new PropertyChangedCallback(OnBlinkChanged)));

        public event EventHandler Blinked;

        private void OnBlinked()
        {
            EventHandler eh = Blinked;
            if (eh != null)
            {
                eh(this, new EventArgs());
            }
        }

        private DispatcherTimer timer = null;

        public MyTextBlock()
        {
            this.DefaultStyleKey = typeof(MyTextBlock);
        }

        public bool Blink
        {
            get { return (bool)GetValue(BlinkProperty); }
            set { SetValue(BlinkProperty, value); }
        }

        private DispatcherTimer Timer
        {
            get
            {
                if (timer == null)
                {
                    timer = new DispatcherTimer();
                    timer.Interval = new TimeSpan(0, 0, 0, 0, 500);
                    timer.Tick += __timer_Tick;
                }
                return timer;
            }
        }

        private static void OnBlinkChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var instance = d as MyTextBlock;
            if (instance != null)
            {
                if (instance.Timer.IsEnabled != instance.Blink)
                {
                    instance.Timer.Start();
                }
                else
                {
                    instance.Timer.Stop();
                }
            }
        }
        private void __timer_Tick(object sender, object e)
        {
            DoBlink();
        }

        private void DoBlink()
        {
            this.Opacity = (this.Opacity + 1) % 2;
            OnBlinked();
        }
    }
}