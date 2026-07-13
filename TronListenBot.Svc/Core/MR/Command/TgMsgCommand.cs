using MediatR;
using Telegram.Bot.Types;

namespace TronListenBot.Svc.Core.MR.Command
{
    public class TgMsgCommand : IRequest
    {
        public Update Update { get; set; }
        public string UserName { get; set; }
    }
}
