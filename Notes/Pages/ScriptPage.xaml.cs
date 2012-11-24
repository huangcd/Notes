using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using DrawToNote.Common;
using DrawToNote.Datas;
using Windows.Devices.Input;
using Windows.Foundation;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Input;
using Windows.UI.Input.Inking;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;
using Callisto.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Popups;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace DrawToNote.Pages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class ScriptPage : LayoutAwarePage
    {
        private Dictionary<InkStroke, Path> _strokeMaps = new Dictionary<InkStroke, Path>();
        private Brush _lineStroke = new SolidColorBrush(new Color { R = 0, G = 0, B = 0, A = 255 });
        private double _lineThickness = 8.0;
        private Button clearButton;
        private Button confirmButton;
        private Point currentPoint;
        private bool _inSpecialRegion = false;
        private ScriptManager scriptManager = ScriptManager.Instance;
        private uint penId;
        private Point previousPoint;
        private uint touchId;

        public bool InSpecialRegion
        {
            get
            {
                return _inSpecialRegion;
            }
        }

        protected override void LoadState(object navigationParameter, Dictionary<string, object> pageState)
        {
            if (navigationParameter == null)
            {
                scriptManager.CreateScript();
            }
            else
            {
                Script script = navigationParameter as Script;
                scriptManager.CurrentScript = script;
            }
            this.DataContext = scriptManager.CurrentScript;
            NotePad.Characters = scriptManager.CurrentScript.Characters;
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
            standard.Source = new Uri("ms-appx:///Common/NoteResources.xaml", UriKind.Absolute);
            Resources.MergedDictionaries.Add(standard);
        }

        public Double LineThickness
        {
            get
            {
                return _lineThickness;
            }
            set
            {
                if (_lineThickness == value)
                {
                    return;
                }
                _lineThickness = value;
                HandlerLineThicknessChanged();
            }
        }

        public async void HandlerLineThicknessChanged()
        {
            await this.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                foreach (var path in _strokeMaps.Values)
                {
                    path.StrokeThickness = LineThickness;
                }
            });
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
                confirmButton.Margin = new Thickness(size.Width - confirmButton.Width - 5, size.Height - confirmButton.Height, 0, 0);
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

        private Path GetPathFromStrokes(InkStroke stroke, double scale = 1, double opacity = 1)
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

                path.StrokeThickness = LineThickness;
                path.Stroke = LineStroke;
                path.Opacity = opacity;
                _strokeMaps[stroke] = path;
            }
            return _strokeMaps[stroke];
        }

        // TODO should it be removed?
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
            // TODO test whether it's OK to clear
            _strokeMaps.Clear();
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

            //AppBarSaveButton.Click += AppBarSaveButton_Click;
            AppBarClearButton.Click += AppBarClearButton_Click;
            AppBarDeleteButton.Click += AppBarDeleteButton_Click;
            LineWidthButton.Click += LineWidthButton_Click;

            //AppBarNewScriptButton.Click += AppBarNewScriptButton_Click;
        }

        void LineWidthButton_Click(object sender, RoutedEventArgs e)
        {
            Flyout f = new Flyout();

            f.Placement = PlacementMode.Top;
            f.PlacementTarget = sender as UIElement; // this is an UI element (usually the sender)

            LineWidthSelector selector = new LineWidthSelector();
            selector.Width = 400;
            selector.Height = 100;

            f.Content = selector;
            selector.ValueChanged += (_sender, _e) =>
            {
                LineThickness = _e.NewValue;
                NotePad.LineWidth = LineThickness;
            };

            f.IsOpen = true;
        }

        private void AppBarNewScriptButton_Click(object sender, RoutedEventArgs e)
        {
            scriptManager.CreateScript();
        }

        private async void AppBarDeleteButton_Click(object sender, RoutedEventArgs e)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                () =>
                {
                    scriptManager.CurrentScript.RemoveLast();
                });
        }

        private async void AppBarClearButton_Click(object sender, RoutedEventArgs e)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                () =>
                {
                    scriptManager.CurrentScript.Clear();
                });
        }

        // Save Current Scripts
        private async void AppBarSaveButton_Click(object sender, RoutedEventArgs e)
        {
            await scriptManager.CurrentScript.SaveAsync();
        }

        #region confirmRegion actions

        private void ConfirmButton_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            _inSpecialRegion = true;
        }

        private void ConfirmButton_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            _inSpecialRegion = false;
        }

        private void ClearButton_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            _inSpecialRegion = true;
        }

        private void ClearButton_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            _inSpecialRegion = false;
        }

        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            ClearDrawPad();
            ClearInkStrokes();
            _inSpecialRegion = false;
        }

        private void ConfirmButton_Click(object sender, RoutedEventArgs e)
        {
            scriptManager.ConfirmCharacter(
                NotePad.CharacterSize,
                NotePad.RenderSize,
                DrawPad.RenderSize,
                NotePad);
            ClearButton_Click(sender, null);
            _inSpecialRegion = false;
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

                if (checkDistance(currentPoint, previousPoint) > 4)
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
            if (_inSpecialRegion)
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
                    RenderStrokeOnDrawPad(stroke);
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

        private void RenderStrokeOnDrawPad(InkStroke stroke, double opacity = 1)
        {
            Path path = GetPathFromStrokes(stroke);
            DrawPad.Children.Add(path);
        }

        private void RePaint(object sender)
        {
            //await this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal,
            //    () =>
            //    {
            ClearButton_Click(sender, null);
            NotePad.Repaint();
            //});
        }

        protected override void GoBack(object sender, RoutedEventArgs e)
        {
            NotePad.Clear();
            scriptManager.CurrentScript.Save();
            this.Frame.Navigate(typeof(NotesPage), scriptManager.CurrentScript);
        }

        private void ScriptTitle_TextChanged(object sender, TextChangedEventArgs e)
        {
            scriptManager.CurrentScript.Title = (sender as TextBox).Text;
        }

        #region Storyboard
        private void Storyboard_Completed_Portrait(object sender, object e)
        {
            RePaint(sender);
        }

        private void Storyboard_Completed_Landscape(object sender, object e)
        {
            RePaint(sender);
        }

        private void Storyboard_Completed_Filled(object sender, object e)
        {
            RePaint(sender);
        }

        private void Storyboard_Completed_Snapped(object sender, object e)
        {
            RePaint(sender);
        } 
        #endregion
    }
}