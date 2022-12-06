using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.BotAPI;
using Telegram.BotAPI.AvailableMethods;
using Telegram.BotAPI.AvailableMethods.FormattingOptions;
using Telegram.BotAPI.AvailableTypes;

namespace ProxmoxControl.Telegram
{
    public static class ApiHelpers
    {
        public static Message ReplyToMessageForceReply(this BotClient tg, Message message, string text)
        {
            return tg.SendMessage(new SendMessageArgs(message.Chat.Id, text) { ReplyToMessageId = message.MessageId, ParseMode = ParseMode.HTML, 
                ReplyMarkup = new ForceReply() });
        }

        public static Message ReplyToMessage(this BotClient tg, Message message, string text)
        {
            return tg.SendMessage(new SendMessageArgs(message.Chat.Id, text) { ReplyToMessageId = message.MessageId, ParseMode = ParseMode.HTML });
        }
    }
}
