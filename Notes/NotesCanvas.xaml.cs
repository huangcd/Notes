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
        readonly DependencyProperty CharacterHeightProperty =
            DependencyProperty.Register("CharacterHeight", typeof(int), typeof(NotesCanvas), new PropertyMetadata(6));
        readonly DependencyProperty CharacterWidthProperty =
             DependencyProperty.Register("CharacterWidth", typeof(int), typeof(NotesCanvas), new PropertyMetadata(6));

        public int CharacterHeight
        {
            get
            {
                return (int)GetValue(CharacterHeightProperty);
            }
            set
            {
                SetValue(CharacterHeightProperty, value);
            }
        }
        public int CharacterWidth
        {
            get
            {
                return (int)GetValue(CharacterWidthProperty);
            }
            set
            {
                SetValue(CharacterWidthProperty, value);
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
            FrameworkElement elem = await noteContent[index].AsFrameworkElementAsync(CharacterWidth, CharacterHeight);
            elem.Margin = new Thickness(5 + (index % CharacterWidth) * CharacterWidth, 5 + (index / CharacterWidth) * CharacterHeight, 0, 0);
            Children.Add(elem);
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

        public void RePaintAll()
        {
            Children.Clear();
            Task[] tasks = new Task[CharacterCount];
            for (int i = 0; i < CharacterCount; i++)
            {
                tasks[i] = RenderCharacter(i);
            }
            Task.WaitAll(tasks);
        }
    }
}