using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Carlo.Net
{
    public static class Carlo
    {
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

            var launchOptions = new PuppeteerSharp.LaunchOptions
            {
                ExecutablePath = executablePath.Item1,
                Headless = false,
                UserDataDir = options.UserDataDir ?? options.LocalDataDir + $"profile-{executablePath.Item2}",
                Args = args.ToArray()
            };

            var browser = await PuppeteerSharp.Puppeteer.LaunchAsync(launchOptions);

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
                executablePath = DownloadChromium(options);
            }

            return null;
        }

        private static string FindChromeLinux(bool canary)
        {
            var installations = new List<string>();

            // Look into the directories where .desktop are saved on gnome based distro's
            var desktopInstallationFolders = new string[] {
              Path.Combine("require('os').homedir()", ".local/share/applications/"),
              "/usr/share/applications/"
            };

            foreach (var folder in desktopInstallationFolders)
            {
                installations.Add(FindChromeExecutables(folder));
            }

            // Look for google-chrome(-stable) & chromium(-browser) executables by using the which command
            var executables = new string[] {
              "google-chrome-stable",
              "google-chrome",
              "chromium-browser",
              "chromium"
            };
            //            executables.forEach(executable => {
            //            try
            //            {
            //                const chromePath =
            //                    execFileSync('which', [executable], { stdio: 'pipe'}).toString().split(newLineRegex)[0];
            //            if (canAccess(chromePath))
            //                installations.push(chromePath);
            //        } catch (e) {
            //      // Not installed.
            //    }
            //});

            //  if (!installations.length)
            //    throw new Error('The environment variable CHROME_PATH must be set to executable of a build of Chromium version 54.0 or later.');

            //const priorities = [
            //    {regex: / chrome - wrapper$/, weight: 51},
            //    {regex: /google-chrome-stable$/, weight: 50},
            //    {regex: /google-chrome$/, weight: 49},
            //    {regex: /chromium-browser$/, weight: 48},
            //    {regex: /chromium$/, weight: 47},
            //  ];

            //  if (process.env.CHROME_PATH)
            //    priorities.unshift({ regex: new RegExp(`${ process.env.CHROME_PATH }`), weight: 101});

            //  return sort(uniq(installations.filter(Boolean)), priorities)[0];
            //}

            return installations.FirstOrDefault();
        }

        private static string FindChromeExecutables(string folder)
        {
            throw new NotImplementedException();
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

        private static string DownloadChromium(Options options)
        {
            throw new NotImplementedException();
        }

        private static string ToHtml(Color c)
        {
            return "#" + c.R.ToString("X2") + c.G.ToString("X2") + c.B.ToString("X2");
        }
    }
}
