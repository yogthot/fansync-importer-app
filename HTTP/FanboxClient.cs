using FanSync.HTTP;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static FanSync.HTTP.FanboxClient;

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

    public enum FanboxStatus
    {
        Unknown,
        NotLoggedIn,
        Cloudflare,
        LoggedIn
    }

    public class FanboxClient
    {
        public const string Domain = "fanbox.cc";
        public const string Endpoint = "https://www." + Domain;
        public const string ApiEndpoint = "https://api." + Domain;

        private Settings Settings { get; set; }

        public FanboxClient(Settings settings)
        {
            Settings = settings;
        }

        public async Task<Tuple<FanboxStatus, string>> GetPlans()
        {
            CookieContainer fanboxCookies = new CookieContainer();
            foreach (var cookie in Settings.cookies)
            {
                fanboxCookies.Add(new Cookie(cookie.Key, cookie.Value) { Domain = Domain });
            }

            using (HttpClientHandler handler = new HttpClientHandler() { CookieContainer = fanboxCookies })
            using (HttpClient client = new HttpClient(handler))
            {
                HttpRequestMessage fanboxRequest = new HttpRequestMessage()
                {
                    Version = HttpVersion.Version11,
                    Method = HttpMethod.Get,
                    RequestUri = new Uri($"{ApiEndpoint}/plan.listCreator?userId={Settings.pixiv_id}"),
                    Headers = { }
                };
                foreach (var header in Settings.headers)
                {
                    fanboxRequest.Headers.TryAddWithoutValidation(header.Key, header.Value);
                }

                if (!Settings.headers.ContainsKey("Origin"))
                    fanboxRequest.Headers.Add("Origin", Endpoint);
                if (!Settings.headers.ContainsKey("Referer"))
                    fanboxRequest.Headers.Add("Referer", Endpoint);
                if (!Settings.headers.ContainsKey("User-Agent"))
                    fanboxRequest.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/95.0.4638.69 Safari/537.36");

                HttpResponseMessage resp = await client.SendAsync(fanboxRequest);
                if (resp.StatusCode == HttpStatusCode.Forbidden)
                {
                    if (resp.Headers.Select(x => x.Key.ToLower()).Contains("cf-mitigated"))
                    {
                        return Tuple.Create(FanboxStatus.Cloudflare, "");
                    }
                }
                string content = await resp.Content.ReadAsStringAsync();

                JObject json = JObject.Parse(content);
                string error = Util.NavigateJson(json, "error")?.ToObject<string>();
                if (error != null)
                {
                    //throw new FanboxAPIError(error, content);
                    return Tuple.Create(FanboxStatus.NotLoggedIn, content);
                }

                return Tuple.Create(FanboxStatus.LoggedIn, content);
            }
        }
        public async Task<Tuple<FanboxStatus, string>> GetSupporters()
        {
            CookieContainer fanboxCookies = new CookieContainer();
            foreach (var cookie in Settings.cookies)
            {
                fanboxCookies.Add(new Cookie(cookie.Key, cookie.Value) { Domain = Domain });
            }

            using (HttpClientHandler handler = new HttpClientHandler() { CookieContainer = fanboxCookies })
            using (HttpClient client = new HttpClient(handler))
            {
                HttpRequestMessage fanboxRequest = new HttpRequestMessage()
                {
                    Version = HttpVersion.Version11,
                    Method = HttpMethod.Get,
                    RequestUri = new Uri($"{ApiEndpoint}/relationship.listFans?status=supporter"),
                    Headers = { }
                };
                foreach (var header in Settings.headers)
                {
                    fanboxRequest.Headers.TryAddWithoutValidation(header.Key, header.Value);
                }

                if (!Settings.headers.ContainsKey("Origin"))
                    fanboxRequest.Headers.Add("Origin", Endpoint);
                if (!Settings.headers.ContainsKey("Referer"))
                    fanboxRequest.Headers.Add("Referer", Endpoint);
                if (!Settings.headers.ContainsKey("User-Agent"))
                    fanboxRequest.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/95.0.4638.69 Safari/537.36");

                HttpResponseMessage resp = await client.SendAsync(fanboxRequest);
                if (resp.StatusCode == HttpStatusCode.Forbidden)
                {
                    if (resp.Headers.Select(x => x.Key.ToLower()).Contains("cf-mitigated"))
                    {
                        return Tuple.Create(FanboxStatus.Cloudflare, "");
                    }
                }
                string content = await resp.Content.ReadAsStringAsync();

                JObject json = JObject.Parse(content);
                string error = Util.NavigateJson(json, "error")?.ToObject<string>();
                if (error != null)
                {
                    //throw new FanboxAPIError(error, content);
                    return Tuple.Create(FanboxStatus.NotLoggedIn, content);
                }

                return Tuple.Create(FanboxStatus.LoggedIn, content);
            }
        }

        // deprecated
        public async Task<string> GetPledges(string month)
        {
            CookieContainer fanboxCookies = new CookieContainer();
            foreach (var cookie in Settings.cookies)
            {
                fanboxCookies.Add(new Cookie(cookie.Key, cookie.Value) { Domain = Domain });
            }

            using (HttpClientHandler handler = new HttpClientHandler() { CookieContainer = fanboxCookies })
            using (HttpClient client = new HttpClient(handler))
            {
                HttpRequestMessage fanboxRequest = new HttpRequestMessage()
                {
                    Version = HttpVersion.Version11,
                    Method = HttpMethod.Get,
                    RequestUri = new Uri($"{ApiEndpoint}/legacy/manage/pledge/monthly?month={month}"),
                    Headers = { }
                };
                foreach (var header in Settings.headers)
                {
                    fanboxRequest.Headers.TryAddWithoutValidation(header.Key, header.Value);
                }

                if (!Settings.headers.ContainsKey("Origin"))
                    fanboxRequest.Headers.Add("Origin", Endpoint);
                if (!Settings.headers.ContainsKey("Referer"))
                    fanboxRequest.Headers.Add("Referer", Endpoint);
                if (!Settings.headers.ContainsKey("User-Agent"))
                    fanboxRequest.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/95.0.4638.69 Safari/537.36");

                HttpResponseMessage resp = await client.SendAsync(fanboxRequest);
                return await resp.Content.ReadAsStringAsync();
            }
        }

        public async Task<Tuple<FanboxStatus, string>> TestCookie()
        {
            CookieContainer fanboxCookies = new CookieContainer();
            foreach (var cookie in Settings.cookies)
            {
                fanboxCookies.Add(new Cookie(cookie.Key, cookie.Value) { Domain = Domain });
            }

            // TODO detect cloudflare
            try
            {
                using (HttpClientHandler handler = new HttpClientHandler() {
                    CookieContainer = fanboxCookies,
                    AllowAutoRedirect = false 
                })
                using (HttpClient client = new HttpClient(handler))
                {
                    HttpRequestMessage fanboxRequest = new HttpRequestMessage()
                    {
                        Method = HttpMethod.Get,
                        RequestUri = new Uri($"{Endpoint}/manage/dashboard"),
                        Headers = { }
                    };
                    foreach (var header in Settings.headers)
                    {
                        fanboxRequest.Headers.TryAddWithoutValidation(header.Key, header.Value);
                    }

                    HttpResponseMessage resp = await client.SendAsync(fanboxRequest);
                    if (resp.StatusCode == HttpStatusCode.Found)
                    {
                        // 302
                        return Tuple.Create(FanboxStatus.NotLoggedIn, "");
                    }
                    else if (resp.StatusCode == HttpStatusCode.Forbidden)
                    {
                        // 403
                        if (resp.Headers.Select(x => x.Key.ToLower()).Contains("cf-mitigated"))
                        {
                            return Tuple.Create(FanboxStatus.Cloudflare, "");
                        }
                    }

                    string body = await resp.Content.ReadAsStringAsync();

                    HtmlDocument html = Util.ParseHtml(body);
                    string metadata = html.GetElementById("metadata").GetAttribute("content");

                    JObject json = JObject.Parse(metadata);
                    string userId = Util.NavigateJson(json, "context", "user", "userId").ToObject<string>();
                    if (userId == null)
                    {
                        return Tuple.Create(FanboxStatus.NotLoggedIn, "");
                    }

                    return Tuple.Create(FanboxStatus.LoggedIn, userId);
                }
            }
            catch (Exception)
            {
                // TODO log (in case it keeps happening)
                return Tuple.Create(FanboxStatus.Unknown, "");
            }
        }
    }
}
