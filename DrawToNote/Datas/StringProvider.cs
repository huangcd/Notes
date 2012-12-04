using System;
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