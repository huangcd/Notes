using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Windows.Foundation;
using Windows.UI.Input.Inking;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;
using System;

namespace DrawToNote.Datas
{
    internal class Stroke
    {
        [JsonIgnore]
        private List<BezierSegment> _segments = new List<BezierSegment>();

        public Stroke(InkStroke stroke, [CallerMemberName] string methodName = "")
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
        }

        public Stroke()
        {
        }

        [JsonProperty]
        internal String BezierSegmentsStr
        {
            get
            {
                return String.Join(",", BezierSegments.Select(x => x.ToJson()));
            }
            private set
            {
                _segments = new List<BezierSegment>();
                string[] strings = value.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                var array = strings.Select(Convert.ToDouble).ToArray();
                if (array.Length > 0)
                {
                    StartPoint = new Point(array[0], array[1]);
                }
                for (int i = 0; i < array.Length; )
                {
                    _segments.Add(new BezierSegment
                    {
                        Point1 = new Point(array[i++], array[i++]),
                        Point2 = new Point(array[i++], array[i++]),
                        Point3 = new Point(array[i++], array[i++])
                    });
                }
            }
        }

        [JsonIgnore]
        internal List<BezierSegment> BezierSegments { get { return _segments; } }

        [JsonProperty]
        internal double LineWidth { get; set; }

        [JsonProperty]
        internal Brush Brush { get; set; }

        [JsonIgnore]
        internal Point StartPoint { get; set; }

        public Path CreatePath()
        {
            Path path = new Windows.UI.Xaml.Shapes.Path();
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
            return path;
        }
    }

    public static class JsonExtension
    {
        public static string ToJson(this double val)
        {
            return string.Format("{0:0.00}", val);
        }

        public static string ToJson(this BezierSegment val)
        {
            return string.Join(",", 
                val.Point1.X.ToJson(), val.Point1.Y.ToJson(), 
                val.Point2.X.ToJson(), val.Point2.Y.ToJson(), 
                val.Point3.X.ToJson(), val.Point3.Y.ToJson());
        }
    }
}