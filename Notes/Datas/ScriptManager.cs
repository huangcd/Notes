using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Storage;
using Windows.UI;
using Windows.UI.Input;
using Windows.UI.Input.Inking;
using System;
using Windows.Storage.Streams;
using System.Linq;
using Windows.UI.Popups;

namespace DrawToNote.Datas
{
    public class ScriptManager : IEnumerable<Script>
    {
        private static readonly ScriptManager instance = new ScriptManager();
        private Script _current;
        private InkManager _inkManager = new InkManager();
        private ObservableCollection<Script> scripts = new ObservableCollection<Script>();
        private ObservableCollection<Script> recentScripts = new ObservableCollection<Script>();
        StorageFolder localFolder = ApplicationData.Current.LocalFolder;

        private ScriptManager()
        {
            CreateScript();
        }

        public static ScriptManager Instance
        {
            get
            {
                return instance;
            }
        }

        public Script CurrentScript
        {
            get
            {
                return _current;
            }
        }

        public Script this[int index]
        {
            get
            {
                return scripts[index];
            }
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            foreach (var script in scripts)
            {
                yield return script;
            }
        }

        /// <summary>
        /// 清空当前缓存的笔画
        /// </summary>
        public void ClearInkStrokes()
        {
            foreach (var stroke in _inkManager.GetStrokes())
            {
                stroke.Selected = true;
            }
            _inkManager.DeleteSelected();
        }

        /// <summary>
        /// 设置InkManager笔画的形状
        /// </summary>
        /// <param name="color"></param>
        /// <param name="size"></param>
        /// <param name="fitToCurve"></param>
        /// <param name="ignorePressure"></param>
        /// <param name="penTip"></param>
        public void ConfigInkDrawingAttributes(
            Color color,
            Size size,
            bool fitToCurve = true,
            bool ignorePressure = false,
            PenTipShape penTip = PenTipShape.Circle)
        {
            var attributes = new InkDrawingAttributes
            {
                Color = color,
                Size = size,
                FitToCurve = fitToCurve,
                IgnorePressure = ignorePressure,
                PenTip = penTip
            };
            _inkManager.SetDefaultDrawingAttributes(attributes);
        }

        /// <summary>
        ///
        /// </summary>
        /// <returns></returns>
        public List<Windows.UI.Xaml.Shapes.Path> ConfirmCharacter(Size charSize, Size noteSize, Size canvasSize)
        {
            var list = new List<Windows.UI.Xaml.Shapes.Path>();
            Char chr = new Char(_inkManager, canvasSize);
            return CurrentScript.AddAndRender(chr, charSize, noteSize);
        }

        public void CreateScript()
        {
            if (_current != null)
            {
                _current.SaveAsync(ApplicationData.Current.LocalFolder);
            }
            _current = new Script();
            _current.DefaultCharColor = new Color
            {
                R = 0,
                G = 0,
                B = 166,
                A = 255,
            };
            _current.DefaultThickness = 8.0;
            scripts.Add(_current);
        }

        public IEnumerator<Script> GetEnumerator()
        {
            foreach (var script in scripts)
            {
                yield return script;
            }
        }

        #region event handler
        public IReadOnlyList<InkStroke> GetStrokes()
        {
            return _inkManager.GetStrokes();
        }

        public void ProcessPointerDown(PointerPoint pointerPoint)
        {
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

        internal void RepaintAll(Size noteSize, Size charSize)
        {
            CurrentScript.RepaintAll(noteSize: noteSize, charSize: charSize);
        }
        #endregion

        public async Task<ObservableCollection<Script>> LoadScriptsAsync()
        {
            var files = await ApplicationData.Current.LocalFolder.GetFilesAsync();
            foreach (var file in files)
            {
                await new MessageDialog(file.Path).ShowAsync();
            }
            var tasks = files.Select(file => Task.Run(
                async () => {
                    IBuffer buffer = await FileIO.ReadBufferAsync(file);
                    Script script = new Script();
                    script.LoadAsync(buffer);
                    scripts.Add(script);
                }));
            var results = Task.WhenAll(tasks);
            return scripts;
        }
    }
}