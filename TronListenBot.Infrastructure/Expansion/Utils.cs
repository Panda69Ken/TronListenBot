using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using System.Net;
using System.Reflection;

namespace TronListenBot.Infrastructure.Expansion
{
    public static class Utils
    {
        static readonly DateTime startTime = new(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        public static long GetTimeStamp(this DateTime time)
        {
            return (time.Ticks - startTime.Ticks) / 10000000;
        }
        public static long GetMilliTimeStamp(this DateTime time)
        {
            return (time.Ticks - startTime.Ticks) / 10000;
        }

        public static DateTime GetTime(this long value)
        {
            return startTime.AddSeconds(value);
        }
        public static DateTime GetMilliTime(this long value)
        {
            return startTime.AddMilliseconds(value);
        }

        // 判断是否为合理的 Unix 毫秒时间戳
        public static bool IsPlausibleUnixMilliseconds(long ms, TimeSpan? maxPast = null, TimeSpan? maxFuture = null)
        {
            try
            {
                var dto = DateTimeOffset.FromUnixTimeMilliseconds(ms);
                var now = DateTimeOffset.UtcNow;

                // 默认：允许过去 100 年，允许未来 1 天（可按场景调整）
                maxPast ??= TimeSpan.FromDays(365 * 100);
                maxFuture ??= TimeSpan.FromDays(1);

                if (dto < DateTimeOffset.UnixEpoch) return false;
                if (dto < now - maxPast.Value) return false;
                if (dto > now + maxFuture.Value) return false;

                return true;
            }
            catch (ArgumentOutOfRangeException)
            {
                return false;
            }
        }

        /// <summary>
        /// CamelCase Json
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static string ToJsonEx(this object obj)
        {
            return JsonConvert.SerializeObject(obj, Formatting.None, new JsonSerializerSettings
            {
                ContractResolver = new Newtonsoft.Json.Serialization.CamelCasePropertyNamesContractResolver()
                //DateFormatString = "yyy-MM-dd HH:mm:ss"
            });
        }

        public static string BuildQuery(object data = null)
        {
            if (data == null) return "";
            string query = "";
            Type type = data.GetType();

            if (data is IQueryCollection collection)
            {
                foreach (var item in collection)
                {
                    if (string.IsNullOrWhiteSpace(item.Value)) continue;

                    query += "&" + item.Key + "=" + WebUtility.UrlEncode(Convert.ToString(item.Value));
                }
            }
            else
            {
                if (data is Dictionary<string, object> dictionary)
                {
                    foreach (var item in dictionary)
                    {
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
                            var value = Convert.ToString(p.GetValue(data));
                            if (string.IsNullOrWhiteSpace(value)) continue;
                            query += "&" + p.Name + "=" + WebUtility.UrlEncode(value);
                        }
                    }
                }
            }

            if (string.IsNullOrEmpty(query)) return "";
            return "?" + query.TrimStart('&');
        }

    }
}
