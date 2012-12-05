using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DrawToNote.Common
{
    public sealed class MetroEventSource : EventSource
    {
        private static MetroEventSource instance = new MetroEventSource();
        public static MetroEventSource Instance
        {
            get
            {
                return instance;
            }
        }

        [Event(0, Level = EventLevel.LogAlways)]
        public void LogAlways(string message)
        {
            this.WriteEvent(0, message);
        }

        [Event(1, Level = EventLevel.Verbose)]
        public void Debug(string message)
        {
            this.WriteEvent(1, message);
        }

        [Event(2, Level = EventLevel.Informational)]
        public void Info(string message)
        {
            this.WriteEvent(2, message);
        }

        [Event(3, Level = EventLevel.Warning)]
        public void Warn(string message)
        {
            this.WriteEvent(3, message);
        }

        [Event(4, Level = EventLevel.Error)]
        public void Error(string message)
        {
            this.WriteEvent(4, message);
        }

        [Event(5, Level = EventLevel.Critical)]
        public void Critical(string message)
        {
            this.WriteEvent(5, message);
        }

    }
}
