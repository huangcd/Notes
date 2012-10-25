using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage.Streams;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.UI.Input.Inking;
using Windows.UI.Xaml.Shapes;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Media;
using Windows.UI.Popups;

namespace Notes.Common
{
    public class Note
    {
        private List<InkData> characters = new List<InkData>();

        public int Count
        {
            get { return characters.Count; }
        }

        public InkData this[int index]
        {
            get
            {
                if (index < 0)
                {
                    return characters[characters.Count + index];
                }
                return characters[index];
            }
        }

        public async Task<bool> AddCharacterAsync(InkManager manager)
        {
            if (manager.GetStrokes().Count == 0)
            {
                return false;
            }
            try
            {
                InMemoryRandomAccessStream mem = new InMemoryRandomAccessStream();
                await manager.SaveAsync(mem);
                mem.Seek(0);
                byte[] bytes = new byte[mem.Size];
                var dataReader = new DataReader(mem.GetInputStreamAt(0));
                var operation = await dataReader.LoadAsync((uint)mem.Size);
                dataReader.ReadBytes(bytes);
                InkData data = new InkData { Data = bytes };
                characters.Add(data);
                return true;
            }
            catch (Exception e)
            {
                new MessageDialog(e.Message).ShowAsync();
                return false;
            }
        }

        public InkData RemoveLastCharacter()
        {
            if (characters.Count == 0)
            {
                return null;
            }
            else
            {
                InkData data = characters[characters.Count - 1];
                characters.RemoveAt(characters.Count - 1);
                return data;
            }
        }

        public class InkData
        {
            private InMemoryRandomAccessStream mem;

            public byte[] Data { get; set; }

            public async Task<IRandomAccessStream> AsStreamAsync()
            {
                mem = null;
                mem = new InMemoryRandomAccessStream();
                await mem.WriteAsync(Data.AsBuffer());
                mem.Seek(0);
                return mem;
            }

            public async Task<Rectangle> AsRectangleAsync(double width, double height)
            {
                var stream = await AsStreamAsync();
                BitmapImage img = new BitmapImage();
                img.SetSource(mem);
                Rectangle rect = new Rectangle
                {
                    Width = width,
                    Height = height,
                    Fill = new ImageBrush { ImageSource = img }
                };
                return rect;
            }
        }
    }
}
