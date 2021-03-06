﻿#region

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using LoLAccountChecker.Data;
using LoLAccountChecker.Views;
using MahApps.Metro.Controls.Dialogs;
using Newtonsoft.Json;

#endregion

namespace LoLAccountChecker
{
    internal class Utils
    {
        public static List<Account> GetLogins(string file)
        {
            var logins = new List<Account>();

            var sr = new StreamReader(file);
            string line;
            while ((line = sr.ReadLine()) != null)
            {
                var accountData = line.Split(new[] { ':' });

                if (accountData.Count() < 2)
                {
                    continue;
                }

                var loginData = new Account
                {
                    Username = accountData[0],
                    Password = accountData[1],
                    State = Account.Result.Unchecked
                };

                logins.Add(loginData);
            }

            return logins;
        }

        public static void ExportLogins(string file, List<Account> accounts, bool exportErrors)
        {
            using (var sw = new StreamWriter(file))
            {
                if (!exportErrors)
                {
                    accounts = accounts.Where(a => a.State == Account.Result.Success).ToList();
                }

                foreach (var account in accounts)
                {
                    sw.WriteLine("{0}:{1}", account.Username, account.Password);
                }
            }
        }

        public static void ExportAsJson(string file, List<Account> accounts, bool exportErrors)
        {
            using (var sw = new StreamWriter(file))
            {
                if (!exportErrors)
                {
                    accounts = accounts.Where(a => a.State == Account.Result.Success).ToList();
                }

                sw.Write(JsonConvert.SerializeObject(accounts));
            }
        }

        public static void ExportException(Exception e)
        {
            var dir = Path.Combine(Directory.GetCurrentDirectory(), "Logs");

            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            var file = string.Format("crash_{0:dd-MM-yyyy_HH-mm-ss}.txt", DateTime.Now);

            using (var sw = new StreamWriter(Path.Combine(dir, file)))
            {
                sw.WriteLine(e.ToString());
            }
        }

        public static async Task UpdateClientVersion()
        {
            using (var wc = new WebClient())
            {
                try
                {
                    var clientVersion =
                        wc.DownloadString(
                            "https://raw.githubusercontent.com/madk/LoLAccountChecker/master/League/Client.version");

                    if (Settings.Config.ClientVersion == null)
                    {
                        Settings.Config.ClientVersion = clientVersion;
                        return;
                    }

                    if (clientVersion == Settings.Config.ClientVersion)
                    {
                        return;
                    }

                    var result =
                        await
                            MainWindow.Instance.ShowMessageAsync(
                                "Client version outdated",
                                "The client version of League of Legends looks different, do you wanna update it?",
                                MessageDialogStyle.AffirmativeAndNegative);

                    if (result == MessageDialogResult.Affirmative)
                    {
                        Settings.Config.ClientVersion = clientVersion;
                    }
                }
                catch
                {
                    // ignore
                }
            }
        }
    }
}