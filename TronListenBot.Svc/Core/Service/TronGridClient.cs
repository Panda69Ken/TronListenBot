using Newtonsoft.Json;
using System.Text;
using TronListenBot.Infrastructure.Enums;
using TronListenBot.Infrastructure.Expansion;
using TronListenBot.Svc.Core.Model;
using TronNet;

namespace TronListenBot.Svc.Core.Service
{
    public interface ITronGridClient
    {
        /// <summary>
        /// 获取账户详细信息
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        Task<Accountv2> GetAccountv2(string address);

        /// <summary>
        /// 获取 TRX 转账列表
        /// </summary>
        /// <returns></returns>
        Task<List<TransactionRecord>> GetTRXTransactions(string address, int total);

        /// <summary>
        /// 获取 USDT 转账列表
        /// </summary>
        /// <returns></returns>
        Task<List<TransactionRecord>> GetUSDTTransactions(string address, int total);
    }

    public class TronGridClient(ILogger<TronGridClient> _logger, IConfigService _config, IHttpClientFactory _clientFactory) : ITronGridClient
    {
        private async Task<string?> RequestHandle(HttpMethod method, string path, object data = null)
        {
            string result;
            var param = "";
            if (method == HttpMethod.Post && data != null)
                param = data.ToJsonEx();
            if (method == HttpMethod.Get)
                param = Utils.BuildQuery(data);

            var httpClient = _clientFactory.CreateTryClient();

            httpClient.BaseAddress = new Uri(_config.TronConfig.TronApi);

            var num = new Random().Next(0, _config.TronConfig.TronApiKeys.Count);

            var apiKey = _config.TronConfig.TronApiKeys[num];

            httpClient.SetHeaders(new Dictionary<string, string> {
                { "TRON-PRO-API-KEY", apiKey }
            });

            try
            {
                var httpRequestMessage = new HttpRequestMessage(method, $"{path}{(method == HttpMethod.Get ? param : "")}");

                if (method == HttpMethod.Post)
                {
                    httpRequestMessage.Content = new StringContent(param, Encoding.UTF8, "application/json");
                }

                var rep = await httpClient.SendAsync(httpRequestMessage);

                if (!rep.IsSuccessStatusCode)
                {
                    result = await rep.Content.ReadAsStringAsync();
                    _logger.LogError($"拒绝访问,path:{path},param:{data.ToJsonEx()},msg:{rep.StatusCode},result:{result}");
                    return result;
                }
                result = await rep.Content.ReadAsStringAsync();

                _logger.LogInformation($"RequestHandle path:{path},param:{data.ToJsonEx()}");
            }
            catch (TimeoutException ex)
            {
                _logger.LogError($"访问超时,path:{path},param:{data.ToJsonEx()},msg:{ex.Message}");
                result = "{\"Msg\":\"" + ex.Message + "\",\"Code\":\"408\"}";
            }
            catch (Exception ex)
            {
                _logger.LogError($"访问异常,path:{path},param:{data.ToJsonEx()},msg:{ex.Message}");
                if (ex.Message.IndexOf("timed out") >= 0 || ex.Message.IndexOf("Bad Gateway") >= 0)
                    result = "{\"Msg\":\"" + ex.Message + "\",\"Code\":\"408\"}";
                else
                    result = "{\"Msg\":\"" + ex.Message + "\",\"Code\":\"500\"}";
            }

            return result;
        }

        private async Task<List<T>> GetPaginatedData<T>(Func<int, int, Task<string>> requestFunc,
            Func<string, List<T>> deserializeFunc,
            int totalNeeded,
            int pageSize = 50)
        {
            var result = new List<T>();
            var start = 0;

            while (result.Count < totalNeeded)
            {
                try
                {
                    var response = await requestFunc(start, pageSize);

                    // 检查错误响应
                    if (IsErrorResponse(response))
                    {
                        _logger.LogWarning($"API 错误响应: {response}");
                        break;
                    }

                    var pageData = deserializeFunc(response);

                    if (pageData == null || pageData.Count == 0)
                        break;

                    result.AddRange(pageData);
                    start += pageSize;
                }
                catch (Exception ex)
                {
                    _logger.LogError($"分页请求异常 | Page: {start} | Error: {ex.Message}");
                    break;
                }
            }

            return result;
        }

        private bool IsErrorResponse(string response)
        {
            return response.Contains("message") || response.Contains("Msg");
        }

        public async Task<Accountv2> GetAccountv2(string address)
        {
            var json = await RequestHandle(HttpMethod.Get, "/api/accountv2", new
            {
                address
            });

            if (json == null || IsErrorResponse(json)) return null;

            return JsonConvert.DeserializeObject<Accountv2>(json);
        }

        public async Task<List<TransactionRecord>> GetTRXTransactions(string address, int total)
        {
            return await GetPaginatedData(
                requestFunc: async (start, limit) =>
                {
                    var param = new
                    {
                        start,
                        limit = 50,
                        address,
                        token = "_",
                        sort = "-timestamp"
                    };
                    return await RequestHandle(HttpMethod.Get, "/api/transfer", param);
                },
                deserializeFunc: (response) =>
                {
                    try
                    {
                        var json = JsonConvert.DeserializeObject<TronTransferResult>(response);
                        return json?.Data?.Select(a => new TransactionRecord
                        {
                            HashId = a.TransactionHash,
                            Symbol = CurrencyEnum.TRX.ToString(),
                            TransactionType = a.TransferToAddress == address ? 1 : 2,
                            Amount = TronUnit.SunToTRX(a.Amount),
                            Timestamp = a.Timestamp
                        }).ToList() ?? [];
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"序列化异常: {ex.Message}");
                        return [];
                    }
                },
                totalNeeded: total
            );
        }

        public async Task<List<TransactionRecord>> GetUSDTTransactions(string address, int total)
        {
            return await GetPaginatedData(
                requestFunc: async (start, limit) =>
                {
                    var param = new
                    {
                        start,
                        limit = 50,
                        contract_address = _config.TronConfig.Contract,
                        relatedAddress = address
                    };
                    return await RequestHandle(HttpMethod.Get, "/api/token_trc20/transfers", param);
                },
                deserializeFunc: (response) =>
                {
                    try
                    {
                        var json = JsonConvert.DeserializeObject<TronTransferResult>(response);
                        return json?.Token_Transfers?.Select(a => new TransactionRecord
                        {
                            HashId = a.Transaction_Id,
                            Symbol = CurrencyEnum.USDT.ToString(),
                            TransactionType = a.To_Address == address ? 1 : 2,
                            Amount = TronUnit.SunToTRX(a.Quant),
                            Timestamp = a.Block_Ts
                        }).ToList() ?? [];
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"序列化异常: {ex.Message}");
                        return [];
                    }
                },
                totalNeeded: total
            );

        }

    }
}
