using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;

namespace DrawToNote.Common
{
    public sealed class StorageFileEventListener : EventListener
    {
        /// <summary>
        /// The format to be used by logging.
        /// </summary>
        private string m_Format = "{0:yyyy-MM-dd HH\\:mm\\:ss\\:ffff}\tType: {1}\tId: {2}\tMessage: '{3}'";

        /// <summary>
        /// Name of the current event listener
        /// </summary>
        private string m_Name;

        private SemaphoreSlim m_SemaphoreSlim = new SemaphoreSlim(1);

        /// <summary>
        /// Storage file to be used to write logs
        /// </summary>
        private StorageFile m_StorageFile = null;
        public StorageFileEventListener(string name)
        {
            this.m_Name = name;

            Debug.WriteLine("StorageFileEventListener for {0} has name {1}", GetHashCode(), name);

            AssignLocalFile();
        }

        protected override void OnEventSourceCreated(EventSource eventSource)
        {
            Debug.WriteLine("OnEventSourceCreated for Listener {0} - {1} got eventSource {2}", GetHashCode(), m_Name, eventSource.Name);
        }

        protected override void OnEventWritten(EventWrittenEventArgs eventData)
        {
            if (m_StorageFile == null)
            {
                return;
            }
            var lines = new List<string>();
            var newFormatedLine = string.Format(m_Format, DateTime.Now, eventData.Level, eventData.EventId, eventData.Payload[0]);
            Debug.WriteLine(newFormatedLine);
            lines.Add(newFormatedLine);
            WriteToFile(lines);
        }

        private async void AssignLocalFile()
        {
            m_StorageFile = await ApplicationData.Current.LocalFolder.CreateFileAsync(m_Name.Replace(" ", "_") + ".log",
                                                                                      CreationCollisionOption.OpenIfExists);
        }

        private async void WriteToFile(IEnumerable<string> lines)
        {
            await m_SemaphoreSlim.WaitAsync();

            await Task.Run(async () =>
            {
                try
                {
                    await FileIO.AppendLinesAsync(m_StorageFile, lines);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex);
                }
                finally
                {
                    m_SemaphoreSlim.Release();
                }
            });
        }
    }
}