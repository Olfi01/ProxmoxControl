using ProxmoxControl.Data;
using ProxmoxControl.Telegram;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Telegram.BotAPI;
using Telegram.BotAPI.AvailableTypes;

namespace ProxmoxControl.Commands.Config
{
    [Commands]
    public class SetupCommands
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private static readonly ProxmoxControlDbContext db = ProxmoxControlDbContext.Instance;

        [Command("/start")]
        [Command("/config")]
        public static bool RegisterChat(Message message, BotClient tg)
        {
            if (db.RegisteredChats.Find(message.Chat.Id) == null)
            {
                db.RegisteredChats.Add(new RegisteredChat(message.Chat.Id));
                db.SaveChanges();
            }
            Message sent = tg.ReplyToMessageForceReply(message, "Send me the address where I can access your Proxmox VE.");
            Program.AddListener(new ReplyListener(sent, "set_proxmox_url"));
            return true;
        }

        private static readonly Regex urlMessageRegex = new(@"^(/seturl(@\S+bot)? )?(?<url>.*)$");
        [Command("/seturl")]
        [Listener("set_proxmox_url")]
        public static bool SetUrl(Message message, BotClient tg)
        {
            RegisteredChat? registeredChat = db.RegisteredChats.Find(message.Chat.Id);
            if (registeredChat == null)
            {
                tg.ReplyToMessage(message, "This chat isn't registered yet! To register a chat, send /start.");
                return true;
            }
            if (message.Text == null) return false;
            Match match = urlMessageRegex.Match(message.Text);
            if (!match.Success)
            {
                Logger.Error("Failed to match regex in SetProxmoxUrl.");
                return false;
            }
            string url = match.Groups["url"].Value;
            if (Uri.CheckHostName(url) == UriHostNameType.Unknown)
            {
                Message sent = tg.ReplyToMessageForceReply(message, "That isn't a valid host name. Try again.");
                Program.AddListener(new ReplyListener(sent, "set_proxmox_url"));
                return true;
            }
            registeredChat.ProxmoxHost = url;
            db.SaveChanges();
            Message response = tg.ReplyToMessageForceReply(message, "Got it! Now please send me your API token in the format <code>USER@REALM!TOKENID=TOKEN</code>.");
            Program.AddListener(new ReplyListener(response, "set_proxmox_api_key"));
            return true;
        }

        private static readonly Regex proxmoxApiKeyRegex =
            new(@"^(?<token>.+@.+!.+=[0-9a-fA-F]{8}\b-[0-9a-fA-F]{4}\b-[0-9a-fA-F]{4}\b-[0-9a-fA-F]{4}\b-[0-9a-fA-F]{12})$");
        [Listener("set_proxmox_api_key")]
        public static bool ConfigStep2(Message message, BotClient tg)
        {
            RegisteredChat? registeredChat = db.RegisteredChats.Find(message.Chat.Id);
            if (registeredChat == null)
            {
                tg.ReplyToMessage(message, "This chat isn't registered yet! To register a chat, send /start.");
                return true;
            }
            if (message.Text == null) return false;
            Match match = proxmoxApiKeyRegex.Match(message.Text);
            if (!match.Success)
            {
                Message sent = tg.ReplyToMessageForceReply(message, "That doesn't look like a valid API key. Try again.");
                Program.AddListener(new ReplyListener(sent, "set_proxmox_api_key"));
                return true;
            }
            registeredChat.ProxmoxApiToken = match.Groups["token"].Value;
            db.SaveChanges();
            tg.ReplyToMessageClearKeys(message, "Okay, I'm all set up!");
            return true;
        }
    }
}
