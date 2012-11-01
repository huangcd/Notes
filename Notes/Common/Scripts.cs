﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Windows.Foundation;
using Windows.Storage.Streams;
using Windows.UI;
using Windows.UI.Input;
using Windows.UI.Input.Inking;
using Windows.UI.Xaml.Media;

namespace Notes.Common
{
    public class Script : IEnumerable<Char>
    {
        internal List<Char> characters = new List<Char>();
        private static UTF8Encoding encoding = new UTF8Encoding();
        private const double ScaleConstance = 1.2;

        public Color DefaultCharColor { get; set; }

        public double DefaultThickness { get; set; }

        public int Count { get { return characters.Count; } }

        public Char this[int index]
        {
            get
            {
                return characters[index];
            }
        }

        public Script()
        {
            CreateDate = DateTime.Now;
        }

        internal DateTime CreateDate { get; set; }

        internal String Title { get; set; }

        public void Add(Char chr)
        {
            characters.Add(chr);
        }

        internal void RepaintAll(Size noteSize, Size charSize)
        {
            int count = Count;
            for (int i = 0; i < count; i++)
            {
                var chr = this[i];
                double shiftX;
                double shiftY;
                double scaleX;
                double scaleY;
                CalcScaleInformation(chr, ref charSize, ref noteSize, i, out shiftX, out shiftY, out scaleX, out scaleY);
                Brush brush = new SolidColorBrush(DefaultCharColor);
                foreach (var stroke in chr.Strokes)
                {
                    stroke.AsPath(brush, DefaultThickness, shiftX, shiftY, scaleX, scaleY);
                }
            }
        }

        public List<Windows.UI.Xaml.Shapes.Path> AddAndRender(Char chr, Size charSize, Size noteSize)
        {
            int index = Count;
            Add(chr);
            double shiftX;
            double shiftY;
            double scaleX;
            double scaleY;
            CalcScaleInformation(chr, ref charSize, ref noteSize, index, out shiftX, out shiftY, out scaleX, out scaleY);
            var paths = new List<Windows.UI.Xaml.Shapes.Path>();
            Brush brush = new SolidColorBrush(DefaultCharColor);
            foreach (var stroke in chr.Strokes)
            {
                paths.Add(stroke.AsPath(brush, DefaultThickness, shiftX, shiftY, scaleX, scaleY));
            }
            return paths;
        }

        private static void CalcScaleInformation(
            Char chr,
            ref Size charSize,
            ref Size noteSize,
            int index,
            out double shiftX,
            out double shiftY,
            out double scaleX,
            out double scaleY)
        {
            int columnCount = (int)(noteSize.Width / charSize.Width);
            int row = index / columnCount;
            int column = index % columnCount;
            shiftX = column * charSize.Width;
            shiftY = row * charSize.Height;
            scaleX = charSize.Width * ScaleConstance / chr.CanvasSize.Width;
            scaleY = charSize.Height * ScaleConstance / chr.CanvasSize.Height;
        }

        public async Task LoadAsync(IInputStream input)
        {
            Stream stream = input.AsStreamForRead();
            int length = (int)stream.Length;
            byte[] bytes = new byte[length];
            await stream.ReadAsync(bytes, 0, length);
            String content = encoding.GetString(bytes, 0, length);
            JObject obj = (JObject)JsonConvert.DeserializeObject(content);
            this.LoadFromJObject(obj);
        }

        public async Task SaveAsync(IOutputStream output)
        {
            JObject obj = this.ToJsonObject();
            byte[] bytes = encoding.GetBytes(obj.ToString());
            await output.WriteAsync(bytes.AsBuffer());
        }

        public IEnumerator<Char> GetEnumerator()
        {
            foreach (var chr in characters)
            {
                yield return chr;
            }
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            foreach (var chr in characters)
            {
                yield return chr;
            }
        }
    }

    internal static class JsonExtension
    {
        public static JArray GetJArray(this JObject obj, string propertyName)
        {
            return obj.GetValue(propertyName) as JArray;
        }

        public static JObject GetJObject(this JObject obj, string propertyName)
        {
            return obj.GetValue(propertyName) as JObject;
        }

        public static JValue GetJValue(this JObject obj, string propertyName)
        {
            return obj.GetValue(propertyName) as JValue;
        }

        public static void LoadFromJObject(this Script scripts, JObject obj)
        {
            scripts.Title = (string)obj.GetJValue("Title");
            JArray characters = obj.GetJArray("Content");
            foreach (var chr in characters)
            {
                scripts.characters.Add((chr as JObject).ToNoteCharacter());
            }
        }

        public static BezierSegment ToBezierSegment(this JObject obj)
        {
            Point point1 = obj.GetJObject("Point1").ToPoint();
            Point point2 = obj.GetJObject("Point2").ToPoint();
            Point point3 = obj.GetJObject("Point3").ToPoint();

            return new BezierSegment
            {
                Point1 = point1,
                Point2 = point2,
                Point3 = point3,
            };
        }

        public static byte[] ToBinary(this JValue val)
        {
            return Convert.FromBase64String(val.Value<string>());
        }

        public static Char ToInkData(this JObject array)
        {
            Char chr = new Char();
            foreach (var item in array.GetJArray("Strokes"))
            {
                chr.Strokes.Add((item as JObject).ToStrokeData());
            }
            chr.CanvasSize = array.GetJObject("CanvasSize").ToSize();
            chr.InkBytes = array.GetJValue("InkBytes").ToBinary();
            return chr;
        }

        public static JObject ToJsonObject(this Script script)
        {
            JObject obj = new JObject();
            obj.Add("Title", script.Title);
            JArray notes = new JArray();
            foreach (var chr in script.characters)
            {
                notes.Add(chr.ToJsonObject());
            }
            obj.Add("Content", notes);
            return obj;
        }

        public static JObject ToJsonObject(this Point point)
        {
            JObject obj = new JObject();
            obj.Add("X", new JValue(point.X));
            obj.Add("Y", new JValue(point.Y));
            return obj;
        }

        public static JObject ToJsonObject(this BezierSegment points)
        {
            JObject obj = new JObject();
            obj.Add("Point1", points.Point1.ToJsonObject());
            obj.Add("Point2", points.Point2.ToJsonObject());
            obj.Add("Point3", points.Point3.ToJsonObject());
            return obj;
        }

        public static JObject ToJsonObject(this Size size)
        {
            JObject obj = new JObject();
            obj.Add("Width", size.Width);
            obj.Add("Height", size.Height);
            return obj;
        }

        public static JObject ToJsonObject(this Stroke stroke)
        {
            JObject obj = new JObject();
            obj.Add("StartPoint", stroke.StartPoint.ToJsonObject());
            obj.Add("CanvasSize", stroke.CanvasSize.ToJsonObject());
            JArray segments = new JArray();
            foreach (var segment in stroke.BezierSegments)
            {
                segments.Add(segment.ToJsonObject());
            }
            obj.Add("BezierSegments", segments);
            return obj;
        }

        public static JValue ToJsonObject(this byte[] bytes)
        {
            return new JValue(Convert.ToBase64String(bytes));
        }

        public static JObject ToJsonObject(this Char chr)
        {
            JObject obj = new JObject();
            JArray arr = new JArray();
            foreach (var stroke in chr.Strokes)
            {
                arr.Add(stroke.ToJsonObject());
            }
            obj.Add("Strokes", arr);
            obj.Add("CanvasSize", chr.CanvasSize.ToJsonObject());
            obj.Add("InkBytes", chr.InkBytes.ToJsonObject());
            return obj;
        }

        public static Char ToNoteCharacter(this JObject obj)
        {
            Char chr = new Char();
            chr.CanvasSize = obj.GetJObject("CanvasSize").ToSize();
            chr.InkBytes = obj.GetJValue("InkBytes").ToBinary();
            JArray arr = obj.GetJArray("Strokes");
            foreach (var item in arr)
            {
                chr.Strokes.Add((item as JObject).ToStrokeData());
            }
            return chr;
        }

        public static Point ToPoint(this JObject obj)
        {
            double x = (double)obj.GetValue("X");
            double y = (double)obj.GetValue("Y");
            return new Point(x, y);
        }

        public static Size ToSize(this JObject obj)
        {
            double width = (double)obj.GetValue("Width");
            double height = (double)obj.GetValue("Height");
            return new Size(width, height);
        }

        public static Stroke ToStrokeData(this JObject obj)
        {
            Point start = obj.GetJObject("StartPoint").ToPoint();
            Size size = obj.GetJObject("CanvasSize").ToSize();
            Stroke data = new Stroke
            {
                StartPoint = start,
                CanvasSize = size,
            };
            JArray segments = obj.GetJArray("BezierSegments");
            foreach (var item in segments)
            {
                data.BezierSegments.Add((item as JObject).ToBezierSegment());
            }
            return data;
        }
    }

    public class Char
    {
        private static UTF8Encoding encoding = new UTF8Encoding();
        private List<Stroke> strokes = new List<Stroke>();

        public Brush CharBrush { get; set; }

        public double Thickness { get; set; }

        public Char(InkManager manager, Size canvasSize)
        {
            CanvasSize = canvasSize;
            if (manager.GetStrokes().Count == 0)
            {
                InkBytes = new byte[0];
            }
            else
            {
                LoadData(manager);
            }
        }

        internal Char()
        {
        }

        internal List<Stroke> Strokes { get { return strokes; } }

        internal Size CanvasSize { get; set; }

        internal byte[] InkBytes { get; set; }

        private async void LoadData(InkManager manager)
        {
            await LoadInkManagerData(manager);
            LoadStrokeData(manager);
        }

        private async Task LoadInkManagerData(InkManager manager)
        {
            InMemoryRandomAccessStream mem = new InMemoryRandomAccessStream();
            await manager.SaveAsync(mem);
            mem.Seek(0);
            byte[] bytes = new byte[mem.Size];
            var dataReader = new DataReader(mem);
            await dataReader.LoadAsync((uint)mem.Size);
            dataReader.ReadBytes(bytes);
            InkBytes = bytes;
        }

        private void LoadStrokeData(InkManager manager)
        {
            foreach (var stroke in manager.GetStrokes())
            {
                strokes.Add(new Stroke(stroke, CanvasSize));
            }
        }
    }

    internal class Stroke
    {
        private List<BezierSegment> segments = new List<BezierSegment>();
        private Windows.UI.Xaml.Shapes.Path path;

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

        public Windows.UI.Xaml.Shapes.Path AsPath(Brush brush, double thickness, double shiftX, double shiftY, double scaleX, double scaleY, double opacity = 1)
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
            path.Margin = new Windows.UI.Xaml.Thickness(shiftX, shiftY, 0, 0);
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
    }
}