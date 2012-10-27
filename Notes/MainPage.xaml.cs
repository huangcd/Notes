using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Windows.Devices.Input;
using Windows.Foundation;
using Windows.Graphics.Display;
using Windows.UI;
using Windows.UI.Input;
using Windows.UI.Input.Inking;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace Notes
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Notes.Common.LayoutAwarePage
    {
        private static Dictionary<InkStroke, Path> _strokeMaps = new Dictionary<InkStroke, Path>();
        private Brush _lineStroke = new SolidColorBrush(Colors.Green);
        private double _lineThickness = 8.0;
        private Rectangle confirmRegion;
        private Rectangle clearRegion;
        private Point currentPoint;
        private bool inComfirmRegion = false;
        private InkManager inkManager = new InkManager();
        private uint penId;
        private Point previousPoint;
        private uint touchId;

        public MainPage()
        {
            this.InitializeComponent();
            HandleEvents();
            ConfigInkAttributes();
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
                if (confirmRegion == null)
                {
                    confirmRegion = new Rectangle
                    {
                        Width = 60,
                        Height = 60,
                        Fill = new SolidColorBrush(Colors.YellowGreen),
                    };
                    confirmRegion.PointerPressed += ConfirmRegion_PointerPressed;
                    confirmRegion.PointerEntered += _confirmRegion_PointerEntered;
                    confirmRegion.PointerExited += _confirmRegion_PointerExited;
                }
                Size size = DrawPad.RenderSize;
                confirmRegion.Margin = new Thickness(size.Width - confirmRegion.Width, size.Height - confirmRegion.Height, 0, 0);
                return confirmRegion;
            }
        }

        private Rectangle ClearRegion
        {
            get
            {
                if (clearRegion == null)
                {
                    clearRegion = new Rectangle
                    {
                        Width = 60,
                        Height = 60,
                        Fill = new SolidColorBrush(Colors.Gray)
                    };
                }
                Size size = DrawPad.RenderSize;
                clearRegion.Margin = new Thickness(0, size.Height - clearRegion.Height, 0, 0);
                return clearRegion;
            }
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
            foreach (var stroke in inkManager.GetStrokes())
            {
                stroke.Selected = true;
            }
            inkManager.DeleteSelected();
        }

        private void ConfigInkAttributes()
        {
            InkDrawingAttributes inkAttributes = new InkDrawingAttributes
            {
                Color = (LineStroke as SolidColorBrush).Color,
                Size = new Size(8, 8),
                PenTip = PenTipShape.Circle,
                FitToCurve = true
            };
            inkManager.SetDefaultDrawingAttributes(inkAttributes);
        }

        private void DisplayProperties_OrientationChanged(object sender)
        {
            ClearButton_Click(sender, new RoutedEventArgs());
            NotePad.RePaintAll();
        }

        private void HandleEvents()
        {
            DrawPad.PointerPressed += DrawPad_PointerPressed;
            DrawPad.PointerMoved += DrawPad_PointerMoved;
            DrawPad.PointerReleased += DrawPad_PointerReleased;
            DrawPad.PointerExited += DrawPad_PointerExited;
            DrawPad.PointerEntered += DrawPad_PointerEntered;
            ClearButton.Click += ClearButton_Click;
            DisplayProperties.OrientationChanged += DisplayProperties_OrientationChanged;
        }
        #region confirmRegion actions

        private void _confirmRegion_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            inComfirmRegion = true;
        }

        private void _confirmRegion_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            inComfirmRegion = false;
        }
        private async void ConfirmRegion_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            await NotePad.AddCharacterAsync(inkManager);
            ClearButton_Click(sender, null);
        }

        #endregion confirmRegion actions
        #region DrawPad Actions

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private double checkDistance(Point p1, Point p2)
        {
            return Math.Sqrt(Math.Pow(p1.X - p2.X, 2) + Math.Pow(p1.Y - p2.Y, 2));
        }

        private void DrawPad_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            if (!DrawPad.Children.Contains(ConfirmRegion))
            {
                DrawPad.Children.Add(ConfirmRegion);
            }
        }

        private void DrawPad_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            DrawPad_PointerReleased(sender, e);
        }

        private void DrawPad_PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            if (e.Pointer.PointerId == penId)
            {
                PointerPoint pt = e.GetCurrentPoint(DrawPad);
                currentPoint = pt.Position;

                if (checkDistance(currentPoint, previousPoint) > 2)
                {
                    Line line = new Line()
                    {
                        X1 = previousPoint.X,
                        Y1 = previousPoint.Y,
                        X2 = currentPoint.X,
                        Y2 = currentPoint.Y,
                        StrokeThickness = LineThickness,
                        Stroke = LineStroke
                    };

                    previousPoint = currentPoint;
                    DrawPad.Children.Add(line);

                    inkManager.ProcessPointerUpdate(pt);
                }
            }
            else if (e.Pointer.PointerId == touchId)
            {
                // Touch
            }
        }

        private void DrawPad_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            if (inComfirmRegion)
            {
                return;
            }

            PointerPoint pt = e.GetCurrentPoint(DrawPad);
            previousPoint = pt.Position;

            PointerDeviceType deviceType = e.Pointer.PointerDeviceType;
            if (deviceType == PointerDeviceType.Pen ||
                (deviceType == PointerDeviceType.Mouse && pt.Properties.IsLeftButtonPressed))
            {
                inkManager.ProcessPointerDown(pt);
                penId = pt.PointerId;

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
            if (e.Pointer.PointerId == penId)
            {
                PointerPoint pt = e.GetCurrentPoint(DrawPad);
                inkManager.ProcessPointerUp(pt);
                ClearDrawPad();
                foreach (InkStroke stroke in inkManager.GetStrokes())
                {
                    RenderStrokeOnDrawPad(stroke, LineStroke, LineThickness);
                }
            }
            else if (e.Pointer.PointerId == touchId)
            {
                // Touch
            }
            touchId = 0;
            penId = 0;
            e.Handled = true;
        }

        #endregion DrawPad Actions

        private void RenderStrokeOnDrawPad(InkStroke stroke, Brush brush, double width, double opacity = 1)
        {
            Path path = GetPathFromStrokes(stroke, brush, width);
            DrawPad.Children.Add(path);
        }

        private void RenderStrokeOnNotePad(InkStroke stroke, Brush brush, double width, double scale)
        {
            Path path = GetPathFromStrokes(stroke, brush, width, 0.1);
        }
    }
}