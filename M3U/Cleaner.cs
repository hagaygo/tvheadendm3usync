using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using TVHeadEndM3USync.M3U.Model;
using System.Linq;
namespace TVHeadEndM3USync.M3U
{
    static class M3UCleaner
    {
        public static void CleanupM3UFile(string filename)
        {
            if (!File.Exists(filename))
            {
                Console.WriteLine($"M3U cleanup - File {filename} not found");
                return;
            }

            var requireSave = false;
            var entires = Parser.GetEntries(filename);
            foreach (var entry in entires)
            {
                var madeChanges = CleanupEntry(entry);
                if (madeChanges)
                    requireSave = true;
            }
            if (requireSave)
            {
                var backupFilename = filename;

                backupFilename = Path.Combine(Path.GetDirectoryName(filename), "cleanupBackup" + DateTime.Now.ToString("yyyyMMddhhmmss") + "_" + Path.GetFileName(filename));
                Console.WriteLine("Cleanup needed , saving backup file to {0}", backupFilename);
                File.Copy(filename, backupFilename);
                Parser.WriteFile(filename, entires);
                Console.WriteLine("File cleaned up.");
            }
            else
                Console.WriteLine("No cleanup needed.");
        }

        private static bool CleanupEntry(Entry entry)
        {
            var tags = entry.XTINF.Substring(entry.XTINF.IndexOf(" ") + 1);
            tags = tags.Substring(0, tags.IndexOf(","));
            var tagList = Parser.ParseTags(tags);
            if (tagList.Any(x => x.Item1 != Parser.TVH_UUID_KEY))
            {
                tagList = tagList.Where(x => x.Item1 == Parser.TVH_UUID_KEY).ToList();
                var prefix = entry.XTINF.Substring(0, entry.XTINF.IndexOf(" ") + 1);
                var suffix = entry.XTINF.Substring(entry.XTINF.IndexOf(","));
                entry.XTINF = prefix + GetTags(tagList) + suffix;
                return true;
            }
            return false;
        }

        private static string GetTags(List<Tuple<string, string>> tagList)
        {
            var sb = new StringBuilder();
            foreach (var t in tagList)
                sb.Append($"{t.Item1}=\"{t.Item2}\" ");
            return sb.ToString();
        }
    }
}
