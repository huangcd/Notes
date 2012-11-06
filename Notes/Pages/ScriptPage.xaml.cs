using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using DrawToNote.Common;
using DrawToNote.Datas;
using Windows.Devices.Input;
using Windows.Foundation;
using Windows.Storage;
using Windows.UI;
using Windows.UI.Input;
using Windows.UI.Input.Inking;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace DrawToNote.Pages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class ScriptPage : LayoutAwarePage
    {
        private static Dictionary<InkStroke, Path> _strokeMaps = new Dictionary<InkStroke, Path>();
        private Brush _lineStroke = new SolidColorBrush(new Color { R = 0, G = 0, B = 0, A = 255 });
        private double _lineThickness = 8.0;
        private Button clearButton;
        private Button confirmButton;
        private Point currentPoint;
        private bool inSpecialRegion = false;
        private ScriptManager scriptManager = ScriptManager.Instance;
        private uint penId;
        private Point previousPoint;
        private uint touchId;

        protected override void LoadState(object navigationParameter, Dictionary<string, object> pageState)
        {
        }

        public ScriptPage()
        {
            this.InitializeComponent();
            HandleEvents();
            ConfigInkAttributes();
            HandleResource();
        }

        private void HandleResource()
        {
            ResourceDictionary standard = new ResourceDictionary();
            standard.Source = new Uri("ms-appx:///Common/StandardStyles.xaml", UriKind.Absolute);
            Resources.MergedDictionaries.Add(standard);
        }

        public Double LineThickness
        {
            get { return _lineThickness; }
            set { _lineThickness = value; }
        }

        private Button ConfirmButton
        {
            get
            {
                if (confirmButton == null)
                {
                    confirmButton = new Button
                    {
                        Style = (Style)Resources["ConfirmCharButtonStyle"],
                        Width = 100,
                        Height = 80,
                    };
                    confirmButton.Click += ConfirmButton_Click;
                    confirmButton.PointerEntered += ConfirmButton_PointerEntered;
                    confirmButton.PointerExited += ConfirmButton_PointerExited;
                }
                Size size = DrawPad.RenderSize;
                confirmButton.Margin = new Thickness(size.Width - confirmButton.Width, size.Height - confirmButton.Height, 0, 0);
                return confirmButton;
            }
        }

        private Button ClearButton
        {
            get
            {
                if (clearButton == null)
                {
                    clearButton = new Button
                    {
                        Style = (Style)Resources["ClearCharButtonStyle"],
                        Width = 100,
                        Height = 80,
                    };
                    clearButton.Click += ClearButton_Click;
                    clearButton.PointerExited += ClearButton_PointerExited;
                    clearButton.PointerEntered += ClearButton_PointerEntered;
                }
                Size size = DrawPad.RenderSize;
                clearButton.Margin = new Thickness(0, size.Height - clearButton.Height, 0, 0);
                return clearButton;
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

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            ClearDrawPad();
            ClearInkStrokes();
        }

        private void ClearDrawPad()
        {
            DrawPad.Children.Clear();
            DrawPad.Children.Add(ConfirmButton);
            DrawPad.Children.Add(ClearButton);
        }

        private void ClearInkStrokes()
        {
            scriptManager.ClearInkStrokes();
        }

        private void ConfigInkAttributes()
        {
            scriptManager.ConfigInkDrawingAttributes((LineStroke as SolidColorBrush).Color, new Size(8, 8));
        }

        private void HandleEvents()
        {
            #region DrawPad

            DrawPad.PointerPressed += DrawPad_PointerPressed;
            DrawPad.PointerMoved += DrawPad_PointerMoved;
            DrawPad.PointerReleased += DrawPad_PointerReleased;
            DrawPad.PointerExited += DrawPad_PointerExited;
            DrawPad.PointerEntered += DrawPad_PointerEntered;

            #endregion DrawPad

            BackButton.Click += BackButton_Click;
            AppBarSaveButton.Click += AppBarSaveButton_Click;
            AppBarClearButton.Click += AppBarClearButton_Click;
            AppBarDeleteButton.Click += AppBarDeleteButton_Click;
            AppBarNewScriptButton.Click += AppBarNewScriptButton_Click;
        }

        private void AppBarNewScriptButton_Click(object sender, RoutedEventArgs e)
        {
            scriptManager.CreateScript();
        }

        private async void AppBarDeleteButton_Click(object sender, RoutedEventArgs e)
        {
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal,
                () =>
                {
                    scriptManager.CurrentScript.RemoveLast();
                });
        }

        private async void AppBarClearButton_Click(object sender, RoutedEventArgs e)
        {
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal,
                () =>
                {
                    scriptManager.CurrentScript.Clear();
                });
        }

        // Save Current Scripts
        private void AppBarSaveButton_Click(object sender, RoutedEventArgs e)
        {
            StorageFolder localFolder = ApplicationData.Current.LocalFolder;
            scriptManager.CurrentScript.SaveAsync(localFolder);
        }

        #region confirmRegion actions

        private void ConfirmButton_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            inSpecialRegion = true;
        }

        private void ConfirmButton_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            inSpecialRegion = false;
        }

        private void ClearButton_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            inSpecialRegion = true;
        }

        private void ClearButton_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            inSpecialRegion = false;
        }

        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            ClearDrawPad();
            ClearInkStrokes();
        }

        private void ConfirmButton_Click(object sender, RoutedEventArgs e)
        {
            List<Path> paths = scriptManager.ConfirmCharacter
                (NotePad.CharacterSize, NotePad.RenderSize, DrawPad.RenderSize);
            foreach (var path in paths)
            {
                NotePad.AddChild(path);
            }
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
            if (!DrawPad.Children.Contains(ConfirmButton))
            {
                DrawPad.Children.Add(ConfirmButton);
            }
            if (!DrawPad.Children.Contains(ClearButton))
            {
                DrawPad.Children.Add(ClearButton);
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

                    scriptManager.ProcessPointerUpdate(pt);
                }
            }
            else if (e.Pointer.PointerId == touchId)
            {
            }
        }

        private void DrawPad_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            if (inSpecialRegion)
            {
                return;
            }

            PointerPoint pt = e.GetCurrentPoint(DrawPad);
            previousPoint = pt.Position;

            PointerDeviceType deviceType = e.Pointer.PointerDeviceType;
            if (deviceType == PointerDeviceType.Pen ||
                (deviceType == PointerDeviceType.Mouse && pt.Properties.IsLeftButtonPressed) ||
                deviceType == PointerDeviceType.Touch)
            {
                scriptManager.ProcessPointerDown(pt);
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
                scriptManager.ProcessPointerUp(pt);
                ClearDrawPad();
                foreach (InkStroke stroke in scriptManager.GetStrokes())
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

        private async void RePaint(object sender)
        {
            await this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal,
                () =>
                {
                    ClearButton_Click(sender, null);
                    scriptManager.RepaintAll(NotePad.RenderSize, NotePad.CharacterSize);
                });
        }

        private void Storyboard_Completed_Portrait(object sender, object e)
        {
            RePaint(sender);
        }

        private void Storyboard_Completed_Landscape(object sender, object e)
        {
            RePaint(sender);
        }
    }
}