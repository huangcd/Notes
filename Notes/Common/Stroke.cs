using System.Collections.Generic;
using System.Linq;
using Windows.Foundation;
using Windows.UI.Input.Inking;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;

namespace Notes.Common
{
    internal class Stroke
    {
        private List<BezierSegment> segments = new List<BezierSegment>();
        private Path path;

        public Stroke(InkStroke stroke, Size size)
        {
            var renderStrokes = stroke.GetRenderingSegments();
            StartPoint = renderStrokes.First().Position;
            foreach (var renderStroke in renderStrokes)
            {
                BezierSegments.Add(new BezierSegment
                {
                    Point1 = renderStroke.BezierControlPoint1,
                    Point2 = renderStroke.BezierControlPoint2,
                    Point3 = renderStroke.Position
                });
            }
            CanvasSize = size;
        }

        internal Stroke()
        {
        }

        internal List<BezierSegment> BezierSegments { get { return segments; } }

        internal Size CanvasSize { get; set; }

        internal Point StartPoint { get; set; }

        public Path AsPath(Brush brush, double thickness, double shiftX, double shiftY, double scaleX, double scaleY, double opacity = 1)
        {
            if (path == null)
            {
                CreatePath();
            }

            path.StrokeThickness = thickness;
            path.Stroke = brush;
            path.Opacity = opacity;
            path.RenderTransform = new ScaleTransform
            {
                ScaleX = scaleX,
                ScaleY = scaleY
            };
            path.Margin = new Thickness(shiftX, shiftY, 0, 0);
            return path;
        }

        private void CreatePath()
        {
            path = new Windows.UI.Xaml.Shapes.Path();
            path.Data = new PathGeometry();
            (path.Data as PathGeometry).Figures = new PathFigureCollection();
            PathFigure pathFigure = new PathFigure();
            pathFigure.StartPoint = StartPoint;
            (path.Data as PathGeometry).Figures.Add(pathFigure);
            foreach (var segments in BezierSegments)
            {
                pathFigure.Segments.Add(new BezierSegment
                {
                    Point1 = segments.Point1,
                    Point2 = segments.Point2,
                    Point3 = segments.Point3
                });
            }
        }

        public void Delete()
        {
            if (path.Parent != null && path.Parent is Canvas)
            {
                (path.Parent as Canvas).Children.Remove(path);
            }
        }
    }
}