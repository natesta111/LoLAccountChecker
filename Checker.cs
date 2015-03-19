#region

using System.Collections.Generic;
using System.Linq;
using LoLAccountChecker.Data;
using LoLAccountChecker.Views;
using MahApps.Metro.Controls.Dialogs;

#endregion

namespace LoLAccountChecker
{
    internal delegate void NewAccount(Account accout);

    internal static class Checker
    {
        static Checker()
        {
            Accounts = new List<Account>();
            IsChecking = false;
        }

        public static List<Account> Accounts { get; set; }
        public static bool IsChecking { get; private set; }

        public static async void Start()
        {
            if (IsChecking)
            {
                return;
            }

            IsChecking = true;

            var region = Settings.Config.SelectedRegion;

            string dialogMessage;
            
            if (!Accounts.Any(a => a.State == Account.Result.Outdated))
            {
                while (Accounts.Any(a => a.State == Account.Result.Unchecked))
                {
                    if (!IsChecking)
                    {
                        break;
                    }
                    var account = Accounts.FirstOrDefault(a => a.State == Account.Result.Unchecked);

                    if (account == null)
                    {
                        continue;
                    }


                    var client = new Client(region, account.Username, account.Password);

                    await client.IsCompleted.Task;

                    var i = Accounts.FindIndex(a => a.Username == account.Username);
                    Accounts[i] = client.Data;

                    MainWindow.Instance.UpdateControls();

                    if (AccountsWindow.Instance != null)
                    {
                        AccountsWindow.Instance.RefreshAccounts();
                    }
                }
                dialogMessage = Accounts.Count(a => a.State == Account.Result.Success) > 1 ?
                    string.Format("{0} accounts have been successfully checked!", Accounts.Count(a => a.State == Account.Result.Success)) :
                    "1 account has been successfully checked!";
            }
            else
            {
                dialogMessage = Accounts.Count(a => a.State == Account.Result.Outdated) > 1 ? 
                    string.Format("{0} accounts have been refreshed!", Accounts.Count(a => a.State == Account.Result.Outdated).ToString()) : 
                    "1 account has been refreshed!"; 
                while (Accounts.Any(a => a.State == Account.Result.Outdated))
                {
                    if (!IsChecking)
                    {
                        break;
                    }
                    var account = Accounts.FirstOrDefault(a => a.State == Account.Result.Outdated);

                    var client = new Client(region, account.Username, account.Password);

                    await client.IsCompleted.Task;

                    var i = Accounts.FindIndex(a => a.Username == account.Username);
                    Accounts[i] = client.Data;

                    MainWindow.Instance.UpdateControls();

                    if (AccountsWindow.Instance != null)
                    {
                        AccountsWindow.Instance.RefreshAccounts();
                    }
                }
            }

            IsChecking = false;

            MainWindow.Instance.UpdateControls();

            if (Accounts.All(a => a.State != Account.Result.Unchecked))
            {
                await MainWindow.Instance.Dispatcher.Invoke(() => MainWindow.Instance.ShowMessageAsync("Done", dialogMessage));
            }
        }

        public static void Stop()
        {
            if (!IsChecking)
            {
                return;
            }

            IsChecking = false;
        }
    }
}