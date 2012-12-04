using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Callisto.Controls;
using DrawToNote.Common;
using DrawToNote.Datas;
using Windows.Devices.Input;
using Windows.Foundation;
using Windows.UI.Core;
using Windows.UI.Input;
using Windows.UI.Input.Inking;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
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
        #region datas

        private List<Stroke> _strokeCached = new List<Stroke>();
        private Button clearButton;
        private Button confirmButton;
        private Point currentPoint;
        private bool inSpecialRegion = false;
        private uint penId;
        private Point previousPoint;
        private ScriptManager scriptManager = ScriptManager.Instance;
        private uint touchId;

        public Double LineThickness
        {
            get
            {
                return DefaultValue.DefaultLineWidth;
            }
            set
            {
                if (DefaultValue.DefaultLineWidth == value)
                {
                    return;
                }
                DefaultValue.DefaultLineWidth = value;
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

        private SolidColorBrush LineStroke
        {
            get
            {
                return new SolidColorBrush(DefaultValue.DefaultLineColor);
            }
            set
            {
                DefaultValue.DefaultLineColor = value.Color;
            }
        }

        #endregion datas

        public ScriptPage()
        {
            this.InitializeComponent();
            HandleEvents();
            HandleResource();
            BlackColoredRectangleButton.Checked = true;
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
        private void ClearDrawPad()
        {
            DrawPad.Children.Clear();
            DrawPad.Children.Add(ConfirmButton);
            DrawPad.Children.Add(ClearButton);
        }

        private void ClearInkStrokes()
        {
            scriptManager.ClearInkStrokes();

            _strokeCached.Clear();
        }

        private void ColoredRectangleButton_Check(object sender, DependencyPropertyChangedEventArgs e)
        {
            foreach (var child in LeftTopCommands.Children)
            {
                if (child is ColoredRectangleButton && !child.Equals(sender) && (child as ColoredRectangleButton).Checked)
                {
                    (child as ColoredRectangleButton).Checked = false;
                }
            }
            LineStroke = (sender as ColoredRectangleButton).ButtonColor;
        }

        private void HandleResource()
        {
            ResourceDictionary standard = new ResourceDictionary();
            standard.Source = new Uri("ms-appx:///Common/NoteResources.xaml", UriKind.Absolute);
            Resources.MergedDictionaries.Add(standard);
        }

        #region actions

        protected override void GoBack(object sender, RoutedEventArgs e)
        {
            NotePad.Clear();
            scriptManager.CurrentScript.Save();
            this.Frame.Navigate(typeof(NotesPage), scriptManager.CurrentScript);
        }

        private async void AppBarClearButton_Click(object sender, RoutedEventArgs e)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                () =>
                {
                    scriptManager.CurrentScript.Clear();
                });
        }

        private async void AppBarDeleteButton_Click(object sender, RoutedEventArgs e)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                () =>
                {
                    scriptManager.CurrentScript.RemoveLast();
                });
        }

        private void AppBarNewScriptButton_Click(object sender, RoutedEventArgs e)
        {
            scriptManager.CreateScript();
        }

        // Save Current Scripts
        private async void AppBarSaveButton_Click(object sender, RoutedEventArgs e)
        {
            await scriptManager.CurrentScript.SaveAsync();
        }

        private void HandleEvents()
        {
            DrawPad.PointerPressed += DrawPad_PointerPressed;
            DrawPad.PointerMoved += DrawPad_PointerMoved;
            DrawPad.PointerReleased += DrawPad_PointerReleased;
            DrawPad.PointerExited += DrawPad_PointerExited;
            DrawPad.PointerEntered += DrawPad_PointerEntered;

            //BackButton.Click += BackButton_Click;
            AppBarClearButton.Click += AppBarClearButton_Click;
            AppBarDeleteButton.Click += AppBarDeleteButton_Click;
            LineWidthButton.Click += LineWidthButton_Click;
        }

        // TODO should it be removed?
        //private void BackButton_Click(object sender, RoutedEventArgs e)
        //{
        //    ClearDrawPad();
        //    ClearInkStrokes();
        //}

        private void LineWidthButton_Click(object sender, RoutedEventArgs e)
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
        private void ScriptTitle_TextChanged(object sender, TextChangedEventArgs e)
        {
            scriptManager.CurrentScript.Title = (sender as TextBox).Text;
        }

        #region confirmRegion actions

        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            ClearDrawPad();
            ClearInkStrokes();
            LeaveSpecialRegion();
        }

        private void ClearButton_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            EnterSpecialRegion();
        }

        private void ClearButton_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            LeaveSpecialRegion();
        }

        private void ConfirmButton_Click(object sender, RoutedEventArgs e)
        {
            scriptManager.ConfirmCharacter(
                DrawPad.RenderSize, _strokeCached);
            ClearButton_Click(sender, null);
            LeaveSpecialRegion();
        }

        private void ConfirmButton_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            EnterSpecialRegion();
        }

        private void ConfirmButton_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            LeaveSpecialRegion();
        }

        private void EnterSpecialRegion()
        {
            inSpecialRegion = true;
        }

        private void LeaveSpecialRegion()
        {
            inSpecialRegion = false;
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

                InkStroke inkStroke = scriptManager.GetStrokes().Last();
                Stroke stroke = new Stroke(inkStroke);
                stroke.LineWidth = LineThickness;
                stroke.Brush = LineStroke;
                RenderStrokeOnDrawPad(stroke);
                _strokeCached.Add(stroke);
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

        #endregion actions

        private void RenderStrokeOnDrawPad(Stroke stroke, double opacity = 1)
        {
            Path path = stroke.CreatePath();
            path.StrokeThickness = stroke.LineWidth;
            path.Stroke = stroke.Brush;
            path.Opacity = opacity;
            DrawPad.Children.Add(path);
        }

        private void RePaint(object sender)
        {
            ClearButton_Click(sender, null);
            NotePad.Repaint();
        }
        #region Storyboard

        private void Storyboard_Completed_Filled(object sender, object e)
        {
            RePaint(sender);
        }

        private void Storyboard_Completed_Landscape(object sender, object e)
        {
            RePaint(sender);
        }

        private void Storyboard_Completed_Portrait(object sender, object e)
        {
            RePaint(sender);
        }
        private void Storyboard_Completed_Snapped(object sender, object e)
        {
            RePaint(sender);
        }

        #endregion Storyboard
    }
}