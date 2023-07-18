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
                    Method = HttpMethod.Get,
                    RequestUri = new Uri($"https://api.fanbox.cc/plan.listCreator?userId={settings.pixiv_id}"),
                    Headers = { }
                };
                foreach (var header in settings.headers)
                {
                    fanboxRequest.Headers.Add(header.Key, header.Value);
                }

                HttpResponseMessage resp = await client.SendAsync(fanboxRequest);
                return await resp.Content.ReadAsStringAsync();
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
                    Method = HttpMethod.Get,
                    RequestUri = new Uri($"https://api.fanbox.cc/relationship.listFans?status=supporter"),
                    Headers = { }
                };
                foreach (var header in settings.headers)
                {
                    fanboxRequest.Headers.Add(header.Key, header.Value);
                }

                HttpResponseMessage resp = await client.SendAsync(fanboxRequest);
                return await resp.Content.ReadAsStringAsync();
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
                    Method = HttpMethod.Get,
                    RequestUri = new Uri($"https://api.fanbox.cc/legacy/manage/pledge/monthly?month={month}"),
                    Headers = { }
                };
                foreach (var header in settings.headers)
                {
                    fanboxRequest.Headers.Add(header.Key, header.Value);
                }

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
