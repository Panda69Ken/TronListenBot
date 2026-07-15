using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using TronListenBot.Infrastructure.Enums;
using TronNet.Crypto;
using TronNet.Protocol;

namespace TronListenBot.Svc.Core.Expansion
{
    public static class StringExtension
    {
        public static string ReplaceFirst(this string value)
        {
            if (string.IsNullOrEmpty(value)) return value;
            var reg = new Regex("0x");
            return reg.Replace(value, "41", 1).EncodeFromHex();
        }

        public static string EncodeFromHex(this string value)
        {
            if (string.IsNullOrEmpty(value)) return "";

            var hexByte = Enumerable.Range(0, value.Length)
                .Where(x => x % 2 == 0)
                .Select(x => Convert.ToByte(value.Substring(x, 2), 16))
            .ToArray(); ;

            return Base58Encoder.EncodeFromHex(hexByte, 65);
        }

        public static byte[] HexStringToBytes(this string hex)
        {
            if (string.IsNullOrEmpty(hex)) return null;
            if (hex.Length % 2 == 1) hex = "0" + hex;
            try
            {
                return Enumerable.Range(0, hex.Length / 2)
                    .Select(i => Convert.ToByte(hex.Substring(i * 2, 2), 16))
                    .ToArray();
            }
            catch
            {
                return null;
            }
        }

        #region ParameterValueToJson
        public static string ParameterValueToJson(this Google.Protobuf.WellKnownTypes.Any param)
        {
            if (param == null) return "";

            try
            {
                var options = new JsonSerializerOptions { WriteIndented = true };

                if (param.Is(TransferContract.Descriptor))
                {
                    var transfer = param.Unpack<TransferContract>();
                    var result = new Dictionary<string, object?>
                    {
                        ["fromAddress"] = transfer.OwnerAddress != null ? Convert.ToHexString(transfer.OwnerAddress.ToByteArray()).ToLowerInvariant().ReplaceFirst() : null,
                        ["toAddress"] = transfer.ToAddress != null ? Convert.ToHexString(transfer.ToAddress.ToByteArray()).ToLowerInvariant().ReplaceFirst() : null,
                        ["amount"] = transfer.Amount,
                        ["symbol"] = CurrencyEnum.TRX.ToString(),
                        ["type"] = TransactionType.TRX
                    };
                    return JsonSerializer.Serialize(result, options);
                }
                if (param.Is(TriggerSmartContract.Descriptor))
                {
                    var sc = param.Unpack<TriggerSmartContract>();
                    var decoded = DecodeTriggerSmartContract(sc);
                    return JsonSerializer.Serialize(decoded, options);
                }
                if (param.Is(FreezeBalanceV2Contract.Descriptor))
                {
                    var transfer = param.Unpack<FreezeBalanceV2Contract>();
                    var result = new Dictionary<string, object?>
                    {
                        ["fromAddress"] = transfer.OwnerAddress != null ? Convert.ToHexString(transfer.OwnerAddress.ToByteArray()).ToLowerInvariant().ReplaceFirst() : null,
                        ["amount"] = transfer.FrozenBalance,
                        ["symbol"] = CurrencyEnum.TRX.ToString(),
                        ["type"] = transfer.Resource == ResourceCode.Energy ? TransactionType.Energy : TransactionType.Bandwidth
                    };
                    return JsonSerializer.Serialize(result, options);
                }
                if (param.Is(UnfreezeBalanceV2Contract.Descriptor))
                {
                    var transfer = param.Unpack<UnfreezeBalanceV2Contract>();
                    var result = new Dictionary<string, object?>
                    {
                        ["fromAddress"] = transfer.OwnerAddress != null ? Convert.ToHexString(transfer.OwnerAddress.ToByteArray()).ToLowerInvariant().ReplaceFirst() : null,
                        ["amount"] = transfer.UnfreezeBalance,
                        ["symbol"] = CurrencyEnum.TRX.ToString(),
                        ["type"] = transfer.Resource == ResourceCode.Energy ? TransactionType.UnEnergy : TransactionType.UnBandwidth
                    };
                    return JsonSerializer.Serialize(result, options);
                }
                if (param.Is(WithdrawBalanceContract.Descriptor))
                {
                    var transfer = param.Unpack<WithdrawBalanceContract>();
                    var result = new Dictionary<string, object?>
                    {
                        ["fromAddress"] = transfer.OwnerAddress != null ? Convert.ToHexString(transfer.OwnerAddress.ToByteArray()).ToLowerInvariant().ReplaceFirst() : null,
                        ["symbol"] = CurrencyEnum.TRX.ToString(),
                        ["type"] = TransactionType.WithdrawTRX
                    };
                    return JsonSerializer.Serialize(result, options);
                }
                if (param.Is(DelegateResourceContract.Descriptor))
                {
                    var transfer = param.Unpack<DelegateResourceContract>();
                    var result = new Dictionary<string, object?>
                    {
                        ["fromAddress"] = transfer.OwnerAddress != null ? Convert.ToHexString(transfer.OwnerAddress.ToByteArray()).ToLowerInvariant().ReplaceFirst() : null,
                        ["toAddress"] = transfer.ReceiverAddress != null ? Convert.ToHexString(transfer.ReceiverAddress.ToByteArray()).ToLowerInvariant().ReplaceFirst() : null,
                        ["amount"] = transfer.Balance,
                        ["symbol"] = CurrencyEnum.TRX.ToString(),
                        ["type"] = transfer.Resource == ResourceCode.Energy ? TransactionType.DelegateEnergy : TransactionType.DelegateBandwidth
                    };
                    return JsonSerializer.Serialize(result, options);
                }
                if (param.Is(UnDelegateResourceContract.Descriptor))
                {
                    var transfer = param.Unpack<UnDelegateResourceContract>();
                    var result = new Dictionary<string, object?>
                    {
                        ["fromAddress"] = transfer.OwnerAddress != null ? Convert.ToHexString(transfer.OwnerAddress.ToByteArray()).ToLowerInvariant().ReplaceFirst() : null,
                        ["toAddress"] = transfer.ReceiverAddress != null ? Convert.ToHexString(transfer.ReceiverAddress.ToByteArray()).ToLowerInvariant().ReplaceFirst() : null,
                        ["amount"] = transfer.Balance,
                        ["symbol"] = CurrencyEnum.TRX.ToString(),
                        ["type"] = transfer.Resource == ResourceCode.Energy ? TransactionType.UnDelegateEnergy : TransactionType.UnDelegateBandwidth
                    };
                    return JsonSerializer.Serialize(result, options);
                }
                if (param.Is(CancelAllUnfreezeV2Contract.Descriptor))
                {
                    var transfer = param.Unpack<CancelAllUnfreezeV2Contract>();
                    var result = new Dictionary<string, object?>
                    {
                        ["fromAddress"] = transfer.OwnerAddress != null ? Convert.ToHexString(transfer.OwnerAddress.ToByteArray()).ToLowerInvariant().ReplaceFirst() : null,
                        ["symbol"] = CurrencyEnum.TRX.ToString(),
                        ["type"] = TransactionType.CancelAllUnfreezeV2
                    };
                    return JsonSerializer.Serialize(result, options);
                }
                if (param.Is(WithdrawExpireUnfreezeContract.Descriptor))
                {
                    var transfer = param.Unpack<WithdrawExpireUnfreezeContract>();
                    var result = new Dictionary<string, object?>
                    {
                        ["fromAddress"] = transfer.OwnerAddress != null ? Convert.ToHexString(transfer.OwnerAddress.ToByteArray()).ToLowerInvariant().ReplaceFirst() : null,
                        ["symbol"] = CurrencyEnum.TRX.ToString(),
                        ["type"] = TransactionType.WithdrawExpireUnfreeze
                    };
                    return JsonSerializer.Serialize(result, options);
                }
            }
            catch
            {
                //忽略解包失败并继续执行
            }

            try
            {
                var text = param.Value.ToStringUtf8();
                if (!string.IsNullOrWhiteSpace(text)) return text;
            }
            catch
            {
                Console.WriteLine("无法将参数解码为 JSON, 回退到十六进制");
            }

            return Convert.ToHexString(param.Value.ToByteArray()).ToLowerInvariant();
        }

        private static object DecodeTriggerSmartContract(TriggerSmartContract sc)
        {
            var result = new Dictionary<string, object?>
            {
                ["fromAddress"] = sc.OwnerAddress != null ? Convert.ToHexString(sc.OwnerAddress.ToByteArray()).ToLowerInvariant().ReplaceFirst() : null
            };

            // 识别合约地址（用于区分 USDT、USDC 等不同的 token）
            var contractAddressHex = sc.ContractAddress != null ? Convert.ToHexString(sc.ContractAddress.ToByteArray()).ToLowerInvariant() : null;
            var contractAddressTron = contractAddressHex != null ? ("41" + contractAddressHex).ReplaceFirst() : null;

            if (contractAddressTron == "TR7NHqjeKQxGTCi8q8ZY4pL8otSzgjLj6t")
            {
                var data = sc.Data?.ToByteArray() ?? [];

                var selector = Convert.ToHexString(data.AsSpan(0, 4)).ToLowerInvariant();
                if (selector == "a9059cbb") // transfer(address,uint256)
                {
                    if (data.Length < 4 + 32 + 32) result["error"] = "data too short";

                    var toBytes32 = data.AsSpan(4, 32).ToArray();
                    var valueBytes32 = data.AsSpan(36, 32).ToArray();
                    var to20 = toBytes32.Skip(12).Take(20).ToArray();
                    var tronHex = "41" + Convert.ToHexString(to20).ToLowerInvariant();
                    var amount = BigIntegerFromUnsignedBigEndian(valueBytes32);

                    result["toAddress"] = tronHex.ReplaceFirst();
                    result["amount"] = amount.ToString();
                    result["symbol"] = CurrencyEnum.USDT.ToString();
                    result["type"] = TransactionType.USDT;
                }
            }

            return result;
        }

        private static BigInteger BigIntegerFromUnsignedBigEndian(byte[] bytes)
        {
            if (bytes == null || bytes.Length == 0) return BigInteger.Zero;
            // Convert big-endian unsigned bytes to Little-endian with extra 0 byte to force positive
            var le = bytes.Reverse().ToArray().Concat(new byte[] { 0 }).ToArray();
            return new BigInteger(le);
        }
        #endregion

        public static string MD5(this string text)
        {
            return text.MD5(Encoding.UTF8);
        }

        public static string MD5(this string text, Encoding encoding)
        {
            if (text == null)
            {
                throw new ArgumentNullException("text");
            }

            MD5CryptoServiceProvider mD5CryptoServiceProvider = new MD5CryptoServiceProvider();
            byte[] array = mD5CryptoServiceProvider.ComputeHash(encoding.GetBytes(text));
            mD5CryptoServiceProvider.Clear();
            StringBuilder stringBuilder = new StringBuilder();
            for (int i = 0; i < array.Length; i++)
            {
                stringBuilder.Append(array[i].ToString("x2"));
            }

            return stringBuilder.ToString();
        }
    }
}
