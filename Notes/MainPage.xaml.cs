using System;
using System.Runtime.CompilerServices;
using Windows.Devices.Input;
using Windows.Foundation;
using Windows.UI;
using Windows.UI.Input;
using Windows.UI.Input.Inking;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Xaml.Shapes;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using Windows.Graphics.Display;
using Windows.Storage.Streams;
using Windows.UI.Xaml.Media.Imaging;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace Notes
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Notes.Common.LayoutAwarePage
    {
        private static Dictionary<InkStroke, Path> _strokeMaps = new Dictionary<InkStroke, Path>();
        private Rectangle _confirmRegion;
        private Point _currentPoint;
        private InkManager _inkManager = new InkManager();
        private Brush _lineStroke = new SolidColorBrush(Colors.Green);
        private double _lineThickness = 8.0;
        private uint _penId;
        private Point _previousPoint;
        private uint _touchId;
        public MainPage()
        {
            this.InitializeComponent();
            this.DrawPad.PointerPressed += DrawPad_PointerPressed;
            this.DrawPad.PointerMoved += DrawPad_PointerMoved;
            this.DrawPad.PointerReleased += DrawPad_PointerReleased;
            this.DrawPad.PointerExited += DrawPad_PointerExited;
            this.DrawPad.PointerEntered += DrawPad_PointerEntered;
            this.ClearButton.Click += ClearButton_Click;
            DisplayProperties.OrientationChanged += DisplayProperties_OrientationChanged;
        }

        public Double LineThickness
        {
            get { return _lineThickness; }
            set { _lineThickness = value; }
        }

        private Rectangle ConfirmRegion
        {
            get
            {
                if (_confirmRegion == null)
                {
                    _confirmRegion = new Rectangle
                    {
                        Width = 60,
                        Height = 60,
                        Fill = new SolidColorBrush(Colors.YellowGreen),
                    };
                    _confirmRegion.PointerPressed += ConfirmRegion_PointerPressed;
                }
                Size size = DrawPad.RenderSize;
                _confirmRegion.Margin = new Thickness(size.Width - _confirmRegion.Width, size.Height - _confirmRegion.Height, 0, 0);
                return _confirmRegion;
            }
        }

        private InMemoryRandomAccessStream mem;

        private async void ConfirmRegion_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            if (_inkManager.GetStrokes().Count == 0)
            {
                return;
            }
            mem = new InMemoryRandomAccessStream();
            await _inkManager.SaveAsync(mem);
            mem.Seek(0);
            BitmapImage img = new BitmapImage();
            img.SetSource(mem);
            Rectangle rect = new Rectangle
            {
                Width = 40,
                Height = 40,
                Fill = new ImageBrush { ImageSource = img }
            };
            NotePad.Children.Add(rect);
        }

        private Brush LineStroke
        {
            get { return _lineStroke; }
            set { _lineStroke = value; }
        }

        private static Path GetPathFromStrokes(InkStroke stroke, Brush brush, double width, double scale = 1, double opacity = 1)
        {
            if (!_strokeMaps.ContainsKey(stroke))
            {
                var renderingStrokes = stroke.GetRenderingSegments();

                // Set up the Path to insert the segments
                Path path = new Path();
                path.Data = new PathGeometry();
                (path.Data as PathGeometry).Figures = new PathFigureCollection();
                PathFigure pathFigure = new PathFigure();
                pathFigure.StartPoint = renderingStrokes.First().Position;
                (path.Data as PathGeometry).Figures.Add(pathFigure);

                // Foreach segment, we add a BezierSegment
                foreach (var renderStroke in renderingStrokes)
                {
                    pathFigure.Segments.Add(new BezierSegment()
                    {
                        Point1 = renderStroke.BezierControlPoint1,
                        Point2 = renderStroke.BezierControlPoint2,
                        Point3 = renderStroke.Position
                    });
                }

                path.StrokeThickness = width;
                path.Stroke = brush;
                path.Opacity = opacity;
                _strokeMaps[stroke] = path;
            }
            return _strokeMaps[stroke];
        }

        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            ClearDrawPad();
            ClearInkStrokes();
        }

        private void ClearDrawPad()
        {
            DrawPad.Children.Clear();
            DrawPad.Children.Add(ConfirmRegion);
        }

        private void ClearInkStrokes()
        {
            foreach (var stroke in _inkManager.GetStrokes())
            {
                stroke.Selected = true;
            }
            _inkManager.DeleteSelected();
        }

        void DisplayProperties_OrientationChanged(object sender)
        {
            ClearButton_Click(sender, new RoutedEventArgs());
        }

        #region DrawPad Actions

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private double checkDistance(Point p1, Point p2)
        {
            return Math.Sqrt(Math.Pow(p1.X - p2.X, 2) + Math.Pow(p1.Y - p2.Y, 2));
        }

        void DrawPad_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            if (!DrawPad.Children.Contains(ConfirmRegion))
            {
                DrawPad.Children.Add(ConfirmRegion);
            }
        }

        void DrawPad_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            DrawPad_PointerReleased(sender, e);
        }

        private void DrawPad_PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            if (e.Pointer.PointerId == _penId)
            {
                PointerPoint pt = e.GetCurrentPoint(DrawPad);
                _currentPoint = pt.Position;

                if (checkDistance(_currentPoint, _previousPoint) > 2)
                {
                    Line line = new Line()
                    {
                        X1 = _previousPoint.X,
                        Y1 = _previousPoint.Y,
                        X2 = _currentPoint.X,
                        Y2 = _currentPoint.Y,
                        StrokeThickness = LineThickness,
                        Stroke = LineStroke
                    };

                    _previousPoint = _currentPoint;
                    DrawPad.Children.Add(line);

                    _inkManager.ProcessPointerUpdate(pt);
                }
            }
            else if (e.Pointer.PointerId == _touchId)
            {
                // Touch
            }
        }

        private void DrawPad_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            PointerPoint pt = e.GetCurrentPoint(DrawPad);
            _previousPoint = pt.Position;

            PointerDeviceType deviceType = e.Pointer.PointerDeviceType;
            if (deviceType == PointerDeviceType.Pen ||
                (deviceType == PointerDeviceType.Mouse && pt.Properties.IsLeftButtonPressed))
            {
                _inkManager.ProcessPointerDown(pt);
                _penId = pt.PointerId;

                e.Handled = true;
            }
            else if (deviceType == PointerDeviceType.Mouse && pt.Properties.IsRightButtonPressed)
            {
                // Right Click
            }
            else if (deviceType == PointerDeviceType.Touch)
            {
                // Touch
            }
        }

        private void DrawPad_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            if (e.Pointer.PointerId == _penId)
            {
                PointerPoint pt = e.GetCurrentPoint(DrawPad);
                _inkManager.ProcessPointerUp(pt);
                ClearDrawPad();
                foreach (InkStroke stroke in _inkManager.GetStrokes())
                {
                    RenderStrokeOnDrawPad(stroke, LineStroke, LineThickness);
                }
            }
            else if (e.Pointer.PointerId == _touchId)
            {
                // Touch
            }
            _touchId = 0;
            _penId = 0;
            e.Handled = true;
        }
        #endregion DrawPad Actions

        private void RenderStrokeOnNotePad(InkStroke stroke, Brush brush, double width, double scale)
        {
            Path path = GetPathFromStrokes(stroke, brush, width, 0.1);
        }

        private void RenderStrokeOnDrawPad(InkStroke stroke, Brush brush, double width, double opacity = 1)
        {
            Path path = GetPathFromStrokes(stroke, brush, width);
            DrawPad.Children.Add(path);
        }
    }
}