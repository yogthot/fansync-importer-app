using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FanSync.HTTP
{
    public class FanboxAPIError : Exception
    {
        public string Content;

        public FanboxAPIError(string message, string content)
            : base(message)
        {
            Content = content;
        }
    }

    public class FanboxClient
    {
        private Settings settings;

        public FanboxClient(Settings settings)
        {
            this.settings = settings;
        }

        public async Task<string> GetPlans()
        {
            CookieContainer fanboxCookies = new CookieContainer();
            foreach (var cookie in settings.cookies)
            {
                fanboxCookies.Add(new Cookie(cookie.Key, cookie.Value) { Domain = "fanbox.cc" });
            }

            using (HttpClientHandler handler = new HttpClientHandler() { CookieContainer = fanboxCookies })
            using (HttpClient client = new HttpClient(handler))
            {
                HttpRequestMessage fanboxRequest = new HttpRequestMessage()
                {
                    Version = HttpVersion.Version11,
                    Method = HttpMethod.Get,
                    RequestUri = new Uri($"https://api.fanbox.cc/plan.listCreator?userId={settings.pixiv_id}"),
                    Headers = { }
                };
                foreach (var header in settings.headers)
                {
                    fanboxRequest.Headers.TryAddWithoutValidation(header.Key, header.Value);
                }

                if (!settings.headers.ContainsKey("Origin"))
                    fanboxRequest.Headers.Add("Origin", "https://www.fanbox.cc");
                if (!settings.headers.ContainsKey("Referer"))
                    fanboxRequest.Headers.Add("Referer", "https://www.fanbox.cc/");
                if (!settings.headers.ContainsKey("User-Agent"))
                    fanboxRequest.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/95.0.4638.69 Safari/537.36");

                HttpResponseMessage resp = await client.SendAsync(fanboxRequest);
                string content = await resp.Content.ReadAsStringAsync();

                JObject json = JObject.Parse(content);
                string error = Util.NavigateJson(json, "error")?.ToObject<string>();
                if (error != null)
                {
                    throw new FanboxAPIError(error, content);
                }

                return content;
            }
        }
        public async Task<string> GetSupporters()
        {
            CookieContainer fanboxCookies = new CookieContainer();
            foreach (var cookie in settings.cookies)
            {
                fanboxCookies.Add(new Cookie(cookie.Key, cookie.Value) { Domain = "fanbox.cc" });
            }

            using (HttpClientHandler handler = new HttpClientHandler() { CookieContainer = fanboxCookies })
            using (HttpClient client = new HttpClient(handler))
            {
                HttpRequestMessage fanboxRequest = new HttpRequestMessage()
                {
                    Version = HttpVersion.Version11,
                    Method = HttpMethod.Get,
                    RequestUri = new Uri($"https://api.fanbox.cc/relationship.listFans?status=supporter"),
                    Headers = { }
                };
                foreach (var header in settings.headers)
                {
                    fanboxRequest.Headers.TryAddWithoutValidation(header.Key, header.Value);
                }

                if (!settings.headers.ContainsKey("Origin"))
                    fanboxRequest.Headers.Add("Origin", "https://www.fanbox.cc");
                if (!settings.headers.ContainsKey("Referer"))
                    fanboxRequest.Headers.Add("Referer", "https://www.fanbox.cc/");
                if (!settings.headers.ContainsKey("User-Agent"))
                    fanboxRequest.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/95.0.4638.69 Safari/537.36");

                HttpResponseMessage resp = await client.SendAsync(fanboxRequest);
                string content = await resp.Content.ReadAsStringAsync();

                JObject json = JObject.Parse(content);
                string error = Util.NavigateJson(json, "error")?.ToObject<string>();
                if (error != null)
                {
                    throw new FanboxAPIError(error, content);
                }

                return content;
            }
        }

        // deprecated
        public async Task<string> GetPledges(string month)
        {
            CookieContainer fanboxCookies = new CookieContainer();
            foreach (var cookie in settings.cookies)
            {
                fanboxCookies.Add(new Cookie(cookie.Key, cookie.Value) { Domain = "fanbox.cc" });
            }

            using (HttpClientHandler handler = new HttpClientHandler() { CookieContainer = fanboxCookies })
            using (HttpClient client = new HttpClient(handler))
            {
                HttpRequestMessage fanboxRequest = new HttpRequestMessage()
                {
                    Version = HttpVersion.Version11,
                    Method = HttpMethod.Get,
                    RequestUri = new Uri($"https://api.fanbox.cc/legacy/manage/pledge/monthly?month={month}"),
                    Headers = { }
                };
                foreach (var header in settings.headers)
                {
                    fanboxRequest.Headers.TryAddWithoutValidation(header.Key, header.Value);
                }

                if (!settings.headers.ContainsKey("Origin"))
                    fanboxRequest.Headers.Add("Origin", "https://www.fanbox.cc");
                if (!settings.headers.ContainsKey("Referer"))
                    fanboxRequest.Headers.Add("Referer", "https://www.fanbox.cc/");
                if (!settings.headers.ContainsKey("User-Agent"))
                    fanboxRequest.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/95.0.4638.69 Safari/537.36");

                HttpResponseMessage resp = await client.SendAsync(fanboxRequest);
                return await resp.Content.ReadAsStringAsync();
            }
        }

        public async Task<string> TestCookie(string cookie)
        {
            CookieContainer fanboxCookies = new CookieContainer();
            fanboxCookies.Add(new Cookie(Settings.FanboxCookieName, cookie) { Domain = "fanbox.cc" });

            try
            {
                using (HttpClientHandler handler = new HttpClientHandler() { CookieContainer = fanboxCookies })
                using (HttpClient client = new HttpClient(handler))
                {
                    HttpRequestMessage fanboxRequest = new HttpRequestMessage()
                    {
                        Method = HttpMethod.Get,
                        RequestUri = new Uri($"https://www.fanbox.cc/manage/dashboard"),
                        Headers = { }
                    };
                    foreach (var header in settings.headers)
                    {
                        fanboxRequest.Headers.Add(header.Key, header.Value);
                    }

                    HttpResponseMessage resp = await client.SendAsync(fanboxRequest);
                    string body = await resp.Content.ReadAsStringAsync();

                    HtmlDocument html = Util.ParseHtml(body);
                    string metadata = html.GetElementById("metadata").GetAttribute("content");

                    JObject json = JObject.Parse(metadata);
                    return Util.NavigateJson(json, "context", "user", "userId").ToObject<string>();
                }
            }
            catch (Exception)
            {
                // TODO log (in case it keeps happening)
                return null;
            }
        }
    }
}
