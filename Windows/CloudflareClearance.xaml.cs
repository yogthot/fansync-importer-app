using FanSync.Ext;
using FanSync.Properties;
using System;
using System.Collections.Generic;
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
using System.Windows.Shapes;
using Res = FanSync.Properties.Resources;

namespace FanSync.Windows
{
    public partial class CloudflareClearance : Window
    {
        public const string UserAgentHeader = "User-Agent";
        public const string CfClearanceCookie = "cf_clearance";

        public Settings Settings { get; set; }

        public string UserAgent { get; set; }
        public string CfClearance { get; set; }

        public CloudflareClearance(Window owner, Settings settings)
        {
            Owner = owner;
            Settings = settings;

            UserAgent = settings.headers.GetDefault(UserAgentHeader, "");
            CfClearance = settings.cookies.GetDefault(CfClearanceCookie, "");

            InitializeComponent();

            Title = Res.lbl_cloudflare_clearance;
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(UserAgent))
                Settings.headers[UserAgentHeader] = UserAgent;

            Settings.cookies.SetOrRemove(CfClearanceCookie, CfClearance);

            DialogResult = true;
            this.Close();
        }
        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            this.Close();
        }
    }
}
