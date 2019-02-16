

namespace WebClient.Util
{
    public static class AssemblyHelper
    {
        public static string GetAssemblyVersion()
        {
            var version = System.Reflection.Assembly.GetEntryAssembly().GetName().Version;

            return version.ToString();
        }
    }
}