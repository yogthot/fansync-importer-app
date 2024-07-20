using FanSync.Ext;
using FanSync.HTTP;
using FanSync.Properties;
using FanSync.Windows;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Res = FanSync.Properties.Resources;

namespace FanSync
{
    class HitTestGuard : IDisposable
    {
        Window Owner { get; }
        public HitTestGuard(Window owner)
        {
            Owner = owner;
            Owner.IsHitTestVisible = false;
        }

        public void Dispose()
        {
            Owner.IsHitTestVisible = true;
        }
    }

    public partial class MainWindow : Window
    {
        private App app;
        public Settings settings;
        public event Action OnChanged;

        private bool closing;
        private bool clearClipboard;
        private bool updating;

        public MainWindow(App app, Settings settings)
        {
            this.app = app;
            this.settings = settings;
            InitializeComponent();

            closing = false;
            clearClipboard = false;
            updating = false;

            if (settings.session_cookie != null)
            {
                FanboxCookie.Text = settings.session_cookie;
            }

            if (settings.token != null)
            {
                FansyncToken.Text = settings.token;
            }

            UserAgent.Text = settings.headers.GetDefault(Settings.UserAgentHeader, "");
            CfClearance.Text = settings.cookies.GetDefault(Settings.CfClearanceCookie, "");

            if (settings.show_cloudflare)
            {
                UserAgentLabel.Visibility = Visibility.Visible;
                UserAgent.Visibility = Visibility.Visible;
                CfClearanceLabel.Visibility = Visibility.Visible;
                CfClearance.Visibility = Visibility.Visible;
            }
            else
            {
                UserAgentLabel.Visibility = Visibility.Hidden;
                UserAgent.Visibility = Visibility.Hidden;
                CfClearanceLabel.Visibility = Visibility.Hidden;
                CfClearance.Visibility = Visibility.Hidden;
            }

            ToggleFanboxCookieInput(false);
            ToggleFansyncTokenInput(false);

            // quick check for clearing the cookie from the clipboard
            DataObject.AddPastingHandler(FanboxCookie, (object sender, DataObjectPastingEventArgs e) =>
            {
                if (!e.IsDragDrop) clearClipboard = true;
            });

            app.importer.OnStatus += Importer_Status;
        }

        public void Exit()
        {
            closing = true;
            Close();
        }
        protected override void OnClosing(CancelEventArgs e)
        {
            this.Hide();
            ToggleFanboxCookieInput(false);
            ToggleFansyncTokenInput(false);

            // prevent closing unless explicitly closed from code
            e.Cancel = !closing;
            base.OnClosing(e);
        }

        private void Window_Activated(object sender, EventArgs e)
        {
            if (!app.isUpToDate)
            {
                VersionStatus.Visibility = Visibility.Visible;
            }

            UpdateBtn.IsEnabled = !string.IsNullOrEmpty(settings.session_cookie) &&
                                  !string.IsNullOrEmpty(settings.token) &&
                                  !updating;
        }
        private void VersionStatus_Click(object sender, MouseButtonEventArgs e)
        {
            Process.Start(app.updateUrl);
        }

        //DateTime LastErrorNotification { get; set; }
        public void NotifyError(string title, string message, Dictionary<string, string> action)
        {
            // TODO have some timer, only show an error every ~2.5 hours
            Notification.Show(title, message, action);
        }
        public void Importer_Status(object sender, ImporterStatus e)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                LastUpdate.Content = e.Timestamp.DateTime.ToString();
                switch (e.FanboxStatus)
                {
                    case FanboxStatus.Unknown:
                    case FanboxStatus.NotLoggedIn:
                        NotifyError(Res.title_error, Res.exc_fanbox_cookie, Notification.Action("settings"));
                        LastStatus.Content = Res.lbl_status_fanbox_error;
                        return;

                    case FanboxStatus.Cloudflare:
                        NotifyError(Res.title_error, Res.err_cloudflare_detected, Notification.Action("settings"));
                        LastStatus.Content = Res.err_cloudflare_detected;

                        settings.show_cloudflare = true;
                        UserAgentLabel.Visibility = Visibility.Visible;
                        UserAgent.Visibility = Visibility.Visible;
                        CfClearanceLabel.Visibility = Visibility.Visible;
                        CfClearance.Visibility = Visibility.Visible;
                        _ = settings.Save();
                        return;

                }
                
                if (!e.FansyncStatus)
                {
                    LastStatus.Content = Res.lbl_status_fansync_error;
                    NotifyError(Res.title_error, Res.exc_fansync_token, Notification.Action("settings"));
                    return;
                }
                else if (!e.NetworkStatus)
                {
                    LastStatus.Content = Res.lbl_status_network_error;
                    return;
                }
                else
                {
                    LastStatus.Content = Res.lbl_status_success;
                    return;
                }
            }));
        }

        #region status tab
        private void Update_Click(object sender, RoutedEventArgs e)
        {
            if (UpdateBtn.IsEnabled)
            {
                app.importer.ForceUpdate();

                LastStatus.Content = Res.lbl_updating;
                UpdateBtn.IsEnabled = false;
                updating = true;
                Task.Run(async () =>
                {
                    await Task.Delay(5000);
                    await Dispatcher.BeginInvoke(new Action(() =>
                    {
                        UpdateBtn.IsEnabled = true;
                        updating = false;
                    }));
                });
            }
        }
        #endregion

        #region settings tab
        private void ToggleFanboxCookieInput(bool active)
        {
            if (string.IsNullOrEmpty(FanboxCookie.Text))
                active = true;

            FanboxCookie.Visibility = active ? Visibility.Visible : Visibility.Hidden;
            FanboxCookieLabel.Visibility = !active ? Visibility.Visible : Visibility.Hidden;
        }
        private void ToggleFansyncTokenInput(bool active)
        {
            if (string.IsNullOrEmpty(FansyncToken.Text))
                active = true;

            FansyncToken.Visibility = active ? Visibility.Visible : Visibility.Hidden;
            FansyncTokenLabel.Visibility = !active ? Visibility.Visible : Visibility.Hidden;
        }

        private void FansyncTokenLabel_MouseDown(object sender, MouseButtonEventArgs e)
        {
            ToggleFansyncTokenInput(true);
        }
        private void FanboxCookieLabel_MouseDown(object sender, MouseButtonEventArgs e)
        {
            ToggleFanboxCookieInput(true);
        }

        private void Form_Changed(object sender, TextChangedEventArgs e)
        {
            string origUserAgent = settings.headers.GetDefault(Settings.UserAgentHeader, "");
            string origCfClearance = settings.cookies.GetDefault(Settings.CfClearanceCookie, "");

            // apply only enabled when values are changed
            ApplyButton.IsEnabled =
                    !FanboxCookie.Text.IsSimilar(settings.session_cookie) ||
                    !FansyncToken.Text.IsSimilar(settings.token) ||
                    !UserAgent.Text.IsSimilar(origUserAgent) ||
                    !CfClearance.Text.IsSimilar(origCfClearance);

            if (sender == FanboxCookie && clearClipboard)
            {
                Clipboard.Clear();
            }

            clearClipboard = false;
        }

        private async Task<bool> SaveSettings()
        {
            string origUserAgent = settings.headers.GetDefault(Settings.UserAgentHeader, "");
            string origCfClearance = settings.cookies.GetDefault(Settings.CfClearanceCookie, "");

            bool error = false;
            bool save = false;

            string cookie = FanboxCookie.Text.Trim();
            string cfCookie = CfClearance.Text.Trim();
            string uaHeader = UserAgent.Text.Trim();
            if (cookie != settings.session_cookie || cfCookie != origCfClearance || uaHeader != origUserAgent)
            {
                Settings test = settings.Clone();

                test.session_cookie = cookie;
                test.cookies[Settings.FanboxCookie] = cookie;
                test.cookies.SetOrRemove(Settings.CfClearanceCookie, cfCookie);
                test.headers[Settings.UserAgentHeader] = uaHeader;

                FanboxClient fanbox = new FanboxClient(test);
                Tuple<FanboxStatus, string> cookieStatus = await fanbox.TestCookie();

                switch (cookieStatus.Item1)
                {
                    case FanboxStatus.LoggedIn:
                        test.pixiv_id = cookieStatus.Item2;
                        settings.Update(test);
                        save = true;
                        break;

                    case FanboxStatus.Cloudflare:
                        DisplayError(Res.err_cloudflare_detected);

                        settings.show_cloudflare = true;
                        UserAgentLabel.Visibility = Visibility.Visible;
                        UserAgent.Visibility = Visibility.Visible;
                        CfClearanceLabel.Visibility = Visibility.Visible;
                        CfClearance.Visibility = Visibility.Visible;
                        await settings.Save();

                        error = true;
                        break;

                    case FanboxStatus.Unknown:
                    case FanboxStatus.NotLoggedIn:
                        DisplayError(Res.err_fanbox_cookie);
                        error = true;
                        break;

                }
            }

            if (!error)
            {
                string token = FansyncToken.Text.Trim();
                if (token != settings.token)
                {
                    FansyncClient fansync = new FansyncClient(settings);
                    bool success = await fansync.TestToken(token, settings.pixiv_id);

                    if (success)
                    {
                        settings.token = token;

                        save = true;
                    }
                    else
                    {
                        DisplayError(Res.err_fansync_token);
                        error = true;
                    }
                }
            }

            if (save)
            {
                await settings.Save();
                OnChanged?.Invoke();
            }

            if (!error)
            {
                // hides the error label
                DisplayError(null);
            }

            return !error;
        }

        private void DisplayError(string message)
        {
            if (message != null)
            {
                ErrorLabel.Content = message;
                ErrorLabel.Visibility = Visibility.Visible;
            }
            else
            {
                ErrorLabel.Visibility = Visibility.Hidden;
            }
        }
        #endregion

        #region advanced tab
        private async void Reset_Click(object sender, RoutedEventArgs e)
        {
            settings.Update(Settings.DefaultSettings);

            if (!string.IsNullOrEmpty(settings.session_cookie))
            {
                settings.cookies[Settings.FanboxCookie] = settings.session_cookie;
            }

            await settings.Save();

            FanboxCookie.Text = settings.session_cookie ?? "";
            FansyncToken.Text = settings.token ?? "";

            UserAgent.Text = settings.headers.GetDefault(Settings.UserAgentHeader, "");
            CfClearance.Text = settings.cookies.GetDefault(Settings.CfClearanceCookie, "");
        }
        private async void Reload_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                settings.Update(await Settings.Load());
            }
            catch
            {
                MessageBox.Show(this, Res.lbl_err_reload, Res.title_error, MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (!string.IsNullOrEmpty(settings.session_cookie))
            {
                settings.cookies[Settings.FanboxCookie] = settings.session_cookie;
            }

            await settings.Save();

            FanboxCookie.Text = settings.session_cookie ?? "";
            FansyncToken.Text = settings.token ?? "";

            UserAgent.Text = settings.headers.GetDefault(Settings.UserAgentHeader, "");
            CfClearance.Text = settings.cookies.GetDefault(Settings.CfClearanceCookie, "");
        }
        private async void Headers_Click(object sender, RoutedEventArgs e)
        {
            var readOnlyHeaders = new List<string>
            {
                Settings.UserAgentHeader,
                Settings.OriginHeader,
                Settings.RefererHeader,
            };
            var editor = new KeyValueEditor(this, Res.lbl_headers, settings.headers, readOnlyHeaders);
            using (new HitTestGuard(this))
                settings.headers = await editor.ShowAndWait();

            UserAgent.Text = settings.headers.GetDefault(Settings.UserAgentHeader, "");

            await settings.Save();
        }
        private async void Cookies_Click(object sender, RoutedEventArgs e)
        {
            var readOnlyCookies = new List<string>
            {
                Settings.FanboxCookie,
            };
            var editor = new KeyValueEditor(this, Res.lbl_cookies, settings.cookies, readOnlyCookies);
            using (new HitTestGuard(this))
                settings.cookies = await editor.ShowAndWait();

            if (settings.cookies[Settings.FanboxCookie] != settings.session_cookie)
            {
                settings.session_cookie = settings.cookies[Settings.FanboxCookie];
                FanboxCookie.Text = settings.session_cookie ?? "";
            }

            CfClearance.Text = settings.cookies.GetDefault(Settings.CfClearanceCookie, "");

            await settings.Save();
        }
        #endregion

        #region bottom buttons

        private async void Ok_Click(object sender, RoutedEventArgs e)
        {
            ErrorLabel.Visibility = Visibility.Hidden;
            LoadingLabel.Visibility = Visibility.Visible;
            if (await SaveSettings())
                this.Close();

            LoadingLabel.Visibility = Visibility.Hidden;
        }
        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
        private async void Apply_Click(object sender, RoutedEventArgs e)
        {
            ErrorLabel.Visibility = Visibility.Hidden;
            LoadingLabel.Visibility = Visibility.Visible;
            if (await SaveSettings())
                ApplyButton.IsEnabled = false;
            LoadingLabel.Visibility = Visibility.Hidden;
        }
        #endregion
    }
}
