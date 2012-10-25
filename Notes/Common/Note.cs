using System;
using System.Collections.Generic;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Storage.Streams;
using Windows.UI.Input.Inking;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Shapes;
using System.Collections;

namespace Notes.Common
{
    public class Note : IEnumerable<Note.InkData>
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

        IEnumerator IEnumerable.GetEnumerator()
        {
            foreach (var ink in characters)
            {
                yield return ink;
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
                InkData data = new InkData();
                mem.Seek(0);
                await data.LoadAsync(mem);
                characters.Add(data);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public void Clear()
        {
            characters.Clear();
        }

        public IEnumerator<Note.InkData> GetEnumerator()
        {
            foreach (var ink in characters)
            {
                yield return ink;
            }
        }

        public async Task LoadAsync(IRandomAccessStream reader)
        {
            while (reader.Position != reader.Size)
            {
                InkData data = new InkData();
                await data.LoadAsync(reader);
                characters.Add(data);
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

        public async Task SaveAync(IOutputStream writer)
        {
            foreach (var ink in characters)
            {
                await ink.SaveAsync(writer);
            }
        }
        public class InkData
        {
            private InMemoryRandomAccessStream mem;

            public byte[] Data { get; set; }

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

            public async Task<IRandomAccessStream> AsStreamAsync()
            {
                mem = null;
                mem = new InMemoryRandomAccessStream();
                await mem.WriteAsync(Data.AsBuffer());
                mem.Seek(0);
                return mem;
            }
            public async Task LoadAsync(IRandomAccessStream reader)
            {
                byte[] bytes = new byte[reader.Size];
                var dataReader = new DataReader(reader);
                await dataReader.LoadAsync((uint)reader.Size);
                dataReader.ReadBytes(bytes);
                Data = bytes;
            }

            public async Task SaveAsync(IOutputStream writer)
            {
                await writer.WriteAsync(Data.AsBuffer());
            }
        }
    }
}