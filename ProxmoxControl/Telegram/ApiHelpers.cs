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

        public static Message ReplyToMessage(this BotClient tg, Message message, string text)
        {
            return tg.SendMessage(new SendMessageArgs(message.Chat.Id, text) { ReplyToMessageId = message.MessageId, ParseMode = ParseMode.HTML });
        }
        public static Message ReplyToMessageForceReply(this BotClient tg, Message message, string text)
        {
            return tg.SendMessage(new SendMessageArgs(message.Chat.Id, text)
            {
                ReplyToMessageId = message.MessageId,
                ParseMode = ParseMode.HTML,
                ReplyMarkup = new ForceReply { Selective = true }
            });
        }

        public static Message ReplyToMessageWithKeyboard(this BotClient tg, Message message, string text, ReplyKeyboardMarkup replyMarkup)
        {
            return tg.SendMessage(new SendMessageArgs(message.Chat.Id, text)
            {
                ReplyToMessageId = message.MessageId,
                ParseMode = ParseMode.HTML,
                ReplyMarkup = replyMarkup
            });
        }

        public static Message ReplyToMessageClearKeys(this BotClient tg, Message message, string text)
        {
            return tg.SendMessage(new SendMessageArgs(message.Chat.Id, text)
            {
                ReplyToMessageId = message.MessageId,
                ParseMode = ParseMode.HTML,
                ReplyMarkup = new ReplyKeyboardRemove() { Selective = true }
            });
        }

        public static string HtmlEscape(this string str)
        {
            return str.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");
        }
    }
}
