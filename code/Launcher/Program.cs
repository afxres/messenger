using Mikodev.Logger;
using Mikodev.Network;
using System;
using System.Collections;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Xml;

namespace Launcher
{
    internal class Program
    {
        private static async Task Main(string[] args)
        {
            Log.Run(nameof(Launcher) + ".log");

            try
            {
                var xml = new XmlDocument();
                xml.Load(nameof(Launcher) + ".settings.xml");
                var lst = xml.SelectNodes("/settings/setting[@key]");
                var dic = ((IEnumerable)lst)
                    .Cast<XmlElement>()
                    .ToDictionary(r => r.SelectSingleNode("@key").Value, r => r.SelectSingleNode("@value").Value);
                var nam = dic["server-name"];
                var add = IPAddress.Parse(dic["listen-address"]);
                var max = int.Parse(dic["client-limits"]);
                var pot = int.Parse(dic["tcp-port"]);
                var bro = int.Parse(dic["udp-port"]);
                await LinkListener.Run(add, pot, bro, max, nam);
            }
            catch (Exception ex)
            {
                Log.Error(ex);
            }

            Log.Close();
        }
    }
}
