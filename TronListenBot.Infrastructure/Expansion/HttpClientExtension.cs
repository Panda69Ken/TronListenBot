using Newtonsoft.Json;
using System.Net;
using System.Reflection;
using System.Text;

namespace TronListenBot.Infrastructure.Expansion
{
    public static class HttpClientExtension
    {
        public static HttpClient CreateTryClient(this IHttpClientFactory factory)
        {
            return factory.CreateClient("ReTry");
        }

        static string BuildQuery(object data = null)
        {
            if (data == null) return "";
            string query = "";
            Type type = data.GetType();

            if (data is Microsoft.AspNetCore.Http.IQueryCollection)
            {
                var data2 = data as Microsoft.AspNetCore.Http.IQueryCollection;
                foreach (var item in data2)
                {
                    if (string.IsNullOrWhiteSpace(item.Value)) continue;

                    query += "&" + item.Key + "=" + WebUtility.UrlEncode(Convert.ToString(item.Value));
                }
            }
            else
            {
                PropertyInfo[] pi = type.GetProperties();
                foreach (PropertyInfo p in pi)
                {
                    if (p.GetValue(data) != null)
                    {
                        query += "&" + p.Name + "=" + WebUtility.UrlEncode(Convert.ToString(p.GetValue(data)));
                    }
                }
            }

            return "?" + query.TrimStart('&');
        }

        public static HttpClient SetHeaders(this HttpClient client, Dictionary<string, string> headers)
        {
            if (headers == null || headers?.Count == 0) return client;

            foreach (var header in headers)
            {
                client.DefaultRequestHeaders.Remove(header.Key);
                client.DefaultRequestHeaders.Add(header.Key, header.Value);
            }

            return client;
        }

        public static async Task<T> SendAsync<T>(this HttpClient client, HttpRequestMessage requestMessage)
        {
            var rep = await client.SendAsync(requestMessage);

            if (!rep.IsSuccessStatusCode) return default;

            var content = await rep.Content.ReadAsStringAsync();

            return JsonConvert.DeserializeObject<T>(content, new JsonSerializerSettings
            {
                ContractResolver = new Newtonsoft.Json.Serialization.CamelCasePropertyNamesContractResolver()
            });
        }

        public static async Task<T> GetAsync<T>(this HttpClient client, string url, object data)
        {
            //var query = string.Join(",", Request.Query.Select(a => $"&{a.Key}={a.Value}")).Trim('&');
            url += BuildQuery(data);
            return await SendAsync<T>(client, new HttpRequestMessage()
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri(url),
            });
        }

        public static async Task<T> PostAsync<T>(this HttpClient client, string url, object data)
        {
            return await SendAsync<T>(client, new HttpRequestMessage()
            {
                Method = HttpMethod.Post,
                RequestUri = new Uri(url),
                Content = new StringContent(data != null ? JsonConvert.SerializeObject(data) : "", Encoding.UTF8, "application/json")
            });
        }
    }
}
