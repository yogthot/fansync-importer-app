using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace FanSync.HTTP
{
    public class FansyncClient
    {
        private Settings settings;

        public FansyncClient(Settings settings)
        {
            this.settings = settings;
        }

        public async Task<FansyncStatus> GetStatus()
        {
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    HttpRequestMessage fansyncRequest = new HttpRequestMessage()
                    {
                        Method = HttpMethod.Get,
                        RequestUri = new Uri($"{settings.endpoint}/api/status")
                    };

                    HttpResponseMessage resp = await client.SendAsync(fansyncRequest);

                    return JsonConvert.DeserializeObject<FansyncStatus>(await resp.Content.ReadAsStringAsync());
                }
            }
            catch
            {
                // log?
                return null;
            }
        }

        public async Task<bool> SubmitPlans(string plans)
        {
            using (HttpClient client = new HttpClient())
            {
                HttpRequestMessage fansyncRequest = new HttpRequestMessage()
                {
                    Method = HttpMethod.Post,
                    RequestUri = new Uri($"{settings.endpoint}/api/creator/{settings.pixiv_id}/plans?token={settings.token}"),
                    Content = new StringContent(plans)
                };

                HttpResponseMessage resp = await client.SendAsync(fansyncRequest);
                if (resp.StatusCode == HttpStatusCode.Forbidden)
                    throw new Exception("fansync token was rejected");

                JObject json = JObject.Parse(await resp.Content.ReadAsStringAsync());
                return json.GetValue("success").ToObject<bool>();
            }
        }
        public async Task<bool> SubmitSupporters(string supporters)
        {
            using (HttpClient client = new HttpClient())
            {
                HttpRequestMessage fansyncRequest = new HttpRequestMessage()
                {
                    Method = HttpMethod.Post,
                    RequestUri = new Uri($"{settings.endpoint}/api/creator/{settings.pixiv_id}/supporters?token={settings.token}"),
                    Content = new StringContent(supporters)
                };

                HttpResponseMessage resp = await client.SendAsync(fansyncRequest);
                if (resp.StatusCode == HttpStatusCode.Forbidden)
                    throw new Exception("fansync token was rejected");

                JObject json = JObject.Parse(await resp.Content.ReadAsStringAsync());
                return json.GetValue("success").ToObject<bool>();
            }
        }

        // deprecated
        public async Task<bool> SubmitPledges(string month, string pledges)
        {
            using (HttpClient client = new HttpClient())
            {
                HttpRequestMessage fansyncRequest = new HttpRequestMessage()
                {
                    Method = HttpMethod.Post,
                    RequestUri = new Uri($"{settings.endpoint}/api/creator/{settings.pixiv_id}/pledges?month={month}&token={settings.token}"),
                    Content = new StringContent(pledges)
                };

                HttpResponseMessage resp = await client.SendAsync(fansyncRequest);
                if (resp.StatusCode == HttpStatusCode.Forbidden)
                    throw new Exception("fansync token was rejected");

                JObject json = JObject.Parse(await resp.Content.ReadAsStringAsync());
                return json.GetValue("success").ToObject<bool>();
            }
        }

        public async Task<bool> TestToken(string token, string pixiv_id)
        {
            using (HttpClient client = new HttpClient())
            {
                HttpRequestMessage fansyncRequest = new HttpRequestMessage()
                {
                    Method = HttpMethod.Get,
                    RequestUri = new Uri($"{settings.endpoint}/api/creator/{pixiv_id}/status?token={token}")
                };

                HttpResponseMessage resp = await client.SendAsync(fansyncRequest);
                return resp.StatusCode == HttpStatusCode.OK;
            }
        }
    }
}
