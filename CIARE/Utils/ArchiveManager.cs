using System.IO;
using System.IO.Compression;

namespace CIARE.Utils
{
    public class ArchiveManager
    {
        /// <summary>
        /// Extract data from zip archive and ouput it in a directory with same file name as the package.
        /// </summary>
        /// <param name="zipFileName"></param>
        /// <param name="richTextBox"></param>
        public static void Extract(string zipFileName)
        {
            try
            {
                var foldername = zipFileName.Replace(".zip", "");
                if (!Directory.Exists(foldername))
                    ZipFile.ExtractToDirectory(zipFileName, foldername);
            }
            catch
            {
                // Ignore
            }
        }
    }
}
