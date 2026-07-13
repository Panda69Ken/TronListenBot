using TronListenBot.Svc.Core.Model;

namespace TronListenBot.Svc.Core.Service
{
    public interface IConfigService
    {
        public TgConfig TgConfig { get; }
        public TronConfig TronConfig { get; }
    }

    public class ConfigService(IConfiguration configuration) : IConfigService
    {
        private readonly IConfiguration _configuration = configuration;


        public string GetSetting(string name)
        {
            var value = _configuration.GetSection(name).Value;
            if (value == null)
            {
                return string.Empty;
            }
            return value;
        }
        public T GetSettingT<T>(string name)
        {
            var value = _configuration.GetSection(name).Get<T>();
            if (value == null)
            {
                return default;
            }
            return value;
        }

        public TgConfig TgConfig => GetSettingT<TgConfig>("TgConfig");

        public TronConfig TronConfig => GetSettingT<TronConfig>("TronConfig");
    }


}
