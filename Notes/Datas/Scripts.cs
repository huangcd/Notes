using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Windows.Foundation;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI;
using Windows.UI.Popups;
using Windows.UI.Xaml.Media;

namespace DrawToNote.Datas
{
    public class Script
    {
        private const String FileSufix = ".js";
        private const double ScaleConstance = 1.2;

        [JsonProperty]
        internal ObservableCollection<Char> characters = new ObservableCollection<Char>();

        private static UTF8Encoding encoding = new UTF8Encoding();
        public Script()
        {
            CreateDate = DateTime.Now;
            Title = "New Script";
        }

        public int Count { get { return characters.Count; } }

        [JsonProperty]
        public Color DefaultCharColor { get; set; }

        [JsonProperty]
        public double DefaultThickness { get; set; }

        [JsonProperty]
        internal DateTime CreateDate { get; set; }

        [JsonProperty]
        public String Title { get; set; }

        public Char this[int index]
        {
            get
            {
                return characters[index];
            }
        }

        public void Add(Char chr)
        {
            characters.Add(chr);
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

        public void RemoveLast()
        {
            characters[characters.Count - 1].Delete();
            characters.RemoveAt(characters.Count - 1);
        }

        /// <summary>
        /// Remove all characters from this script
        /// </summary>
        public void Clear()
        {
            foreach (var chr in characters)
            {
                chr.Delete();
            }
            characters.Clear();
        }

        public IEnumerator<Char> GetEnumerator()
        {
            foreach (var chr in characters)
            {
                yield return chr;
            }
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

        public void LoadAsync(IBuffer buffer)
        {
            byte[] bytes = buffer.ToArray();
            String content = encoding.GetString(bytes, 0, bytes.Length);
            JObject obj = (JObject)JsonConvert.DeserializeObject(content);
            this.LoadFromJObject(obj);
        }

        public async void SaveAsync(StorageFolder folder)
        {
            String fileName = CreateDate.ToString("MM_dd_yyyy_H-mm-ss") + FileSufix;
            StorageFile file = await folder.CreateFileAsync(fileName, CreationCollisionOption.ReplaceExisting);
            await FileIO.WriteTextAsync(file, JsonConvert.SerializeObject(this));
        }

        public async void SaveAsync(IOutputStream output)
        {
            JObject obj = this.ToJsonObject();
            byte[] bytes = encoding.GetBytes(obj.ToString());
            await output.WriteAsync(bytes.AsBuffer());
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
    }
}