using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FanSync
{
    [JsonObject(MemberSerialization.OptIn)]
    public class Settings
    {
        public static string ConfigFileName = "fansync.json";
        public static string ConfigPath => $"{AppDomain.CurrentDomain.BaseDirectory}/{ConfigFileName}";

        public const string FanboxCookie  = "FANBOXSESSID";
        public const string CfClearanceCookie = "cf_clearance";

        public const string UserAgentHeader   = "User-Agent";
        public const string OriginHeader = "Origin";
        public const string RefererHeader = "Referer";

        public static Settings DefaultSettings => new Settings()
        {
            endpoint = "https://fansync.moe",
            headers = new Dictionary<string, string>()
                    {
                        { OriginHeader, "https://www.fanbox.cc" },
                        { RefererHeader, "https://www.fanbox.cc/" },
                        { UserAgentHeader, "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/126.0.0.0 Safari/537.36" },
                    }
        };

        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public string endpoint { get; set; }

        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public string token { get; set; }

        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public string pixiv_id { get; set; }

        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public string session_cookie { get; set; }

        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public Dictionary<string, string> cookies { get; set; }

        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public Dictionary<string, string> headers { get; set; }

        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public DateTimeOffset? last_update_time { get; set; }

        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public bool show_cloudflare { get; set; }

        public Settings()
        {
            cookies = new Dictionary<string, string>();
            headers = new Dictionary<string, string>();
        }

        public bool IsValid()
        {
            return
                !string.IsNullOrEmpty(endpoint) &&
                !string.IsNullOrEmpty(token) &&
                !string.IsNullOrEmpty(pixiv_id) &&
                !string.IsNullOrEmpty(session_cookie);
        }

        public static async Task<Settings> Load()
        {
            try
            {
                using (FileStream stream = File.Open(ConfigPath, FileMode.Open, FileAccess.Read))
                {
                    byte[] content = new byte[stream.Length];
                    await stream.ReadAsync(content, 0, content.Length);
                    string json = Encoding.UTF8.GetString(content);

                    return JsonConvert.DeserializeObject<Settings>(json);
                }
            }
            catch (FileNotFoundException)
            {
                return DefaultSettings;
            }
        }

        public async Task Save()
        {
            using (FileStream stream = File.Open(ConfigPath, FileMode.Create, FileAccess.Write))
            {
                string json = JsonConvert.SerializeObject(this, Formatting.Indented);
                byte[] content = Encoding.UTF8.GetBytes(json);

                await stream.WriteAsync(content, 0, content.Length);
            }
        }

        public void Update(Settings other)
        {
            endpoint = other.endpoint;
            token = other.token;

            pixiv_id = other.pixiv_id;
            session_cookie = other.session_cookie;

            cookies = other.cookies;
            headers = other.headers;

            //last_update_time = other.last_update_time;

            show_cloudflare = other.show_cloudflare;
        }

        public Settings Clone()
        {
            return new Settings
            {
                endpoint = this.endpoint,
                token = this.token,

                pixiv_id = this.pixiv_id,
                session_cookie = this.session_cookie,

                cookies = this.cookies.ToDictionary(x => x.Key, x => x.Value),
                headers = this.headers.ToDictionary(x => x.Key, x => x.Value),

                last_update_time = this.last_update_time,

                show_cloudflare = this.show_cloudflare,
            };
        }
    }
}
