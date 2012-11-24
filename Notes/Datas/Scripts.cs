using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using DrawToNote.Common;
using Newtonsoft.Json;
using Windows.Foundation;
using Windows.Globalization.DateTimeFormatting;
using Windows.Storage;
using Windows.Storage.Streams;
using System.Reflection.Emit;
using System.Reflection.Context;
using Newtonsoft.Json.Linq;

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
                if (startMonitorModify)
                {
                    lock (LockObject)
                    {
                        Save();
                    }
                }
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
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine(e.Message);
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
            StorageFolder folder = ApplicationData.Current.LocalFolder;
            await SaveAsync(folder);
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

        private async Task SaveAsync(StorageFolder folder)
        {
            String fileName = CreateDate.ToString("MM_dd_yyyy_H-mm-ss") + FileSufix;
            StorageFile file = await folder.CreateFileAsync(fileName, CreationCollisionOption.ReplaceExisting);
            await FileIO.WriteTextAsync(file, JsonConvert.SerializeObject(this, Formatting.Indented));
        }
    }
}