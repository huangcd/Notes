using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using DrawToNote.Datas;
using Windows.Foundation;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace DrawToNote.Pages
{
    public sealed partial class ScriptView : UserControl
    {
        private const double ScaleConstance = 1.2;

        #region Properties

        private readonly DependencyProperty CharacterHeightProperty =
            DependencyProperty.Register("CharacterHeight", typeof(double), typeof(ScriptView), new PropertyMetadata(20));

        private readonly DependencyProperty CharacterWidthProperty =
             DependencyProperty.Register("CharacterWidth", typeof(double), typeof(ScriptView), new PropertyMetadata(20));

        private readonly DependencyProperty CanvasBackgroundProperty =
             DependencyProperty.Register("CanvasBackground", typeof(Brush), typeof(ScriptView), new PropertyMetadata(null));

        private readonly DependencyProperty CharactersProperty =
            DependencyProperty.Register("Characters",
            typeof(ObservableCollection<Char>),
            typeof(ScriptView),
            new PropertyMetadata(new ObservableCollection<Char>(), OnCharactersChanged));

        private readonly DependencyProperty SnapshotProperty =
            DependencyProperty.Register("Snapshot",
            typeof(bool),
            typeof(ScriptView),
            new PropertyMetadata(false));

        private static void OnCharactersChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ScriptView container = d as ScriptView;
            (e.OldValue as ObservableCollection<Char>).CollectionChanged -= container.BindingScript_ScriptChanged;
            (e.NewValue as ObservableCollection<Char>).CollectionChanged += container.BindingScript_ScriptChanged;
            if (container.Snapshot)
            {
                container.Repaint();
            }
        }

        private readonly DependencyProperty ThicknessProperty =
            DependencyProperty.Register("LineWidth", typeof(double), typeof(ScriptView), new PropertyMetadata(DefaultValue.DefaultLineWidth));

        public bool Snapshot
        {
            get
            {
                return (bool)GetValue(SnapshotProperty);
            }
            set
            {
                SetValue(SnapshotProperty, value);
            }
        }

        public double LineWidth
        {
            get
            {
                return (double)GetValue(ThicknessProperty);
            }
            set
            {
                if (value == (double)GetValue(ThicknessProperty))
                {
                    return;
                }
                SetValue(ThicknessProperty, value);
                //foreach (var path in _cached.Values)
                //{
                //    path.StrokeThickness = value;
                //}
            }
        }

        public ObservableCollection<Char> Characters
        {
            get
            {
                return (ObservableCollection<Char>)GetValue(CharactersProperty);
            }
            set
            {
                SetValue(CharactersProperty, value);
            }
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
                Size size = Snapshot ? new Size(Width, Height) : base.RenderSize;
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

        #endregion Properties

        public ScriptView()
        {
            this.InitializeComponent();
            Characters.CollectionChanged += BindingScript_ScriptChanged;
            SizeChanged += ScrollScriptView_SizeChanged;
        }

        private void ScrollScriptView_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            double x1 = 0;
            double x2 = ActualWidth;
            double y = CharacterHeight;
            while (y < ActualHeight)
            {
                Line line = new Line
                {
                    X1 = x1,
                    X2 = x2,
                    Y1 = y,
                    Y2 = y,
                    StrokeThickness = 1,
                    Stroke = new SolidColorBrush(Colors.LightGray)
                };
                ScrollCanvas.Children.Add(line);
                y += CharacterHeight;
            }
        }

        private void CalcScaleInformation(
            Char chr,
            Size charSize,
            int index,
            out double shiftX,
            out double shiftY,
            out double scaleX,
            out double scaleY)
        {
            int columnCount = Snapshot ? (int)(RenderSize.Width / charSize.Width) : (int)((RenderSize.Width - 20) / charSize.Width);
            int row = index / columnCount;
            int column = index % columnCount;
            shiftX = column * charSize.Width;
            shiftY = row * charSize.Height;
            scaleX = charSize.Width * ScaleConstance / chr.CanvasSize.Width;
            scaleY = charSize.Height * ScaleConstance / chr.CanvasSize.Height;
            if (!Snapshot && (double.IsNaN(Height) || shiftY + charSize.Height > Height))
            {
                Height = System.Math.Max(shiftY + charSize.Height + 10, RenderSize.Height);
            }
        }

        private Dictionary<Stroke, Path> _cached = new Dictionary<Stroke, Path>();

        private Path GetPath(Stroke stroke)
        {
            if (!_cached.ContainsKey(stroke))
            {
                Path path = stroke.CreatePath();
                _cached[stroke] = path;
            }
            return _cached[stroke];
        }

        private Path ConvertStrokeToPath(
            Stroke stroke,
            double shiftX,
            double shiftY,
            double scaleX,
            double scaleY,
            double opacity = 1)
        {
            Path path = GetPath(stroke);
            path.StrokeThickness = stroke.LineWidth;
            path.Stroke = stroke.Brush;
            path.Opacity = opacity;
            path.RenderTransform = new ScaleTransform
            {
                ScaleX = scaleX,
                ScaleY = scaleY
            };
            path.Margin = new Thickness(shiftX, shiftY, 0, 0);
            return path;
        }

        private void BindingScript_ScriptChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (RenderSize.Width == 0 && RenderSize.Height == 0)
            {
                Characters.CollectionChanged -= BindingScript_ScriptChanged;
                return;
            }
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    CharAdded(e.NewStartingIndex);
                    break;

                case NotifyCollectionChangedAction.Move:
                    break;

                case NotifyCollectionChangedAction.Remove:
                    CharRemoved(e.OldItems);
                    break;

                case NotifyCollectionChangedAction.Replace:
                    break;

                case NotifyCollectionChangedAction.Reset:
                    CharsReset();
                    break;
            }
        }

        private void CharsReset()
        {
            foreach (var path in ScrollCanvas.Children.Where(x => x is Path).ToList())
            {
                ScrollCanvas.Children.Remove(path);
            }

            //ScrollCanvas.Children.Clear();
            for (int i = 0, count = Characters.Count; i < count; i++)
            {
                RenderCharacter(i, Characters[i]);
            }
        }

        private void CharRemoved(System.Collections.IList items)
        {
            foreach (var item in items)
            {
                foreach (var stroke in (item as Char).Strokes)
                {
                    ScrollCanvas.Children.Remove(GetPath(stroke));
                }
            }
        }

        private void CharAdded(int index)
        {
            Char chr = Characters[index];
            RenderCharacter(index, chr);
        }

        private void RenderCharacter(int index, Char chr)
        {
            double shiftX;
            double shiftY;
            double scaleX;
            double scaleY;
            CalcScaleInformation(chr, CharacterSize, index,
                out shiftX, out shiftY, out scaleX, out scaleY);

            // TODO Make it inside Char
            foreach (var stroke in chr.Strokes)
            {
                var path = ConvertStrokeToPath(stroke, shiftX, shiftY, scaleX, scaleY);
                AddChild(path);
            }
        }

        public void Clear()
        {
            ScrollCanvas.Children.Clear();
        }

        public void AddChild(FrameworkElement child)
        {
            if (child.Parent != null)
            {
                (child.Parent as Panel).Children.Remove(child);
            }
            if (!ScrollCanvas.Children.Contains(child))
            {
                ScrollCanvas.Children.Add(child);
            }
        }

        public void Repaint()
        {
            int count = Characters.Count;
            for (int i = 0; i < count; i++)
            {
                var chr = Characters[i];
                double shiftX;
                double shiftY;
                double scaleX;
                double scaleY;
                CalcScaleInformation(chr, CharacterSize, i, out shiftX, out shiftY, out scaleX, out scaleY);

                // TODO Make it inside Char
                foreach (var stroke in chr.Strokes)
                {
                    var path = ConvertStrokeToPath(stroke, shiftX, shiftY, scaleX, scaleY);
                    AddChild(path);
                }
            }
        }
    }
}