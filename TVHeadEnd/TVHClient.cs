using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using TVHeadEndM3USync.M3U.Model;
using TVHeadEndM3USync.TVHeadEnd.Model;

namespace TVHeadEndM3USync.TVHeadEnd
{
    enum AuthenticationType
    {
        Plain,
    }

    class GridRequestParameters
    {
        public int? Start { get; set; }
        public int? Limit { get; set; }
    }

    class TVHClient
    {
        public string Username { get; set; }
        public string Password { get; set; }
        public string _baseUrl { get; }

        public AuthenticationType AuthenticationType { get; set; }

        public TVHClient(string baseUrl)
        {
            _baseUrl = baseUrl;
        }

        HttpWebRequest NewWebRequest(string urlPath)
        {
            var wr = (HttpWebRequest)WebRequest.Create(Path.Combine(_baseUrl, urlPath));
            switch (AuthenticationType)
            {
                case AuthenticationType.Plain:
                    wr.Headers["Authorization"] = "Basic " + Convert.ToBase64String(Encoding.GetEncoding("ISO-8859-1").GetBytes(Username + ":" + Password)); ;
                    break;
                default:
                    throw new NotImplementedException();
            }
            return wr;
        }

        HttpWebRequest CreateRequest(string urlPath, GridRequestParameters p = null)
        {
            if (p == null)
                p = new GridRequestParameters();
            var wr = NewWebRequest(urlPath);
            wr.Method = "POST";
            var param = new List<string>();
            if (p.Start.HasValue)
                param.Add("start=" + p.Start.Value);
            if (p.Limit.HasValue)
                param.Add("limit=" + p.Limit.Value);
            var byteArray = Encoding.UTF8.GetBytes(string.Join("&",param));

            SetRequestStream(wr, byteArray);

            return wr;
        }

        public void CreateNewIPTVNework(string name)
        {
            var wr = NewWebRequest("api/mpegts/network/create");
            wr.Method = "POST";
            var param = "class=iptv_network&conf=";
            var obj = new Newtonsoft.Json.Linq.JObject();
            obj["enabled"] = false;
            obj["max_timeout"] = 10;
            obj["networkname"] = name;
            obj["pnetworkname"] = name;
            obj["scan_create"] = 0;
            obj["max_streams"] = 1;
            var byteArray = Encoding.UTF8.GetBytes(param + System.Web.HttpUtility.UrlEncode(obj.ToString()));

            SetRequestStream(wr, byteArray);

            wr.GetResponse();
        }

        private static void SetRequestStream(HttpWebRequest wr, byte[] byteArray)
        {
            wr.ContentType = "application/x-www-form-urlencoded";
            wr.ContentLength = byteArray.Length;
            var postStream = wr.GetRequestStream();
            postStream.Write(byteArray, 0, byteArray.Length);
            postStream.Close();
        }

        internal string AddMux(Network workNetwork, Entry e)
        {
            var wr = NewWebRequest("api/mpegts/network/mux_create");
            wr.Method = "POST";
            var param = "uuid=" + workNetwork.UUID + "&conf=";
            var obj = new Newtonsoft.Json.Linq.JObject();
            obj["Enabled"] = true;
            obj["epg"] = false;
            obj["iptv_url"] = e.Url;
            obj["iptv_muxname"] = e.Name;
            obj["scan_state"] = 0;
            var byteArray = Encoding.UTF8.GetBytes(param + System.Web.HttpUtility.UrlEncode(obj.ToString()));

            SetRequestStream(wr, byteArray);            

            var res = GetJSON(wr);
            return res.Value<string>("uuid");
        }

        internal void UpdateMux(Mux mux)
        {
            var wr = NewWebRequest("api/idnode/save");
            wr.Method = "POST";
            var param = "node=";
            var obj = new Newtonsoft.Json.Linq.JObject();
            obj["uuid"] = mux.UUID;
            obj["iptv_muxname"] = mux.Name;
            obj["iptv_url"] = mux.Url;            
            var byteArray = Encoding.UTF8.GetBytes(param + System.Web.HttpUtility.UrlEncode(obj.ToString()));

            SetRequestStream(wr, byteArray);

            wr.GetResponse();
        }

        internal void DeleteMux(Mux mux)
        {
            var wr = NewWebRequest("api/idnode/delete");
            wr.Method = "POST";
            var param = "uuid=";
            var obj = new Newtonsoft.Json.Linq.JArray();
            obj.Add(mux.UUID);            
            var byteArray = Encoding.UTF8.GetBytes(param + System.Web.HttpUtility.UrlEncode(obj.ToString()));

            SetRequestStream(wr, byteArray);

            wr.GetResponse();
        }

        Newtonsoft.Json.Linq.JObject GetJSON(HttpWebRequest wr)
        {
            var res = (HttpWebResponse)wr.GetResponse();
            using (var sr = new StreamReader(res.GetResponseStream(), Encoding.UTF8))
            {
                var obj = Newtonsoft.Json.Linq.JObject.Parse(sr.ReadToEnd());
                return obj;
            }
        }

        public List<Network> GetNetworks()
        {
            var lst = new List<Network>();
            var wr = CreateRequest("api/mpegts/network/grid");
            var obj = GetJSON(wr);
            foreach (var ent in obj["entries"])
            {
                var n = new Network();
                n.Name = ent.Value<string>("networkname");
                n.Enabled = ent.Value<bool>("enabled");
                n.UUID = ent.Value<string>("uuid");
                lst.Add(n);
            }

            return lst;
        }

        public List<Mux> GetMuxes()
        {
            var lst = new List<Mux>();
            var wr = CreateRequest("api/mpegts/mux/grid", new GridRequestParameters { Start = 0, Limit = int.MaxValue });
            var obj = GetJSON(wr);
            foreach (var ent in obj["entries"])
            {
                var m = new Mux();                
                m.Enabled = ent.Value<bool>("enabled");
                m.UUID = ent.Value<string>("uuid");
                m.NetworkName = ent.Value<string>("networkname");
                m.NetworkUUID = ent.Value<string>("network_uuid");
                m.Name = ent.Value<string>("name");
                m.Url = ent.Value<string>("iptv_url");
                lst.Add(m);
            }
            return lst;
        }        
    }
}
