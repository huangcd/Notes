using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Windows.Storage;
using Windows.UI;
using Windows.UI.Xaml.Media;

namespace DrawToNote.Datas
{
    public static class DefaultValue
    {
        private static ApplicationDataContainer settings = ApplicationData.Current.LocalSettings;
        private const String DefaultLineWidthKey = "DefaultLineWidth";
        private const String DefaultLineColorKey = "DefaultLineColor";

        public static Color DefaultLineColor
        {
            get
            {
                Object obj = settings.Values[DefaultLineColorKey];
                if (obj == null)
                {
                    settings.Values[DefaultLineColorKey] = JsonConvert.SerializeObject(Colors.Black);
                    return Colors.Black;
                }
                return JsonConvert.DeserializeObject<Color>(settings.Values[DefaultLineColorKey] as String);
            }
            set
            {
                settings.Values[DefaultLineColorKey] = JsonConvert.SerializeObject(value);
            }
        }

        public static double DefaultLineWidth
        {
            get
            {
                Object obj = settings.Values[DefaultLineWidthKey];
                if (obj == null)
                {
                    settings.Values[DefaultLineWidthKey] = 8.0;
                }
                return (double)settings.Values[DefaultLineWidthKey];
            }
            set
            {
                settings.Values[DefaultLineWidthKey] = value;
            }
        }
    }
}
