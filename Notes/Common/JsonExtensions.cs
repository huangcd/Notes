using System;
using Newtonsoft.Json.Linq;
using Windows.Foundation;
using Windows.UI.Xaml.Media;

namespace Notes.Common
{
    internal static class JsonExtensions
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
}