using Newtonsoft.Json;
using System.Text;
using TronListenBot.Infrastructure.Enums;
using TronListenBot.Infrastructure.Expansion;
using TronListenBot.Svc.Core.Expansion;
using TronListenBot.Svc.Core.Model;
using TronNet;

namespace TronListenBot.Svc.Core.Service
{
    public interface ITronGridClient
    {
        /// <summary>
        /// 获取账户历史转账记录（包括trc10转账和TRX转账）
        /// </summary>
        /// <returns></returns>
        Task<List<TransactionRecord>> GetTRC10Transactions(string address);

        /// <summary>
        /// 获取账户历史合约交易记录（TRC20、TRC721转账记录）
        /// </summary>
        /// <returns></returns>
        Task<List<TransactionRecord>> GetTRC20Transactions(string address);
    }

    public class TronGridClient(ILogger<TronGridClient> _logger, IConfigService _config, IHttpClientFactory _clientFactory) : ITronGridClient
    {
        private async Task<string> RequestHandle(HttpMethod method, string path, object data = null)
        {
            string result;
            var param = "";
            if (method == HttpMethod.Post && data != null)
                param = data.ToJsonEx();
            if (method == HttpMethod.Get)
                param = Utils.BuildQuery(data);

            var httpClient = _clientFactory.CreateTryClient();
            httpClient.BaseAddress = new Uri(_config.TronConfig.TronGrid);

            var num = new Random().Next(0, _config.TronConfig.ApiKeys.Count);

            var apiKey = _config.TronConfig.ApiKeys[num];

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
                    _logger.LogError($"拒绝访问,apiKey:{apiKey},path:{path},param:{data.ToJsonEx()},msg:{rep.StatusCode},result:{result}");
                    return result;
                }
                result = await rep.Content.ReadAsStringAsync();

                _logger.LogInformation($"stopwatch apiKey:{apiKey},path:{path},param:{data.ToJsonEx()}");
            }
            catch (TimeoutException ex)
            {
                _logger.LogError($"访问超时,apiKey:{apiKey},path:{path},param:{data.ToJsonEx()},msg:{ex.Message}");
                result = "{\"Msg\":\"" + ex.Message + "\",\"Code\":\"408\"}";
            }
            catch (Exception ex)
            {
                _logger.LogError($"访问异常,apiKey:{apiKey},path:{path},param:{data.ToJsonEx()},msg:{ex.Message}");
                if (ex.Message.IndexOf("timed out") >= 0 || ex.Message.IndexOf("Bad Gateway") >= 0)
                    result = "{\"Msg\":\"" + ex.Message + "\",\"Code\":\"408\"}";
                else
                    result = "{\"Msg\":\"" + ex.Message + "\",\"Code\":\"500\"}";
            }

            return result;
        }

        public async Task<List<TransactionRecord>> GetTRC10Transactions(string address)
        {
            var path = $"/v1/accounts/{address}/transactions";

            var list = new List<TransactionRecord>();

            var param = new
            {
                only_confirmed = true,
                limit = 200,
                order_by = "block_timestamp,desc"
            };

        GetList:
            var result = await RequestHandle(HttpMethod.Get, path, param);

            if (result == "0")
                return list;

            try
            {
                var json = JsonConvert.DeserializeObject<TronGridTRXTransactionInfoResult>(result);

                var datas = json.data.Where(a => a.raw_data.contract[0].type == "TransferContract" || a.raw_data.contract[0].type == "TransferAssetContract");

                if (datas.Any())
                {
                    list.AddRange(datas.Select(a => new TransactionRecord
                    {
                        HashId = a.txID,
                        Symbol = a.raw_data.contract[0].type == "TransferContract" ? CurrencyEnum.TRX.ToString() : "",
                        TransactionType = a.raw_data.contract[0].parameter.value.to_address.ReplaceFirst() == address ? 1 : 2,
                        Amount = TronUnit.SunToTRX(a.raw_data.contract[0].parameter.value.amount),
                        TransactionTime = a.block_timestamp
                    }));
                }

                if (json.meta.links != null)
                {
                    if (!string.IsNullOrEmpty(json.meta.links.next))
                    {
                        path = json.meta.links.next.Replace(_config.TronConfig.TronGrid, "");
                        param = null;
                        goto GetList;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"序列化异常,result:{result},error:{ex.Message}");

                return list;
            }

            return list;
        }

        public async Task<List<TransactionRecord>> GetTRC20Transactions(string address)
        {
            var path = $"/v1/accounts/{address}/transactions/trc20";

            var list = new List<TransactionRecord>();

            var param = new
            {
                only_confirmed = true,
                limit = 200,
                //contract_address = _config.TronConfig.Contract
            };

        GetList:
            var result = await RequestHandle(HttpMethod.Get, path, param);

            if (result == "0")
                return list;

            try
            {
                var json = JsonConvert.DeserializeObject<TronGridTUSDTransactionInfoResult>(result);

                list.AddRange(json.data.Select(a => new TransactionRecord
                {
                    HashId = a.transaction_id,
                    Symbol = a.token_info.symbol == CurrencyEnum.USDT.ToString() ? CurrencyEnum.USDT.ToString() : a.token_info.symbol,
                    TransactionType = a.to == address ? 1 : 2,
                    Amount = TronUnit.SunToTRX(a.value), //Convert.ToDecimal(a.value) / 1000000
                    TransactionTime = a.block_timestamp
                }));

                if (json.meta.links != null)
                {
                    if (!string.IsNullOrEmpty(json.meta.links.next))
                    {
                        path = json.meta.links.next.Replace(_config.TronConfig.TronGrid, "");
                        param = null;
                        goto GetList;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"序列化异常,result:{result},error:{ex.Message}");

                return list;
            }

            return list;
        }
    }
}
