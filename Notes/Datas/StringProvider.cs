using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.Resources;

namespace DrawToNote.Datas
{
    public static class StringProvider
    {
        private static ResourceLoader loader = new ResourceLoader();
        public static String GetValue(String Key)
        {
            return loader.GetString(Key);
        }
    }
}
