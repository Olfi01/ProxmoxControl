using Corsinvest.ProxmoxVE.Api;
using Corsinvest.ProxmoxVE.Api.Extension;
using Corsinvest.ProxmoxVE.Api.Shared.Models.Node;
using Newtonsoft.Json;
using ProxmoxControl.Data;
using ProxmoxControl.Telegram;
using System.Reflection;
using System.Text.RegularExpressions;
using Telegram.BotAPI;
using Telegram.BotAPI.AvailableMethods;
using Telegram.BotAPI.AvailableTypes;
using Telegram.BotAPI.GettingUpdates;

namespace ProxmoxControl.Commands
{
    public class BotCommands
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private static readonly ProxmoxControlDbContext db = ProxmoxControlDbContext.Instance;
        private delegate bool CommandMethod(Message message, BotClient tg);

        public static void Init()
        {
            foreach (Type type in Assembly.GetExecutingAssembly().GetTypes())
            {
                if (type.GetCustomAttribute<CommandsAttribute>() == null) continue;
                var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Static);
                foreach (var method in methods)
                {
                    try
                    {
                        Delegate commandDelegate = Delegate.CreateDelegate(typeof(CommandMethod), method);
                        if (commandDelegate != null)
                        {
                            IEnumerable<CommandAttribute> commandAttributes = method.GetCustomAttributes<CommandAttribute>();
                            foreach (CommandAttribute attribute in commandAttributes)
                            {
                                commands.Add(attribute.Command, (CommandMethod)commandDelegate);
                            }
                            IEnumerable<ListenerAttribute> listenerAttributes = method.GetCustomAttributes<ListenerAttribute>();
                            foreach (ListenerAttribute attribute in listenerAttributes)
                            {
                                listeners.Add(attribute.ListenerID, (CommandMethod)commandDelegate);
                            }
                        }
                    }
                    catch { }
                }
            }
        }

        private static readonly Dictionary<string, CommandMethod> commands = new();
        private static readonly Dictionary<string, CommandMethod> listeners = new();

        public static void HandleCommand(string command, Message message, BotClient tg)
        {
            if (!commands.ContainsKey(command))
            {
                Logger.Warn("Command '{0}' not found!", command);
                return;
            }
            commands[command].Invoke(message, tg);
        }

        public static bool HandleListener(string listenerID, Message message, BotClient tg)
        {
            if (!listeners.ContainsKey(listenerID))
            {
                Logger.Error("Tried to invoke listener '{0}' but didn't find it!", listenerID);
                return true;
            }
            return listeners[listenerID].Invoke(message, tg);
        }

        public static bool EnsureProxmoxContext(Message message, BotClient tg, out PveClient pve)
        {
            RegisteredChat? registeredChat = db.RegisteredChats.Find(message.Chat.Id);
            if (registeredChat == null)
            {
                tg.ReplyToMessage(message, "This chat isn't registered yet! To register a chat, send /start.");
                pve = default!;
                return false;
            }
            if (registeredChat.ProxmoxHost == null || registeredChat.ProxmoxApiToken == null)
            {
                tg.ReplyToMessage(message, "Your host URL and API key aren't configured yet. Please set them using /config.");
                pve = default!;
                return false;
            }
            pve = new(registeredChat.ProxmoxHost)
            {
                ApiToken = registeredChat.ProxmoxApiToken
            };
            return true;
        }
    }
}