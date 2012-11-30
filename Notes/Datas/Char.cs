using System;
using System.Collections.Generic;
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

        public Char(IEnumerable<Stroke> strokes, Size canvasSize)
        {
            CanvasSize = canvasSize;
            this.strokes = new ObservableCollection<Stroke>(strokes);
        }

        public Char()
        {
        }

        [JsonProperty]
        internal Size CanvasSize { get; set; }

        internal ObservableCollection<Stroke> Strokes
        {
            get
            {
                return strokes;
            }
        }
    }
}