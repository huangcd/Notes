using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Storage.Streams;
using Windows.UI.Input.Inking;
using Windows.UI.Xaml.Media;

namespace Notes.Common
{
    public class Char
    {
        private static UTF8Encoding encoding = new UTF8Encoding();
        private List<Stroke> strokes = new List<Stroke>();

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

        public Brush CharBrush { get; set; }

        public double Thickness { get; set; }
        internal Size CanvasSize { get; set; }

        internal byte[] InkBytes { get; set; }

        internal List<Stroke> Strokes
        {
            get
            {
                return strokes;
            }
        }

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

        public void Delete()
        {
            foreach (var stroke in strokes)
            {
                stroke.Delete();
            }
        }
    }
}