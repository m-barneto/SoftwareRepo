using Newtonsoft.Json;
using System.Diagnostics;
using System.Net;
using System.Runtime.InteropServices;
using static BiosDownloader.MoboManager;
using static BiosDownloader.MoboManager.Asus;
using static BiosDownloader.MoboManager.Msi;

namespace BiosDownloader {
    #pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    public class MsiBios {
        public class AMIBIO {
            public int download_id { get; set; }
            public string download_url { get; set; }
            public string download_title { get; set; }
            public string download_description { get; set; }
            public string download_note { get; set; }
            public string download_file { get; set; }
            public int download_size { get; set; }
            public int isLiveupdate { get; set; }
            public bool os { get; set; }
            public string type_title { get; set; }
            public object language_title { get; set; }
            public bool youtube_link { get; set; }
            public bool youtube_title { get; set; }
            public string download_version { get; set; }
            public string download_release { get; set; }
            public int download_show_date { get; set; }
        }

        public class Downloads {
            [JsonProperty("AMI BIOS")]
            public List<AMIBIO> AMIBIOS { get; set; }
            public List<string> type_title { get; set; }
            public List<object> os { get; set; }
        }

        public class Result {
            public string category { get; set; }
            public string title { get; set; }
            public string note { get; set; }
            public Downloads downloads { get; set; }
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

    public class AsusBios {
        // Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse);
        public class DownloadUrl {
            public string Global { get; set; }
            public object China { get; set; }
        }

        public class File {
            public string Id { get; set; }
            public string Version { get; set; }
            public string Title { get; set; }
            public string Description { get; set; }
            public string FileSize { get; set; }
            public string ReleaseDate { get; set; }
            public string IsRelease { get; set; }
            public object PosType { get; set; }
            public DownloadUrl DownloadUrl { get; set; }
            public string SWID { get; set; }
            public string ExeModule { get; set; }
            public string Reboot { get; set; }
            public string Ac_power { get; set; }
            public string Usefor { get; set; }
            public string Severity { get; set; }
            public string UserSession { get; set; }
            public string Sign { get; set; }
            public string Tid { get; set; }
            public object HardwareInfoList { get; set; }
            public object INFDate { get; set; }
        }

        public class Obj {
            public string Name { get; set; }
            public int Count { get; set; }
            public List<File> Files { get; set; }
            public bool IsDescShow { get; set; }
        }

        public class Result {
            public int Count { get; set; }
            public List<Obj> Obj { get; set; }
        }

        public class Root {
            public Result Result { get; set; }
            public string Status { get; set; }
            public string Message { get; set; }
        }


    }
    #pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

    internal class Program {
        static readonly int BIOSES_TO_DOWNLOAD = 5;
        static int downloadedBioses = 0;

        static void Main(string[] args) {
            // Iterate over motherboards and handle downloading bioses
            MoboManager moboManager = new MoboManager();
            var asusMobos = moboManager.GetAsusMotherboards();
            Console.WriteLine($"Found {asusMobos.Count} Asus motherboards.");
            DownloadAsusBioses(asusMobos);
            var msiMobos = moboManager.GetMsiMotherboards();
            Console.WriteLine($"Found {msiMobos.Count} Msi motherboards.");
            DownloadMSIBioses(msiMobos);
            Console.WriteLine($"Successfully downloaded {downloadedBioses} new bioses.");
        }

        static void DownloadMSIBioses(List<GetProductList> motherboards) {
            // pruning motherboards to get more accurate progress report
            int pruned = 0;
            for (int i = motherboards.Count - 1; i > 0; i--) {
                GetProductList mobo = motherboards[i];
                string? chipset = GetChipset(mobo.title);
                if (chipset == null) {
                    pruned++;
                    motherboards.RemoveAt(i);
                }
            }
            Console.WriteLine($"Pruned {pruned} motherboards that we don't want to track. Remaining: {motherboards.Count}");
            
            Console.Write("Downloading MSI bioses... ");
            using (var progress = new ProgressBar()) {
                for (int i = 0; i <= motherboards.Count - 1; i++) {
                    progress.Report((double)i / (motherboards.Count - 1));
                    GetProductList mobo = motherboards[i];
                    string? chipset = GetChipset(mobo.title);
                    if (chipset == null) {
                        continue;
                    }

                    string biosLink = $"https://www.msi.com/api/v1/product/support/panel?product={mobo.link}&type=bios";

                    MsiBios.Root biosData = JsonConvert.DeserializeObject<MsiBios.Root>(GetJsonData(biosLink).Result)!;
                    List<MsiBios.AMIBIO> bioses = biosData.result.downloads.AMIBIOS;
                    if (bioses == null) continue;

                    DirectoryInfo dir = Directory.CreateDirectory($"C:\\Software\\BIOS\\MSI\\{chipset}\\{mobo.title}");

                    int biosCount = Math.Min(bioses.Count, BIOSES_TO_DOWNLOAD);
                    for (int j = 0; j < biosCount; j++) {
                        MsiBios.AMIBIO bios = bioses[j];
                        // Check if dir/file exists?
                        string biosFilePath = dir.FullName + Path.DirectorySeparatorChar + bios.download_version;
                        if (Directory.Exists(biosFilePath)) {
                            continue;
                        }

                        // Download and extract, overwrite existing file or delete after extracting
                        try {
                            DownloadFile(bios.download_url, biosFilePath + ".zip").Wait();
                        } catch (Exception e) {
                            Console.WriteLine(e);
                            continue;
                        }
                        System.IO.Compression.ZipFile.ExtractToDirectory(biosFilePath + ".zip", biosFilePath);
                        File.Delete(biosFilePath + ".zip");
                        downloadedBioses++;
                    }
                }
            }
            Console.WriteLine("Done.\n");
        }

        static void RunAsusBiosRenamer(string biosFilePath) {
            if (Directory.Exists(biosFilePath)) {
                // Get renamer in bios folder
                string renamer = biosFilePath + "\\BIOSRenamer.exe";
                if (File.Exists(renamer)) {
                    ProcessStartInfo info = new ProcessStartInfo(renamer);
                    info.WorkingDirectory = biosFilePath;
                    info.RedirectStandardOutput = true;
                    info.UseShellExecute = false;
                    info.CreateNoWindow = true;
                    info.RedirectStandardInput = true;
                    var process = Process.Start(info);
                    if (process == null) {
                        Console.WriteLine("Failed to start BIOSRenamer.");
                        return;
                    }
                    var standardInput = process.StandardInput;
                    standardInput.WriteLine();
                    process.WaitForExit();
                }
            }
        }

        static void DownloadAsusBioses(List<Asus.Sku> motherboards) {
            // pruning motherboards to get more accurate progress report
            int pruned = 0;
            for (int i = motherboards.Count - 1; i > 0; i--) {
                Sku mobo = motherboards[i];
                string? chipset = GetChipset(mobo.mktName);
                if (chipset == null) {
                    pruned++;
                    motherboards.RemoveAt(i);
                }
            }
            Console.WriteLine($"Pruned {pruned} motherboards that we don't want to track. Remaining: {motherboards.Count}");

            Console.Write("Downloading Asus bioses...");
            using (var progress = new ProgressBar()) {
                for (int i = 0; i <= motherboards.Count - 1; i++) {
                    progress.Report((double)i / (motherboards.Count - 1));
                    Sku mobo = motherboards[i];
                    string? chipset = GetChipset(mobo.mktName);
                    if (chipset == null) {
                        continue;
                    }

                    string biosLink = $"https://rog.asus.com/support/webapi/product/GetPDBIOS?website=us&pdid={mobo.productId}&cpu=";
                    AsusBios.Root biosData = JsonConvert.DeserializeObject<AsusBios.Root>(GetJsonData(biosLink).Result)!;
                    if (biosData.Result.Obj.Count == 0) continue;

                    List<AsusBios.File> bioses = biosData.Result.Obj[0].Files;
                    if (bioses == null) continue;

                    DirectoryInfo dir = Directory.CreateDirectory($"C:\\Software\\BIOS\\ASUS\\{chipset}\\{mobo.mktName}");

                    int biosCount = Math.Min(bioses.Count, BIOSES_TO_DOWNLOAD);

                    for (int j = 0; j < biosCount; j++) {
                        AsusBios.File bios = bioses[j];
                        // Check if dir/file exists?
                        string biosFilePath = dir.FullName + Path.DirectorySeparatorChar + bios.Title;
                        if (Directory.Exists(biosFilePath)) {
                            continue;
                        }

                        // Download and extract, overwrite existing file or delete after extracting
                        try {
                            if (bios.DownloadUrl.China != null) {
                                Console.WriteLine("CHINESE DOWNLOAD???");
                                Console.WriteLine(mobo.mktName);
                            }
                            DownloadFile(bios.DownloadUrl.Global, biosFilePath + ".zip").Wait();
                        } catch (Exception e) {
                            Console.WriteLine(e);
                            continue;
                        }
                        System.IO.Compression.ZipFile.ExtractToDirectory(biosFilePath + ".zip", biosFilePath);
                        File.Delete(biosFilePath + ".zip");
                        RunAsusBiosRenamer(biosFilePath);
                        downloadedBioses++;
                    }
                }
            }
            Console.WriteLine("Done.\n");
        }

        public static async Task DownloadFile(string address, string fileName) {
            using (var client = new HttpClient())
            using (var response = await client.GetAsync(address)) {
                if (!response.IsSuccessStatusCode) {
                    throw new Exception($"{address} response " +  response.StatusCode);
                }
                using (var stream = await response.Content.ReadAsStreamAsync())
                using (var file = File.OpenWrite(fileName)) {
                    stream.CopyTo(file);
                }
            }
        }
    }
}