using System;
using System.IO;
using System.Net;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace Modpack_Installer
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Copyright 2020 LONGACAT");
            Console.WriteLine("This installer should be downloaded only from LONGACAT's GitHub.");
            Console.WriteLine("SKLauncher should be downloaded only from skmedix.pl.");
            Console.WriteLine("All re-hosts could modify the code, making the launcher or the modpack installer unsafe.");
            Console.WriteLine("");
            Console.WriteLine("Looking for modpack.zip...");
            if (!File.Exists(@"modpack.zip"))
            {
                Console.WriteLine("modpack.zip not found.");
                Console.ReadKey();
                System.Environment.Exit(1);
            }
            if (Directory.Exists(@"modpack"))
            {
                Directory.Delete(@"modpack", true);
            }
            Console.WriteLine("Extracting...");
            System.IO.Compression.ZipFile.ExtractToDirectory(@"modpack.zip", @"modpack");
            Directory.CreateDirectory(@"modpack\skl-profile");
            Directory.CreateDirectory(@"modpack\skl-profile\mods");
            Console.WriteLine("Reading data...");
            JObject modpackInfo = JObject.Parse(File.ReadAllText(@"modpack\manifest.json"));
            Console.WriteLine("Modpack is {0} by {1}, version {2}", modpackInfo["name"], modpackInfo["author"], modpackInfo["version"]);
            int filesToDownload = modpackInfo["files"].Count();
            Console.WriteLine("{0} files to download.", filesToDownload);
            for (int i = 0; i < filesToDownload; i++)
            {
                using (var client = new WebClient())
                {
                    string modURL = client.DownloadString("https://addons-ecs.forgesvc.net/api/v2/addon/" + modpackInfo["files"][i]["projectID"] + "/file/" + modpackInfo["files"][i]["fileID"] + "/download-url");
                    int idx = modURL.LastIndexOf('/');
                    if (idx == -1)
                    {
                        break;
                    }
                    string filename = modURL.Substring(idx + 1);
                    Console.WriteLine("Downloading {0}...", filename);
                    client.DownloadFile(new System.Uri(modURL), @"modpack\skl-profile\mods\" + filename);
                }
            }
            Console.WriteLine("Done. Copying overrides...");
            CopyAll(new DirectoryInfo(@"modpack\" + modpackInfo["overrides"]), new DirectoryInfo(@"modpack\skl-profile"));
            Console.WriteLine("Done. Checking for SKLauncher...");
            if (!Directory.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), ".minecraft")))
            {
                Console.WriteLine(".minecraft not found. Please copy the required files yourself.");
                Console.ReadKey();
                System.Environment.Exit(2);
            }
            if (!File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), @".minecraft\sklauncher-fx.jar")))
            {
                Console.WriteLine("SKLauncher not found. This app does not support any other launcher, please copy the required files yourself.");
                Console.ReadKey();
                System.Environment.Exit(3);
            }
            if (!Directory.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), @".minecraft\profiles")))
            {
                Directory.CreateDirectory(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), @".minecraft\profiles"));
            }
            if (Directory.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), @".minecraft\profiles\" + modpackInfo["name"])))
            {
                Console.WriteLine("The modpack is arleady installed.");
                Console.ReadKey();
                System.Environment.Exit(4);
            }
            Directory.CreateDirectory(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), @".minecraft\profiles\" + modpackInfo["name"]));
            Console.WriteLine("Done. Copying modpack to .minecraft...");
            CopyAll(new DirectoryInfo(@"modpack\skl-profile"), new DirectoryInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), @".minecraft\profiles\" + modpackInfo["name"])));
            Console.WriteLine("Done. Checking for modloader...");
            string versionString = modpackInfo["minecraft"]["version"].ToString();
            if (modpackInfo["minecraft"]["modLoaders"][0]["id"].ToString().Substring(0, 5) == "forge")
            {
                versionString = modpackInfo["minecraft"]["version"].ToString() + "-forge" + modpackInfo["minecraft"]["version"].ToString() + "-" + modpackInfo["minecraft"]["modLoaders"][0]["id"].ToString().Substring(6);
            }
            else
            {
                Console.WriteLine("Modpack does not use Forge. Please set the modloader yourself. Modloader info: " + modpackInfo["minecraft"]["version"].ToString() + " " + modpackInfo["minecraft"]["modLoaders"][0]["id"].ToString());
            }
            Console.WriteLine("Done. Writing new profile...");
            JObject launcherProfiles = JObject.Parse(File.ReadAllText(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), @".minecraft\launcher_profiles.json")));
            string profileString = @"{ """ + modpackInfo["name"] + @""": { ""name"": """ + modpackInfo["name"] + @""", ""gameDir"": """ + Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), @".minecraft\profiles\" + modpackInfo["name"]).ToString().Replace("\\", "\\\\") + @""",""lastVersionId"": """ + versionString + @""",""memoryMax"": 4096}}";
            launcherProfiles["profiles"]["(Default)"].Parent.AddAfterSelf(JToken.Parse(profileString).First);
            File.WriteAllText(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), @".minecraft\launcher_profiles.json"), launcherProfiles.ToString());
            Console.WriteLine("Done. Cleaing temporary files...");
            Directory.Delete(@"modpack", true);
            Console.WriteLine("Done. Installation completed.");

        }
        public static void CopyAll(DirectoryInfo source, DirectoryInfo target)
        {
            if (source.FullName.ToLower() == target.FullName.ToLower())
            {
                return;
            }

            // Check if the target directory exists, if not, create it.
            if (Directory.Exists(target.FullName) == false)
            {
                Directory.CreateDirectory(target.FullName);
            }

            // Copy each file into it's new directory.
            foreach (FileInfo fi in source.GetFiles())
            {
                // Console.WriteLine(@"Copying {0}\{1}", target.FullName, fi.Name);
                fi.CopyTo(Path.Combine(target.ToString(), fi.Name), true);
            }

            // Copy each subdirectory using recursion.
            foreach (DirectoryInfo diSourceSubDir in source.GetDirectories())
            {
                DirectoryInfo nextTargetSubDir =
                    target.CreateSubdirectory(diSourceSubDir.Name);
                CopyAll(diSourceSubDir, nextTargetSubDir);
            }
        }
    }
}
