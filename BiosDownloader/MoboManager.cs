using Newtonsoft.Json;

namespace BiosDownloader {
    internal class MoboManager {
        #pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public class Asus {
            public class EcCustomize {
                public bool status { get; set; }
                public int buttonStatusCode { get; set; }
                public string buttonStatus { get; set; }
                public string storeLink { get; set; }
            }
            public class EnabledItem {
                public int itemId { get; set; }
                public int count { get; set; }
            }
            public class Logo {
                public string name { get; set; }
                public string alt { get; set; }
                public string x2MediaUrl { get; set; }
                public string x1MediaUrl { get; set; }
                public string logoLink { get; set; }
            }
            public class Result {
                public int count { get; set; }
                public List<Sku> skus { get; set; }
                public List<EnabledItem> enabledItems { get; set; }
                public List<object> enabledGroups { get; set; }
            }
            public class Root {
                public Result result { get; set; }
                public int status { get; set; }
                public string message { get; set; }
            }
            public class Sku {
                public int id { get; set; }
                public string mktName { get; set; }
                public string name { get; set; }
                public int productId { get; set; }
                public string partNo { get; set; }
                public string externalId { get; set; }
                public string inch { get; set; }
                public string productImgUrl { get; set; }
                public string secondProductImgUrl { get; set; }
                public string skuLink { get; set; }
                public string wtbLink { get; set; }
                public bool isNew { get; set; }
                public int displayKeySpec { get; set; }
                public string ksp { get; set; }
                public List<object> keySpec { get; set; }
                public List<Logo> logo { get; set; }
                public EcCustomize ecCustomize { get; set; }
            }
        }

        public class Msi {
            public class GetProductList {
                public int id { get; set; }
                public string title { get; set; }
                public string subname { get; set; }
                public string link { get; set; }
                public string desc { get; set; }
                public string ean { get; set; }
                public string release { get; set; }
                public string product_line { get; set; }
                public string picture { get; set; }
                public List<object> color { get; set; }
                public string label { get; set; }
            }
            public class Result {
                public List<GetProductList> getProductList { get; set; }
                public List<string> buyNowCountry { get; set; }
                public int count { get; set; }
            }
            public class Root {
                public Status status { get; set; }
                public Result result { get; set; }
            }
            public class Status {
                public int code { get; set; }
                public string response { get; set; }
            }
        }
        #pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

        public enum Manufacturer {
            MSI,
            ASUS,
            UNKNOWN
        }

        private const string ASUS_MOBO_URL = "https://api-rog.asus.com/recent-data/api/v5/Filters/Results?count=1000&WebsiteId=1&LevelTagId=77";
        private const string MSI_MOBO_URL = "https://www.msi.com/api/v1/product/getProductList?product_line=mb&page_size=1000";

        private const string ASUS_JSON_PATH = @"asus.json";
        private const string MSI_JSON_PATH = @"msi.json";

        private Msi.Root msi;
        private Asus.Root asus;

        public MoboManager(bool forceUpdate = true) {
            if (forceUpdate || !File.Exists(MSI_JSON_PATH)) {
                Console.WriteLine("Fetching MSI motherboards from api.");
                msi = JsonConvert.DeserializeObject<Msi.Root>(GetJsonData(MSI_MOBO_URL).Result)!;

                File.WriteAllText(MSI_JSON_PATH, JsonConvert.SerializeObject(msi, Formatting.Indented));
            } else msi = JsonConvert.DeserializeObject<Msi.Root>(File.ReadAllText(MSI_JSON_PATH))!;

            if (forceUpdate || !File.Exists(ASUS_JSON_PATH)) {
                Console.WriteLine("Fetching Asus motherboards from api.");
                asus = JsonConvert.DeserializeObject<Asus.Root>(GetJsonData(ASUS_MOBO_URL).Result)!;
                File.WriteAllText(ASUS_JSON_PATH, JsonConvert.SerializeObject(asus, Formatting.Indented));
            } else asus = JsonConvert.DeserializeObject<Asus.Root>(File.ReadAllText(ASUS_JSON_PATH))!;
        }

        public Manufacturer GetManufacturer(string manufacturer) {
            manufacturer = manufacturer.ToLower();
            if (manufacturer.Contains("micro-star")) {
                return Manufacturer.MSI;
            } else if (manufacturer.Contains("asus")) {
                return Manufacturer.ASUS;
            } else {
                return Manufacturer.UNKNOWN;
            }
        }

        public string? GetLinkForMobo(Manufacturer manufacturer, string moboName) {
            moboName = moboName.ToLower();
            if (manufacturer == Manufacturer.MSI) {
                moboName = moboName.Split("(")[0];
                var info = GetMsiMoboInfo(moboName);
                if (info == null) {
                    Console.WriteLine("Info not found.");
                    return null;
                }
                return $"https://www.msi.com/Motherboard/{info.link}/support";
            } else if (manufacturer == Manufacturer.ASUS) {
                var info = GetAsusMoboInfo(moboName);
                if (info == null) {
                    Console.WriteLine("Info not found.");
                    return null;
                }
                return $"{info.skuLink}/helpdesk_bios/";
            } else {
                // Manufacturer not supported.
                Console.WriteLine("Manufacturer not supported.");
                return null;
            }
        }

        private Asus.Sku? GetAsusMoboInfo(string moboName) {
            //skuLink
            foreach (var entry in asus.result.skus) {
                if (entry.mktName.ToLower().Equals(moboName)) {
                    return entry;
                }
            }
            return null;
        }

        Msi.GetProductList? GetMsiMoboInfo(string moboName) {
            foreach (var entry in msi.result.getProductList) {
                if (entry.title.ToLower().Equals(moboName.Trim())) {
                    return entry;
                }
            }
            return null;
        }

        public static async Task<string> GetJsonData(string url) {
            return await new HttpClient().GetStringAsync(url);
        }

        public List<Msi.GetProductList> GetMsiMotherboards() {
            List<Msi.GetProductList> motherboards = new List<Msi.GetProductList>();
            // Remove invalid results
            // 18957 TPM 2.0 Module
            // 234531 xx (Mobo that was removed from site?)
            foreach (var mobo in msi.result.getProductList) {
                if (mobo.id == 18957) continue;
                if (mobo.id == 234531) continue;
                motherboards.Add(mobo);
            }
            return motherboards;
        }

        public List<Asus.Sku> GetAsusMotherboards() {
            return asus.result.skus;
        }


        private static readonly List<string> INTEL_CHIPSETS = [ 
            "Z690",
            "W680",
            "Q670",
            "H670",
            "B660",
            "H610",
            "R680",
            "Q670",
            "H610",
            "Z790",
            "H770",
            "B760",
        ];
        private static readonly List<string> AMD_CHIPSETS = [
            "A620",
            "B650",
            "X670",
        ];
        public static string? GetChipset(string moboName) {
            moboName = moboName.Trim().ToUpper();

            foreach (string cs in INTEL_CHIPSETS) {
                if (moboName.Contains(cs)) return cs;
            }

            foreach (string cs in AMD_CHIPSETS) {
                if (moboName.Contains(cs)) return cs;
            }

            return null;
        }
    }
}
