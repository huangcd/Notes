using System;
using System.Collections.ObjectModel;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Windows.Foundation;
using Windows.Storage.Streams;
using Windows.UI.Input.Inking;

namespace DrawToNote.Datas
{
    public class Char
    {
        private static UTF8Encoding encoding = new UTF8Encoding();

        [JsonProperty]
        private ObservableCollection<Stroke> strokes = new ObservableCollection<Stroke>();

        public Char(InkManager manager, Size canvasSize)
        {
            CanvasSize = canvasSize;
            if (manager.GetStrokes().Count == 0)
            {
                //InkBytes = new byte[0];
            }
            else
            {
                LoadData(manager);
            }
        }

        public Char()
        {
        }

        [JsonProperty]
        internal Size CanvasSize { get; set; }

        //[JsonProperty]
        //internal byte[] InkBytes { get; set; }

        internal ObservableCollection<Stroke> Strokes
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
            //InkBytes = bytes;
        }

        private void LoadStrokeData(InkManager manager)
        {
            foreach (var stroke in manager.GetStrokes())
            {
                strokes.Add(new Stroke(stroke));
            }
        }
    }
}