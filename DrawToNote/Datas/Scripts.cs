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
        internal const String FILE_SURFIX                 = ".js";
        private const double SCALE_CONSTANCE              = 1.2;
        private static readonly DateTimeFormatter datefmt = new DateTimeFormatter("shortdate");
        private readonly static object LockObject         = new object();
        private static readonly DateTimeFormatter timefmt = new DateTimeFormatter("shorttime");
        private static UTF8Encoding encoding              = new UTF8Encoding();

        // This must be the first field to store
        [JsonProperty("_createDate")]
        private DateTime _____createDate                  = DateTime.Now;

        [JsonProperty]
        private ObservableCollection<Character> _characters    = new ObservableCollection<Character>();

        [JsonProperty]
        private DateTime _lastModifyDate                  = DateTime.Now;

        [JsonProperty]
        private String _title                             = StringProvider.GetValue("NewScriptName");

        public Script()
        {
            Characters.CollectionChanged += characters_CollectionChanged;
        }

        public event NotifyCollectionChangedEventHandler ScriptChanged;

        [JsonIgnore]
        public ObservableCollection<Character> Characters
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
        public String CreateDateString
        {
            get
            {
                return datefmt.Format(CreateDate) + " " + timefmt.Format(CreateDate);
            }
        }

        [JsonIgnore]
        public String ModifyDateString
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

        [JsonIgnore]
        public string FileName
        {
            get
            {
                return CreateDate.ToString("MM_dd_yyyy_H-mm-ss") + FILE_SURFIX;
            }
        }

        public Character this[int index]
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

        public static bool operator !=(Script first, Script second)
        {
            return !(first == second);
        }

        public static bool operator <(Script first, Script second)
        {
            if (second == null)
            {
                return false;
            }
            if (first == null)
            {
                return true;
            }
            return first.LastModifyDate < second.LastModifyDate;
        }

        public static bool operator ==(Script first, Script second)
        {
            object obj = first as object;
            if (obj == null) return ((second as object) == null);
            return first.Equals(second);
        }

        public static bool operator >(Script first, Script second)
        {
            return first.LastModifyDate > second.LastModifyDate;
        }

        public void Add(Character character)
        {
            Characters.Add(character);
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
                MetroEventSource.Instance.Info(String.Format("Trying to delete file {0}", FileName));
                StorageFolder folder = ApplicationData.Current.LocalFolder;
                var asyncTask = folder.GetFileAsync(FileName);
                var file = await asyncTask;
                if (asyncTask.Status == AsyncStatus.Completed)
                {
                    await file.DeleteAsync();
                }
            }
            catch (Exception e)
            {
                MetroEventSource.Instance.Info(String.Format("Failed to delete file {0} due to: {1}", FileName, e.Message));
            }
        }

        public override bool Equals(object obj)
        {
            Script objAsScript = obj as Script;
            if (obj == null)
            {
                return false;
            }
            return this.CreateDate == objAsScript.CreateDate;
        }

        public IEnumerator<Character> GetEnumerator()
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
            int times = 0;
            bool succeed = false;
            while (!succeed)
            {
                try
                {
                    times++;
                    StorageFolder folder = ApplicationData.Current.LocalFolder;
                    MetroEventSource.Instance.Info(String.Format("Trying to save script {0}", this.Title));
                    succeed = await SaveAsync(folder);
                }
                catch (Exception ex)
                {
                    MetroEventSource.Instance.Error(String.Format("Failed to save script \"{0}\" due to Exception: {1}", this.Title, ex.Message));
                }
            }
            MetroEventSource.Instance.Info(String.Format("Succeed in saving script {0}", this.Title));
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
            StorageFile targetFile = await folder.CreateFileAsync(FileName, CreationCollisionOption.OpenIfExists);
            BasicProperties targetFileProperties = await targetFile.GetBasicPropertiesAsync();
            // New file, write directly
            if (targetFileProperties.Size == 0)
            {
                await FileIO.WriteTextAsync(targetFile, await contentTask);
                return true;
            }
            // Old file, create a backup file to write and then replace the origin file
            else
            {
                String backupFileName = FileName + ".bak";
                StorageFile backupFile = await folder.CreateFileAsync(backupFileName, CreationCollisionOption.ReplaceExisting);

                await FileIO.WriteTextAsync(backupFile, await contentTask);
                BasicProperties properties = await backupFile.GetBasicPropertiesAsync();
                // write successful, replace the origin file
                if (properties.Size > 0)
                {
                    try
                    {
                        await backupFile.MoveAndReplaceAsync(targetFile);
                        return true;
                    }
                    catch (Exception ex)
                    {
                        MetroEventSource.Instance.Error(String.Format(
                            "Something really bad happend while moving {0} to {1}: {2}",
                            backupFileName,
                            FileName,
                            ex.Message));
                        return false;
                    }
                }
                return false;
            }
        }
    }
}