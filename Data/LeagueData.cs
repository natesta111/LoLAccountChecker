﻿#region

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using LoLAccountChecker.Views;
using MahApps.Metro.Controls.Dialogs;
using Newtonsoft.Json;

#endregion

namespace LoLAccountChecker.Data
{
    internal static class LeagueData
    {
        private static string _repoUrl;
        private static List<string> _files;

        static LeagueData()
        {
            _repoUrl = "https://raw.githubusercontent.com/madk/LoLAccountChecker/master/";
            _files = new List<string>
            {
                "League/Champions.json",
                "League/Runes.json"
            };
        }

        public static List<Champion> Champions { get; private set; }
        public static List<Rune> Runes { get; private set; }

        public static async Task Load()
        {
            var toDownload = _files.Where(f => !File.Exists(f) || IsOutdated(f));

            if (toDownload.Any())
            {
                var fileCount = 0;

                var wc = new WebClient();
                var dialog = await MainWindow.Instance.ShowProgressAsync("Updating", "Downloading required files...");
                wc.DownloadProgressChanged += (o, p) => dialog.SetProgress(p.ProgressPercentage / 100d);
                wc.DownloadFileCompleted += (o, a) =>
                {
                    if (fileCount >= toDownload.Count())
                    {
                        dialog.CloseAsync();
                    }
                };

                if (!Directory.Exists("League"))
                {
                    Directory.CreateDirectory("League");
                }

                foreach (var file in toDownload)
                {
                    fileCount++;

                    var versionFile = file.Replace(Path.GetFileName(file), string.Empty) +
                                      Path.GetFileNameWithoutExtension(file) + ".version";

                    await wc.DownloadFileTaskAsync(new Uri(_repoUrl + file), file);
                    try
                    {
                        await wc.DownloadFileTaskAsync(new Uri(_repoUrl + versionFile), versionFile);
                    }
                    catch
                    {
                        // no version file for this file
                    }
                }
            }

            using (var sr = new StreamReader("League/Champions.json"))
            {
                Champions = JsonConvert.DeserializeObject<List<Champion>>(sr.ReadToEnd());
            }

            using (var sr = new StreamReader("League/Runes.json"))
            {
                Runes = JsonConvert.DeserializeObject<List<Rune>>(sr.ReadToEnd());
            }
        }

        private static bool IsOutdated(string file)
        {
            var versionFile = file.Replace(Path.GetFileName(file), string.Empty) +
                              Path.GetFileNameWithoutExtension(file) + ".version";

            if (!File.Exists(file) || !File.Exists(versionFile))
            {
                return true;
            }

            using (var wc = new WebClient())
            {
                string version;

                try
                {
                    version = wc.DownloadString(_repoUrl + versionFile);
                }
                catch
                {
                    return false; // no version file for this file
                }

                string currentVersion;

                using (var sr = new StreamReader(versionFile))
                {
                    currentVersion = sr.ReadLine();
                }

                return currentVersion != null && Version.Parse(version) > Version.Parse(currentVersion);
            }
        }
    }
}