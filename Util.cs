using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FanSync
{
    public static class Util
    {
        public static HtmlDocument ParseHtml(string body)
        {
            using (WebBrowser browser = new WebBrowser())
            {
                browser.ScriptErrorsSuppressed = true;
                browser.Navigate(String.Empty);
                HtmlDocument html = browser.Document;
                html.Write(body);
                return html;
            }
        }

        public static JToken NavigateJson(JToken obj, params string[] keys)
        {
            foreach (string key in keys)
            {
                if (!((JObject)obj).ContainsKey(key))
                    return null;
                
                obj = ((JObject)obj).GetValue(key);
            }
            return obj;
        }

        public static bool IsSimilar(this string left, string right)
        {
            return (string.IsNullOrEmpty(left) && string.IsNullOrEmpty(right)) || left == right;
        }
    }
}
