using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage.Streams;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.UI.Input.Inking;

namespace Notes.Common
{
    public class Note
    {
        public class InkData
        {
            private InMemoryRandomAccessStream mem;
            public byte[] Data { get; set; }

            public async Task<IInputStream> AsAsyncStream()
            {
                mem = null;
                mem = new InMemoryRandomAccessStream();
                await mem.WriteAsync(Data.AsBuffer());
                mem.Seek(0);
                return mem;
            }
        }

        private List<InkData> characters = new List<InkData>();

        public InkData this[int index]
        {
            get { return characters[index]; }
        }

        public int Size
        {
            get { return characters.Count; }
        }

        public async Task<bool> AddCharacterAsync(InkManager manager)
        {
            try
            {
                InMemoryRandomAccessStream mem = new InMemoryRandomAccessStream();
                await manager.SaveAsync(mem);
                mem.Seek(0);
                byte[] bytes = new byte[mem.Size];
                var dataReader = new DataReader(mem.GetInputStreamAt(0));
                dataReader.ReadBytes(bytes);
                InkData data = new InkData { Data = bytes };
                characters.Add(data);
                return true;
            }
            catch (Exception e)
            {
                return false;
            }
        }
    }
}
