using FanSync.HTTP;
using System;
using System.Collections.Generic;
using System.ComponentModel;
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
    public partial class MainWindow : Window
    {
        private App app;
        public Settings settings;
        public event Action OnChanged;

        private bool closing;

        public MainWindow(App app, Settings settings)
        {
            this.app = app;
            InitializeComponent();
            this.settings = settings;
            closing = false;

            if (settings.session_cookie != null)
            {
                FanboxCookie.Text = settings.session_cookie;
            }

            if (settings.token != null)
            {
                FansyncToken.Text = settings.token;
            }

            ToggleFanboxCookieInput(false);
            ToggleFansyncTokenInput(false);

            app.importer.OnStatus += Importer_Status;
        }

        public void Importer_Status(object sender, ImporterStatus e)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                LastUpdate.Content = e.Timestamp.DateTime.ToString();
                if (!e.FanboxStatus)
                {
                    LastStatus.Content = Res.lbl_status_fanbox_error;
                }
                else if (!e.FansyncStatus)
                {
                    LastStatus.Content = Res.lbl_status_fansync_error;
                }
                else
                {
                    LastStatus.Content = Res.lbl_status_success;
                }
            }));
        }

        private void Window_Activated(object sender, EventArgs e)
        {
            if (!app.isUpToDate)
            {
                // not like it can update itself
                VersionStatus.Foreground = Brushes.Red;
                VersionStatus.Content = Res.lbl_update_available;
            }

            UpdateBtn.IsEnabled = !string.IsNullOrEmpty(settings.session_cookie) &&
                                  !string.IsNullOrEmpty(settings.token);
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

        public static bool IsSimilar(string left, string right)
        {
            return (string.IsNullOrEmpty(left) && string.IsNullOrEmpty(right)) || left == right;
        }
        private void Form_Changed(object sender, TextChangedEventArgs e)
        {
            // apply only enabled when values are changed
            ApplyButton.IsEnabled =
                    !IsSimilar(FanboxCookie.Text, settings.session_cookie) ||
                    !IsSimilar(FansyncToken.Text, settings.token);
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

        private async Task<bool> SaveSettings()
        {
            bool error = false;
            bool save = false;

            string cookie = FanboxCookie.Text.Trim();
            if (cookie != settings.session_cookie)
            {
                FanboxClient fanbox = new FanboxClient(settings);
                string pixiv_id = await fanbox.TestCookie(cookie);

                if (pixiv_id != null)
                {
                    settings.pixiv_id = pixiv_id;
                    settings.session_cookie = cookie;
                    settings.cookies[Settings.FanboxCookieName] = cookie;

                    save = true;
                }
                else
                {
                    DisplayError(Res.err_fanbox_cookie);
                    error = true;
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

            if (save) {
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

        private void Update_Click(object sender, RoutedEventArgs e)
        {
            app.importer.ForceUpdate();
        }
        
        private async void Reset_Click(object sender, RoutedEventArgs e)
        {
            Settings def = Settings.DefaultSettings;
            settings.endpoint = def.endpoint;
            settings.headers = def.headers;
            settings.cookies = def.cookies;

            if (!string.IsNullOrEmpty(settings.session_cookie))
            {
                settings.cookies[Settings.FanboxCookieName] = settings.session_cookie;
            }

            await settings.Save();
        }

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
    }
}
