using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using DrawToNote.Common;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Windows.Foundation;
using Windows.Globalization.DateTimeFormatting;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.Storage.Streams;

namespace DrawToNote.Datas
{
    public class Script : BindableBase, IComparable<Script>
    {
        private const String FileSufix = ".js";
        private const double ScaleConstance = 1.2;
        private static readonly DateTimeFormatter datefmt = new DateTimeFormatter("shortdate");
        private readonly static object LockObject = new object();
        private static readonly DateTimeFormatter timefmt = new DateTimeFormatter("shorttime");
        private static UTF8Encoding encoding = new UTF8Encoding();

        [JsonIgnore]
        private bool startMonitorModify = false;

        // This must be the first field to store
        [JsonProperty("_createDate")]
        private DateTime _____createDate = DateTime.Now;

        [JsonProperty]
        private ObservableCollection<Char> _characters = new ObservableCollection<Char>();

        [JsonProperty]
        private DateTime _lastModifyDate = DateTime.Now;

        [JsonProperty]
        private String _title = StringProvider.GetValue("NewScriptName");

        public Script()
        {
            Characters.CollectionChanged += characters_CollectionChanged;
        }

        public event NotifyCollectionChangedEventHandler ScriptChanged;

        [JsonIgnore]
        public ObservableCollection<Char> Characters
        {
            get
            {
                return _characters;
            }
        }

        [JsonIgnore]
        public int Count
        {
            get
            {
                return Characters.Count;
            }
        }

        [JsonIgnore]
        public String CreateDateStr
        {
            get
            {
                return datefmt.Format(CreateDate) + " " + timefmt.Format(CreateDate);
            }
        }

        [JsonIgnore]
        public String ModifyDateStr
        {
            get
            {
                return datefmt.Format(LastModifyDate) + " " + timefmt.Format(LastModifyDate);
            }
        }

        [JsonIgnore]
        public String Title
        {
            get
            {
                return _title;
            }
            set
            {
                SetProperty(ref _title, value);
            }
        }

        [JsonIgnore]
        internal DateTime CreateDate
        {
            get
            {
                return _____createDate;
            }
            set
            {
                SetProperty(ref _____createDate, value);
            }
        }

        [JsonIgnore]
        internal DateTime LastModifyDate
        {
            get
            {
                return _lastModifyDate;
            }
            set
            {
                SetProperty(ref _lastModifyDate, value);
                //if (startMonitorModify)
                //{
                //    lock (LockObject)
                //    {
                //        Save();
                //    }
                //}
            }
        }

        public Char this[int index]
        {
            get
            {
                return Characters[index];
            }
        }

        public static Script LoadAsync(IBuffer buffer)
        {
            lock (LockObject)
            {
                byte[] bytes = buffer.ToArray();
                String content = encoding.GetString(bytes, 0, bytes.Length);
                return LoadAsync(content);
            }
        }

        public static Script LoadAsync(String content)
        {
            // OldData
            if (content.Contains("StartPoint"))
            {
                try
                {
                    content = HandlingOldData(content);
                }
                catch (Exception)
                {
                }
            }
            Script script = JsonConvert.DeserializeObject<Script>(content);
            script.startMonitorModify = true;
            return script;
        }

        private static string HandlingOldData(string content)
        {
            JObject obj = JObject.Parse(content);
            JArray chrArray = obj["_characters"] as JArray;
            foreach (var tok in chrArray)
            {
                JObject chr = tok as JObject;
                JArray strokeArray = chr["strokes"] as JArray;
                foreach (var stk in strokeArray)
                {
                    JObject stroke = stk as JObject;
                    stroke.Remove("StartPoint");
                    JArray segments = stroke["BezierSegments"] as JArray;
                    String segmentsStr = String.Join(",", segments.Select(ConvertBezierSegment));
                    stroke.Remove("BezierSegments");
                    stroke.Add("BezierSegmentsStr", new JValue(segmentsStr));
                }
            }
            return obj.ToString();
        }

        private static String ConvertBezierSegment(JToken segment)
        {
            return String.Join(",",
                (segment["Point1"]["X"] as JValue).Value, (segment["Point1"]["Y"] as JValue).Value,
                (segment["Point2"]["X"] as JValue).Value, (segment["Point3"]["Y"] as JValue).Value,
                (segment["Point2"]["X"] as JValue).Value, (segment["Point3"]["Y"] as JValue).Value);
        }

        public static bool operator !=(Script s1, Script s2)
        {
            return !(s1 == s2);
        }

        public static bool operator <(Script s1, Script s2)
        {
            return s1.LastModifyDate < s2.LastModifyDate;
        }

        public static bool operator ==(Script s1, Script s2)
        {
            object obj = s1 as object;
            if (obj == null) return ((s2 as object) == null);
            return s1.Equals(s2);
        }

        public static bool operator >(Script s1, Script s2)
        {
            return s1.LastModifyDate > s2.LastModifyDate;
        }

        public void Add(Char chr)
        {
            Characters.Add(chr);
        }

        /// <summary>
        /// Remove all characters from this script
        /// </summary>
        public void Clear()
        {
            Characters.Clear();
            LastModifyDate = DateTime.Now;
        }

        public int CompareTo(Script other)
        {
            return other._lastModifyDate.CompareTo(this._lastModifyDate);
        }

        public async Task DeleteFile()
        {
            try
            {
                StorageFolder folder = ApplicationData.Current.LocalFolder;
                String fileName = CreateDate.ToString("MM_dd_yyyy_H-mm-ss") + FileSufix;
                var asyncTask = folder.GetFileAsync(fileName);
                var file = await asyncTask;
                if (asyncTask.Status == AsyncStatus.Completed)
                {
                    await file.DeleteAsync();
                }
            }
            catch (Exception)
            {
            }
        }

        public override bool Equals(object obj)
        {
            if (!(obj is Script))
            {
                return false;
            }
            return this.CreateDate == (obj as Script).CreateDate;
        }

        public IEnumerator<Char> GetEnumerator()
        {
            foreach (var chr in Characters)
            {
                yield return chr;
            }
        }

        public override int GetHashCode()
        {
            return CreateDate.GetHashCode();
        }

        public void RemoveLast()
        {
            if (Characters.Count > 0)
            {
                Characters.RemoveAt(Characters.Count - 1);
                LastModifyDate = DateTime.Now;
            }
        }

        public async void Save()
        {
            await SaveAsync();
        }

        public async Task SaveAsync()
        {
            bool succeed = false;
            while (!succeed)
            {
                try
                {
                    StorageFolder folder = ApplicationData.Current.LocalFolder;
                    succeed = await SaveAsync(folder);
                }
                catch (Exception)
                {
                }
            }
        }

        private void characters_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            LastModifyDate = DateTime.Now;
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    break;

                case NotifyCollectionChangedAction.Move:
                    break;

                case NotifyCollectionChangedAction.Remove:
                    break;

                case NotifyCollectionChangedAction.Replace:
                    break;

                case NotifyCollectionChangedAction.Reset:
                    break;
            }
            var eventHandler = this.ScriptChanged;
            if (eventHandler != null)
            {
                eventHandler(this, e);
            }
        }

        private async Task<String> SerializeToStringAsync()
        {
            return JsonConvert.SerializeObject(this);
            //try
            //{
            //    String content = await JsonConvert.SerializeObjectAsync(this);
            //    return content;
            //}
            //catch (Exception)
            //{
            //    return JsonConvert.SerializeObject(this);
            //}
        }

        private async Task<bool> SaveAsync(StorageFolder folder)
        {
            Task<String> contentTask = SerializeToStringAsync();
            String fileName = CreateDate.ToString("MM_dd_yyyy_H-mm-ss") + FileSufix;
            StorageFile targetFile = await folder.CreateFileAsync(fileName, CreationCollisionOption.OpenIfExists);
            BasicProperties targetFileProperties = await targetFile.GetBasicPropertiesAsync();
            // New file, write directly
            if (targetFileProperties.Size == 0)
            {
                await FileIO.WriteTextAsync(targetFile, await contentTask);
                return true;
            }
            // Old file, create a backup file to write and then replace the origion file
            else
            {
                String backupFileName = fileName + ".bak";
                StorageFile backupFile = await folder.CreateFileAsync(backupFileName, CreationCollisionOption.ReplaceExisting);

                await FileIO.WriteTextAsync(backupFile, await contentTask);
                BasicProperties properties = await backupFile.GetBasicPropertiesAsync();
                // write successful, replace the origion file
                if (properties.Size > 0)
                {
                    try
                    {
                        await backupFile.MoveAndReplaceAsync(targetFile);
                        return true;
                    }
                    catch (Exception)
                    {
                        return false;
                    }
                }
                return false;
            }
        }
    }
}