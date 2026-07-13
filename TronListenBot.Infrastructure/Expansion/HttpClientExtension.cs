using Newtonsoft.Json;

namespace TronListenBot.Infrastructure.Expansion
{
    public static class HttpClientExtension
    {
        public static HttpClient CreateTryClient(this IHttpClientFactory factory)
        {
            return factory.CreateClient("ReTry");
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

    }
}
