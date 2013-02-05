using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using DrawToNote.Common;
using DrawToNote.Pages;
using Windows.Foundation;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI;
using Windows.UI.Input;
using Windows.UI.Input.Inking;

namespace DrawToNote.Datas
{
    public class ScriptManager : BindableBase, IEnumerable<Script>
    {
        #region Getter, Setter

        private static readonly ScriptManager _instance = new ScriptManager();
        private static int RecentScriptLimit = 3;
        private Script _current;
        private InkManager _inkManager = new InkManager();
        private ObservableCollection<Script> _recentScripts = new ObservableCollection<Script>();
        private ObservableCollection<Script> _scripts = new ObservableCollection<Script>();

        private ScriptManager()
        {
            Scripts.CollectionChanged += scripts_CollectionChanged;
        }

        public static ScriptManager Instance
        {
            get
            {
                return _instance;
            }
        }

        public Script CurrentScript
        {
            get
            {
                return _current;
            }
            set
            {
                SetProperty(ref _current, value);
            }
        }

        public ObservableCollection<Script> RecentScripts
        {
            get
            {
                return _recentScripts;
            }
        }

        public ObservableCollection<Script> Scripts
        {
            get
            {
                return _scripts;
            }
        }

        public Script this[int index]
        {
            get
            {
                return Scripts[index];
            }
        }

        public int Count
        {
            get
            {
                return Scripts.Count;
            }
        }

        #endregion Getter, Setter

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            foreach (var script in Scripts)
            {
                yield return script;
            }
        }

        public void Add(Script script)
        {
            lock (Scripts)
            {
                var index = 0;
                var size = Scripts.Count;
                for (index = 0; index < size; index++)
                {
                    if (Scripts[index] == script)
                    {
                        // Already exists
                        return;
                    }
                    if (Scripts[index] < script)
                    {
                        break;
                    }
                }
                Scripts.Insert(index, script);
                script.PropertyChanged += script_PropertyChanged;
            }
        }

        private void script_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            //if (e.PropertyName == "LastModifyDate")
            //{
            //    Script script = sender as Script;
            //    Remove(script);
            //    Add(script);
            //}
        }

        public async void Remove(Script script)
        {
            Scripts.Remove(script);
            script.PropertyChanged -= script_PropertyChanged;
            await script.DeleteFile();
        }

        public void ClearInkStrokes()
        {
            foreach (var stroke in _inkManager.GetStrokes())
            {
                stroke.Selected = true;
            }
            _inkManager.DeleteSelected();
        }

        /// <summary>
        ///
        /// </summary>
        /// <returns></returns>
        public void ConfirmCharacter(Size canvasSize, List<Stroke> strokes)
        {
            Character chr = new Character(strokes, canvasSize);
            CurrentScript.Add(chr);
        }

        public Script CreateScript()
        {
            if (CurrentScript != null)
            {
                CurrentScript.Save();
            }
            Script script = new Script();
            Add(script);
            return script;
        }

        public IEnumerator<Script> GetEnumerator()
        {
            foreach (var script in Scripts)
            {
                yield return script;
            }
        }

        public async Task LoadScriptsAsync()
        {
            var files = await ApplicationData.Current.LocalFolder.GetFilesAsync();
            foreach (var file in files)
            {
                try
                {
                    if (!file.Name.EndsWith(Script.FILE_SURFIX))
                    {
                        continue;
                    }
                    IBuffer buffer = await FileIO.ReadBufferAsync(file);
                    Script script = Script.LoadAsync(buffer);
                    Add(script);
                }
                catch (Exception e)
                {
                    MetroEventSource.Instance.Error(String.Format("Failed to load script from file {0} due to: {1}", file.Name, e.Message));
                }
            }
        }

        private void scripts_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    if (e.NewStartingIndex < RecentScriptLimit)
                    {
                        RecentScripts.Insert(e.NewStartingIndex, Scripts[e.NewStartingIndex]);
                        if (RecentScripts.Count > RecentScriptLimit)
                        {
                            RecentScripts.RemoveAt(RecentScriptLimit);
                        }
                    }
                    break;

                case NotifyCollectionChangedAction.Move:
                    if (e.OldStartingIndex < RecentScriptLimit && e.NewStartingIndex < RecentScriptLimit)
                    {
                        RecentScripts.Move(e.OldStartingIndex, e.NewStartingIndex);
                    }
                    else if (e.OldStartingIndex < RecentScriptLimit)
                    {
                        RecentScripts.RemoveAt(e.OldStartingIndex);
                        RecentScripts.Add(Scripts[RecentScriptLimit - 1]);
                    }
                    else if (e.NewStartingIndex < RecentScriptLimit)
                    {
                        RecentScripts.Insert(e.NewStartingIndex, Scripts[e.NewStartingIndex]);
                        RecentScripts.RemoveAt(RecentScriptLimit);
                    }
                    break;

                case NotifyCollectionChangedAction.Remove:
                    if (e.OldStartingIndex < RecentScriptLimit)
                    {
                        RecentScripts.RemoveAt(e.OldStartingIndex);
                        if (Scripts.Count >= RecentScriptLimit)
                        {
                            RecentScripts.Add(Scripts[RecentScriptLimit - 1]);
                        }
                    }
                    break;

                case NotifyCollectionChangedAction.Replace:
                    if (e.OldStartingIndex < RecentScriptLimit)
                    {
                        RecentScripts[e.OldStartingIndex] = Scripts[e.OldStartingIndex];
                    }
                    break;

                case NotifyCollectionChangedAction.Reset:
                    RecentScripts.Clear();
                    while (RecentScripts.Count < Scripts.Count && RecentScripts.Count < RecentScriptLimit)
                    {
                        RecentScripts.Add(Scripts[RecentScripts.Count]);
                    }
                    break;
            }
        }

        #region event handler

        public IReadOnlyList<InkStroke> Strokes
        {
            get
            {
                return _inkManager.GetStrokes();
            }
        }

        public void ProcessPointerDown(PointerPoint pointerPoint)
        {
            // TODO: 可能出现"平板电脑墨迹错误代码"之类的错误
            _inkManager.ProcessPointerDown(pointerPoint);
        }

        public Rect ProcessPointerUp(PointerPoint pointerPoint)
        {
            var result = _inkManager.ProcessPointerUp(pointerPoint);
            return result;
        }

        public object ProcessPointerUpdate(PointerPoint pointerPoint)
        {
            return _inkManager.ProcessPointerUpdate(pointerPoint);
        }

        #endregion event handler
    }
}