using System.Collections.Generic;
using Windows.Foundation;
using Windows.Storage;
using Windows.UI;
using Windows.UI.Input;
using Windows.UI.Input.Inking;

namespace Notes.Common
{
    public class ScriptManager : IEnumerable<Script>
    {
        private Script _current;
        private InkManager _inkManager = new InkManager();
        private List<Script> scripts = new List<Script>(); 
        StorageFolder localFolder = ApplicationData.Current.LocalFolder;

        public ScriptManager()
        {
            CreateScript();
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
            _current = new Script();
            _current.DefaultCharColor = Colors.Black;
            _current.DefaultThickness = 4.0;
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
    }
}