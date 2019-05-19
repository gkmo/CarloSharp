using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using PuppeteerSharp;

namespace CarloSharp
{
    public static class Carlo
    {
        static Carlo()
        {
            Logger = new ConsoleLogger();
        }

        public static ILogger Logger { get; set; }

        public static App Launch(Options options)
        {
            return LaunchAsync(options).Result;
        }

        public static async Task<App> LaunchAsync(Options options)
        {
            if (options.BgColor == Color.Empty)
            {
                options.BgColor = Color.FromArgb(255, 255, 255, 255);
            }

            if (string.IsNullOrEmpty(options.LocalDataDir))
            {
                options.LocalDataDir = ".local-data";
            }

            var executablePath = FindChrome(options);

            if (executablePath == null)
            {
                throw new Exception("Could not find Chrome installation, please make sure Chrome browser is installed from https://www.google.com/chrome/.");
            }

            var title = string.IsNullOrEmpty(options.Title) ? "" : Uri.EscapeDataString(options.Title);
            var background = Uri.EscapeDataString(ToHtml(options.BgColor));
            var paramsForReuse = options.ParamsForReuse == null ? "undefided" : Newtonsoft.Json.JsonConvert.ToString(options.ParamsForReuse);

            string targetPage = $@"
                <title>{title}</title>
                <style>html{{ background:{background} }}</style>
                <script>self.paramsForReuse = {paramsForReuse};</script>";

            var args = new List<string> {
                $"--app=data:text/html,{targetPage}",
                $"--enable-features=NetworkService"
            };

            if (options.Args != null)
            {
                args.AddRange(options.Args);
            }

            if (options.Width.HasValue && options.Height.HasValue)
            {
                args.Add($"--window-size={options.Width},{options.Height}");
            }

            if (options.Left.HasValue && options.Top.HasValue)
            {
                args.Add($"--window-position={options.Left},{options.Top}");
            }

            var launchOptions = new LaunchOptions
            {
                ExecutablePath = executablePath.Item1,
                Headless = false,
                DefaultViewport = null,
                UserDataDir = options.UserDataDir ?? options.LocalDataDir + $"profile-{executablePath.Item2}",
                Args = args.ToArray()
            };

            var browser = await Puppeteer.LaunchAsync(launchOptions);

            var app = new App(browser, options);

            await app.InitAsync();

            return app;
        }

        private static Tuple<string, string> FindChrome(Options options)
        {
            if (!string.IsNullOrEmpty(options.ExecutablePath))
            {
                return new Tuple<string, string>(options.ExecutablePath, "user");
            }

            if (options.Channel == null)
            {
                options.Channel = new string[] { "stable" };
            }

            string executablePath = null;

            if (options.Channel.Contains("canary") || options.Channel.Contains("*"))
            {
                if (Environment.OSVersion.Platform == PlatformID.Unix)
                {
                    executablePath = FindChromeLinux(true);
                }
                else if (Environment.OSVersion.Platform == PlatformID.Win32NT)
                {
                    executablePath = FindChromeWin32(true);
                }

                if (executablePath != null)
                {
                    return new Tuple<string, string>( executablePath, "canary");
                }
            }

            if (options.Channel.Contains("stable") || options.Channel.Contains("*"))
            {
                if (Environment.OSVersion.Platform == PlatformID.Unix)
                {
                    executablePath = FindChromeLinux(false);
                }
                else if (Environment.OSVersion.Platform == PlatformID.Win32NT)
                {
                    executablePath = FindChromeWin32(false);
                }

                if (executablePath != null)
                {
                    return new Tuple<string, string>(executablePath, "stable");
                }
            }

            if (options.Channel.Contains("chromium") || options.Channel.Contains("*"))
            {
                var revisionInfo = DownloadChromiumAsync(new BrowserFetcherOptions()).Result;

                return new Tuple<string, string>(revisionInfo.ExecutablePath, revisionInfo.Revision.ToString());
            }

            return null;
        }

        private static string FindChromeLinux(bool canary)
        {
            var installations = new List<string>();

            // Look into the directories where .desktop are saved on gnome based distro's
            var desktopInstallationFolders = new string[] {
              Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".local/share/applications/"),
              "/usr/share/applications/"
            };

            foreach (var folder in desktopInstallationFolders)
            {
                var executableInstallations = FindChromeExecutables(folder);
                
                if (executableInstallations != null)
                {
                    installations.AddRange(executableInstallations);
                }
            }

            // Look for google-chrome(-stable) & chromium(-browser) executables by using the which command
            var executables = new string[] {
              "google-chrome-stable",
              "google-chrome",
              "chromium-browser",
              "chromium"
            };
            
            foreach (var executable in executables)
            {
                var chromePath = ExecuteBashCommand($"which {executable}").Split('\n').FirstOrDefault();

                if (!string.IsNullOrEmpty(chromePath))
                {
                    installations.Add(chromePath);
                }                
            }
            
            if (installations.Count == 0)
            {
                throw new Exception("The environment variable CHROME_PATH must be set to executable of a build of Chromium version 54.0 or later.");
            }

            var priorities = new Dictionary<string, int>(){
               {"/chrome-wrapper$/", 51},
               {"/google-chrome-stable$/", 50},
               {"/google-chrome$/", 49},
               {"/chromium-browser$/", 48},
               {"/chromium$/", 47}
             };

            var environmentChromePath = Environment.GetEnvironmentVariable("CHROME_PATH");

            if (!string.IsNullOrEmpty(environmentChromePath))
            {
                priorities.Add(environmentChromePath, 101);
            }
            
            return Sort(installations, priorities).FirstOrDefault();
        }

        private static IList<string> Sort(List<string> installations, Dictionary<string, int> priorities) 
        {
            const int defaultPriority = 10;

            var result = new List<Tuple<string, int>>();

            foreach (var installation in installations)
            {
                bool added = false;

                foreach (var priority in priorities)
                {
                    if (Regex.IsMatch(installation, priority.Key))
                    {
                        result.Add(new Tuple<string, int>(installation, priority.Value));
                        added = true;
                        break;
                    }
                }

                if (!added)
                {
                    result.Add(new Tuple<string, int>(installation, defaultPriority));
                }
            }

            return result.OrderByDescending(i => i.Item2).Select(i => i.Item1).ToList();
        }

        private static string ExecuteBashCommand(string command)
        {
            command = command.Replace("\"","\"\"");

            var proc = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "/bin/bash",
                    Arguments = "-c \""+ command + "\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                }
            };

            proc.Start();
            proc.WaitForExit();

            return proc.StandardOutput.ReadToEnd();
        }

        private static List<string> FindChromeExecutables(string folder)
        {
            var argumentsRegex = @"/ (^[^ ] +).*/"; // Take everything up to the first space
            var chromeExecRegex = @"^Exec=\/.*\/(google-chrome|chrome|chromium)-.*";

            if (CanAccess(folder))
            {
                // Output of the grep & print looks like:
                //    /opt/google/chrome/google-chrome --profile-directory
                //    /home/user/Downloads/chrome-linux/chrome-wrapper %U
                string execPaths;

                // Some systems do not support grep -R so fallback to -r.
                // See https://github.com/GoogleChrome/chrome-launcher/issues/46 for more context.
                try
                {
                    execPaths = ExecuteBashCommand($"grep - ER \"{chromeExecRegex}\" ${ folder} | awk - F '=' '{{print $2}}'");
                }
                catch (Exception)
                {
                    execPaths = ExecuteBashCommand($"grep - Er \"{chromeExecRegex}\" ${ folder} | awk - F '=' '{{print $2}}'");
                }

                var paths = execPaths.Split('\n');

                var installations = new List<string>();

                foreach (var path in paths)
                {
                    var newPath = Regex.Replace(path, argumentsRegex, "$1", RegexOptions.IgnoreCase);

                    if (CanAccess(newPath))
                    {
                        installations.Add(newPath);
                    }
                }

                return installations;
            }

            return null;
        }

        private static bool CanAccess(string folder)
        {
            return !string.IsNullOrEmpty(folder);
        }

        private static string FindChromeWin32(bool canary)
        {
            var suffix = canary ? $"Google{Path.DirectorySeparatorChar}Chrome SxS{Path.DirectorySeparatorChar}Application{Path.DirectorySeparatorChar}chrome.exe" :
                                  $"Google{Path.DirectorySeparatorChar}Chrome{Path.DirectorySeparatorChar}Application{Path.DirectorySeparatorChar}chrome.exe";

            var prefixes = new string[] 
            {
              Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
              Environment.ExpandEnvironmentVariables("%ProgramW6432%"),
              Environment.ExpandEnvironmentVariables("%ProgramFiles(x86)%")
            };

            string result = null;

            foreach (var prefix in prefixes)
            {
                var chromePath = Path.Combine(prefix, suffix);

                if (File.Exists(chromePath))
                {
                    result = chromePath;
                }
            }
            
            return result;
        }

        private static async Task<RevisionInfo> DownloadChromiumAsync(BrowserFetcherOptions options)
        {
            var fetcher = Puppeteer.CreateBrowserFetcher(options);

            if (fetcher.LocalRevisions().Contains(BrowserFetcher.DefaultRevision))
            {
                return fetcher.RevisionInfo(BrowserFetcher.DefaultRevision);
            }

            try
            {
                Logger?.Debug($"Downloading Chromium r{BrowserFetcher.DefaultRevision}...");

                var revisionInfo = await fetcher.DownloadAsync(BrowserFetcher.DefaultRevision);

                Logger?.Debug($"Chromium downloaded to {revisionInfo.FolderPath}");

                return revisionInfo;
            }
            catch(Exception ex)
            {
                Logger?.Error($"ERROR: Failed to download Chromium r{BrowserFetcher.DefaultRevision}!)");
                Logger?.Error(ex.Message);
            }

            return null;
        }

        private static string ToHtml(Color c)
        {
            return "#" + c.R.ToString("X2") + c.G.ToString("X2") + c.B.ToString("X2");
        }
    }
}
