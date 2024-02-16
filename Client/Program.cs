using System;
using System.Management;
using System.Net;
using System.Net.Http;

namespace Client {
    internal class Program {
        internal class Motherboard {
            public string manufacturer;
            public string name;
            public Motherboard(string manufacturer, string name) {
                this.manufacturer = manufacturer;
                this.name = name;
            }
        }

        static Motherboard GetMotherboard() {
            ManagementObjectSearcher searcher = new ManagementObjectSearcher("select * from " + "Win32_BaseBoard");
            string manufacturer = "";
            string mobo = "";
            foreach (ManagementObject share in searcher.Get()) {
                foreach (PropertyData prop in share.Properties) {
                    if (prop.Name.Equals("Product")) {
                        mobo = prop.Value.ToString();
                    } else if (prop.Name.Equals("Manufacturer")) {
                        //BaseBoard Manufacturer	Micro-Star International Co., Ltd.
                        manufacturer = prop.Value.ToString();
                    }
                }
            }
            return new Motherboard(manufacturer, mobo);
        }


        static void Main(string[] args) {
            // client sends motherboard info to softwarerepo server and gets back a bios
            Motherboard motherboard = GetMotherboard();
            HttpClient httpClient = new HttpClient();
            string uri = $"ip/api/?manufacturer={motherboard.manufacturer}&motherboard={motherboard.name}";
            httpClient.GetStreamAsync(uri);
            // save stream to file (USB)
        }
    }
}
