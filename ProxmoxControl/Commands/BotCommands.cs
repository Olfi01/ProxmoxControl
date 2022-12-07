using Corsinvest.ProxmoxVE.Api;
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
            var type = typeof(BotCommands);
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
                } catch { }
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

        [Command("/start")]
        [Command("/register")]
        public static bool RegisterChat(Message message, BotClient tg)
        {
            if (db.RegisteredChats.Find(message.Chat.Id) == null)
            {
                db.RegisteredChats.Add(new RegisteredChat(message.Chat.Id));
                db.SaveChanges();
                Message sent = tg.ReplyToMessageForceReply(message, "Send me the address where I can access your Proxmox VE.");
                Program.AddListener(new ReplyListener(sent, "set_proxmox_url"));
            }
            return true;
        }

        private static readonly Regex urlMessageRegex = new(@"^(/seturl(@\S+bot)? )?(?<url>.*)$");
        [Command("/config")]
        [Listener("set_proxmox_url")]
        public static bool Config(Message message, BotClient tg)
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

        private static readonly Regex vmIdRegex = new(@"^(((/startvm)|(/stopvm))(@\S+bot)? )?(?<vmid>\d+)@(?<node>.*)$", RegexOptions.IgnoreCase);
        [Command("/startvm")]
        [Listener("start_vm")]
        public static bool StartVM(Message message, BotClient tg)
        {
            RegisteredChat? registeredChat = db.RegisteredChats.Find(message.Chat.Id);
            if (registeredChat == null)
            {
                tg.ReplyToMessage(message, "This chat isn't registered yet! To register a chat, send /start.");
                return true;
            }
            if (registeredChat.ProxmoxHost == null || registeredChat.ProxmoxApiToken == null)
            {
                tg.ReplyToMessage(message, "Your host URL and API key aren't configured yet. Please set them using /config.");
                return true;
            }
            if (message.Text == null) return false;
            Match match = vmIdRegex.Match(message.Text);
            if (!match.Success)
            {
                Message sent = tg.ReplyToMessageForceReply(message, "I don't recognize this VM. Please try again.");
                Program.AddListener(new ReplyListener(sent, "start_vm"));
                return true;
            }
            PveClient pve = new(registeredChat.ProxmoxHost)
            {
                ApiToken = registeredChat.ProxmoxApiToken
            };
            string node = match.Groups["node"].Value;
            int vmid = int.Parse(match.Groups["vmid"].Value);
            tg.ReplyToMessage(message, $"Trying to start VM {vmid} on node {node}...");
            pve.Nodes[node].Qemu[vmid].Status.Start.VmStart().ContinueWith(task =>
            {
                if (task.Result.IsSuccessStatusCode)
                {
                    tg.ReplyToMessageClearKeys(message, "Successfully started VM!");
                }
                else
                {
                    tg.ReplyToMessageClearKeys(message, "Failed to start VM.");
                }
            });
            return true;
        }

        [Command("/stopvm")]
        [Listener("stop_vm")]
        public static bool StopVM(Message message, BotClient tg)
        {
            RegisteredChat? registeredChat = db.RegisteredChats.Find(message.Chat.Id);
            if (registeredChat == null)
            {
                tg.ReplyToMessage(message, "This chat isn't registered yet! To register a chat, send /start.");
                return true;
            }
            if (registeredChat.ProxmoxHost == null || registeredChat.ProxmoxApiToken == null)
            {
                tg.ReplyToMessage(message, "Your host URL and API key aren't configured yet. Please set them using /config.");
                return true;
            }
            if (message.Text == null) return false;
            Match match = vmIdRegex.Match(message.Text);
            if (!match.Success)
            {
                Message sent = tg.ReplyToMessageForceReply(message, "I don't recognize this VM. Please try again.");
                Program.AddListener(new ReplyListener(sent, "stop_vm"));
                return true;
            }
            PveClient pve = new(registeredChat.ProxmoxHost)
            {
                ApiToken = registeredChat.ProxmoxApiToken
            };
            string node = match.Groups["node"].Value;
            int vmid = int.Parse(match.Groups["vmid"].Value);
            tg.ReplyToMessage(message, $"Trying to stop VM {vmid} on node {node}...");
            pve.Nodes[node].Qemu[vmid].Status.Stop.VmStop().ContinueWith(task =>
            {
                if (task.Result.IsSuccessStatusCode)
                {
                    tg.ReplyToMessageClearKeys(message, "Successfully stopped VM!");
                }
                else
                {
                    tg.ReplyToMessageClearKeys(message, "Failed to stop VM.");
                }
            });
            return true;
        }
    }
}