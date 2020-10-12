using System;
using System.IO;
using System.Linq;
using TVHeadEndM3USync.TVHeadEnd;

namespace TVHeadEndM3USync
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length == 1)
            {
                M3U.M3UCleaner.CleanupM3UFile(args[0]);
                return;
            }

            if (args.Length < 3)
            {
                Console.WriteLine("Missing Parameters");
                Console.WriteLine("1. TVHeadEnd Url");
                Console.WriteLine("2. M3U File Path");
                Console.WriteLine("3. TVHeadEnd Network Name");
                Console.WriteLine("4. Username (Optional , depends on if set on TVH)");
                Console.WriteLine("5. Password (Optional , depends on if set on TVH)");
                Environment.Exit(1);
            }
            var url = args[0];
            var m3uFile = args[1];
            var networkNameToSync = args[2];
            var username = args.Length > 3 ? args[3] : string.Empty;
            var password = args.Length > 4 ? args[4] : string.Empty;

            if (!File.Exists(m3uFile))
            {
                Console.WriteLine($"M3U File ({m3uFile}) not found");
                Environment.Exit(1);
            }
            try
            {
                var uri = new Uri(url);
            }
            catch
            {
                Console.WriteLine($"Invalid Url ({url})");
                Environment.Exit(1);
            }

            var m3uEntries = M3U.Parser.GetEntries(m3uFile);

            var cli = new TVHClient(url) { Username = username, Password = password };

            var currentNetwork = GetNetwork(networkNameToSync, cli);


            // scan existings muxes and update them according to m3u file info

            var muxes = cli.GetMuxes();
            var networkMuxes = muxes.Where(x => x.NetworkUUID == currentNetwork.UUID);
            foreach (var mux in networkMuxes)
            {
                var match = m3uEntries.FirstOrDefault(x => x.TVH_UUID == mux.UUID);
                if (match != null)
                {
                    var needsUpdate = false;
                    if (mux.Url != match.Url)
                    {
                        Console.WriteLine("mux {0} url changed from {1} to {2}", mux.Name, mux.Url, match.Url);
                        mux.Url = match.Url;
                        needsUpdate = true;
                    }
                    if (mux.Name != match.Name)
                    {
                        Console.WriteLine("mux name changed from {0} to {1}", mux.Name, match.Name);
                        mux.Name = match.Name;
                        needsUpdate = true;
                    }
                    if (needsUpdate)
                        cli.UpdateMux(mux);
                }
            }

            // m3u update if needed , only update mux uuid tag on the correct enter for future sync , normally should happen only on first run for the entry

            var updateM3UFile = false;

            foreach (var e in m3uEntries)
            {
                var currentMux = networkMuxes.FirstOrDefault(x => x.Url == e.Url);
                if (currentMux == null)
                {
                    Console.WriteLine("Creating new mux with url {0} , name {1}", e.Url, e.Name);
                    var uuid = cli.AddMux(currentNetwork, e);
                    e.TVH_UUID = uuid;
                    updateM3UFile = true;
                }
                else
                {
                    if (e.Name != currentMux.Name)
                    {
                        e.TVH_UUID = currentMux.UUID;
                        Console.WriteLine("mux name changed from {0} to {1}, updateing m3u to mux uuid = {2}", currentMux.Name, e.Name, e.TVH_UUID);
                        currentMux.Name = e.Name;
                        cli.UpdateMux(currentMux);
                        updateM3UFile = true;
                    }
                }
            }
            Console.WriteLine("Finished analyzing {0} M3U entries.", m3uEntries.Count);
            if (updateM3UFile)
            {
                Console.Write("Updating M3U file... ");
                M3U.Parser.WriteFile(m3uFile, m3uEntries);
                Console.WriteLine("Done.");
            }
            else
                Console.WriteLine("No M3U file update needed.");

            // find muxes for the network which have urls not found on m3u file , maybe add parameter for this , not all users will want this , if user manually add mux it will beremoved , user should add only from m3u file
            // should only happen if user removed entry from m3u

            muxes = cli.GetMuxes().Where(x => x.NetworkUUID == currentNetwork.UUID).ToList();
            if (muxes.Count > 0)
            {
                int counter = 0;
                foreach (var mux in muxes)
                {
                    if (m3uEntries.All(x => x.Url != mux.Url))
                    {
                        if (counter == 0)
                            Console.WriteLine("Deleting old muxes from network {0} ...", currentNetwork.Name);
                        Console.WriteLine("Deleting old mux {0} , url {1}", mux.Name, mux.Url);
                        cli.DeleteMux(mux);
                        counter++;
                    }
                }
                if (counter == 0)
                    Console.WriteLine("No muxes for deletion.");
                else
                    Console.WriteLine("Finished deleting old muxes.");
            }
        }        

        private static TVHeadEnd.Model.Network GetNetwork(string networkNameToSync, TVHClient cli)
        {
            var networks = cli.GetNetworks();
            var workNetwork = networks.FirstOrDefault(x => x.Name == networkNameToSync);
            if (workNetwork == null)
            {
                cli.CreateNewIPTVNework(networkNameToSync);
                networks = cli.GetNetworks();
                workNetwork = networks.First(x => x.Name == networkNameToSync);
            }

            return workNetwork;
        }
    }
}