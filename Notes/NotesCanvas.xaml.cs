using System;
using System.Collections.Generic;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Storage.Streams;
using Windows.UI.Input.Inking;
using Windows.UI.Xaml.Controls;
using Notes.Common;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Shapes;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Notes
{
    public sealed partial class NotesCanvas : Canvas
    {
        readonly DependencyProperty RowCountProperty =
            DependencyProperty.Register("RowCount", typeof(int), typeof(NotesCanvas), new PropertyMetadata(6));
        readonly DependencyProperty ColumnCountProperty =
             DependencyProperty.Register("ColumnCount", typeof(int), typeof(NotesCanvas), new PropertyMetadata(6));

        public int RowCount
        {
            get
            {
                return (int)GetValue(RowCountProperty);
            }
            set
            {
                SetValue(RowCountProperty, value);
            }
        }
        public int ColumnCount
        {
            get
            {
                return (int)GetValue(ColumnCountProperty);
            }
            set
            {
                SetValue(ColumnCountProperty, value);
            }
        }

        private Note noteContent = new Note();

        public NotesCanvas()
        {
            this.InitializeComponent();
        }

        public async Task AddCharacterAsync(InkManager manager)
        {
            await noteContent.AddCharacterAsync(manager);
            await RenderCharacter(noteContent.Count - 1);
        }

        private async Task RenderCharacter(int index)
        {
            var canvasWidth = RenderSize.Width;
            var canvasHeight = RenderSize.Height;
            var gridWidth = canvasWidth / ColumnCount;
            var gridHeight = canvasHeight / RowCount;
            Rectangle rect = await noteContent[index].AsRectangleAsync(gridWidth, gridHeight);
            rect.Margin = new Thickness(5 + (index % ColumnCount) * gridWidth, 5 + (index / ColumnCount) * gridHeight, 0, 0);
            Children.Add(rect);
        }

        public Note.InkData RemoveLastCharacter()
        {
            Children.RemoveAt(Children.Count - 1);
            return noteContent.RemoveLastCharacter();
        }

        public int CharacterCount
        {
            get { return noteContent.Count; }
        }

        public async Task RePaintAll()
        {
            Children.Clear();
            List<Task> tasks = new List<Task>();
            for (int i = 0; i < CharacterCount; i++)
            {
                tasks.Add(RenderCharacter(i));
            }
            foreach (var task in tasks)
            {
                await task;
            }
        }
    }
}