using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FanSync
{
    public class Util
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
                obj = ((JObject)obj).GetValue(key);
            }
            return obj;
        }
    }
}
