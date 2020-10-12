using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using TVHeadEndM3USync.M3U.Model;

namespace TVHeadEndM3USync.M3U
{
    class Parser
    {
        const string HEADER = "#EXTM3U";
        public const string TVH_UUID_KEY = "TVH-UUID";
        public const string TVH_UUID = TVH_UUID_KEY + "=\"";

        public static List<Entry> GetEntries(string filePath)
        {
            using (var sr = new StreamReader(new FileStream(filePath, FileMode.Open, FileAccess.Read)))
            {
                var header = sr.ReadLine();
                if (header != HEADER)
                    throw new ApplicationException("expected #EXTM3U as file header");
                var lst = new List<Entry>();

                string lastXTINF = null;
                while (!sr.EndOfStream)
                {
                    var line = sr.ReadLine();
                    if (line.StartsWith("#EXTINF"))
                        lastXTINF = line;
                    else
                        if (lastXTINF != null)
                    {
                        var e = new Entry();
                        e.Url = line;
                        e.XTINF = lastXTINF;
                        var idx = e.XTINF.LastIndexOf(",");
                        if (idx > 0)
                        {
                            e.Name = e.XTINF.Substring(idx + 1).Trim();
                            e.TVH_UUID = GetTVH_UUID(e.XTINF);
                            lst.Add(e);
                        }
                        lastXTINF = null;
                    }
                }
                return lst;
            }
        }

        private static string GetTVH_UUID(string xTINF)
        {
            var idx = xTINF.IndexOf(TVH_UUID);
            if (idx > 0)
            {
                var uuid = xTINF.Substring(idx + TVH_UUID.Length).Split("\"")[0];
                return uuid;
            }
            return null;
        }

        internal static List<Tuple<string, string>> ParseTags(string tags)
        {
            var lst = new List<Tuple<string, string>>();
            while (tags.Contains("="))
            {
                var key = tags.Substring(0, tags.IndexOf("="));
                tags = tags.Remove(0, key.Length + 2); // extra one for "
                var value = tags.Substring(0, tags.IndexOf("\""));
                tags = tags.Remove(0, value.Length + 1).Trim();
                lst.Add(new Tuple<string, string>(key, value));
            }

            return lst;
        }

        internal static void WriteFile(string m3uFile, List<Entry> entries)
        {
            File.Copy(m3uFile, m3uFile + ".backup" + DateTime.Now.ToString("yyyyMMddHHmmss"));
            File.Delete(m3uFile);
            using (var sw = new StreamWriter(File.Create(m3uFile)))
            {
                sw.WriteLine(HEADER);
                foreach (var e in entries)
                {
                    var idx = e.XTINF.LastIndexOf(",");
                    var x = e.XTINF;
                    if (idx > 0)
                        x = x.Substring(0, idx);

                    if (!string.IsNullOrEmpty(e.TVH_UUID))
                    {
                        var currentUUID = GetTVH_UUID(e.XTINF);
                        if (currentUUID == null)
                        {
                            x = x + " " + TVH_UUID + e.TVH_UUID + "\"";
                        }
                        else
                        {
                            x = x.Replace(currentUUID, e.TVH_UUID);
                        }
                    }

                    x = x + "," + e.Name;
                    sw.WriteLine(x);
                    sw.WriteLine(e.Url);
                }
            }
        }
    }
}