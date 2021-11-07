using Microsoft.Toolkit.Uwp.Notifications;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace FanSync
{
    public class Notification
    {
        public static void Show(string title, string message, Dictionary<string, string> data = null, Dictionary<string, string> actions = null)
        {
            var notif = new ToastContentBuilder();
            notif.AddText(title);
            notif.AddText(message);

            if (data != null)
            {
                foreach (var entry in data)
                {
                    notif.AddArgument(entry.Key, entry.Value);
                }
            }

            if (actions != null)
            {
                foreach (var entry in actions)
                {
                    notif.AddButton(
                        new ToastButton()
                            .SetContent(entry.Key)
                            .AddArgument("action", entry.Value)
                    );
                }
            }

            notif.Show();
        }

        public static Dictionary<string, string> Action(string action)
        {
            return new Dictionary<string, string>()
            {
                { "action", action }
            };
        }
    }
}
