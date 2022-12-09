using Corsinvest.ProxmoxVE.Api;
using Corsinvest.ProxmoxVE.Api.Extension;
using Corsinvest.ProxmoxVE.Api.Shared.Models.Node;
using Newtonsoft.Json;
using ProxmoxControl.Telegram;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Telegram.BotAPI;
using Telegram.BotAPI.AvailableTypes;

namespace ProxmoxControl.Commands.Interactive
{
    [Commands]
    public class NodeCommands
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        private static void SendNodesMessage(int page, Message message, BotClient tg, PveClient pve)
        {
            pve.Nodes.Get().ContinueWith(task =>
            {
                if (task.IsCompletedSuccessfully)
                {
                    IEnumerable<NodeItem> nodes = task.Result;
                    ReplyKeyboardMarkup replyMarkup = KeyboardHelper.GetReplyMarkupPage(nodes.Select(node => node.Node), page);
                    Message sent = tg.ReplyToMessageWithKeyboard(message, MessageHelper.GetNodesMessage(nodes, page), replyMarkup);
                    Program.AddListener(new ReplyListener(sent, "select_node"));
                }
                else
                {
                    tg.ReplyToMessage(message, "Something went wrong contacting your server. Please check its availability.");
                }
            });
        }
        [Command("/browse")]
        [Command("/nodes")]
        [Listener("list_nodes")]
        public static bool Browse(Message message, BotClient tg)
        {
            if (!BotCommands.EnsureProxmoxContext(message, tg, out PveClient pve)) return true;
            SendNodesMessage(0, message, tg, pve);
            return true;
        }

        private static void SendNodeOptions(int page, string node, Message message, BotClient tg, PveClient pve)
        {
            IEnumerable<NodeOption> options = Enum.GetValues(typeof(NodeOption)).Cast<NodeOption>();
            ReplyKeyboardMarkup replyMarkup = KeyboardHelper.GetReplyMarkupPage(options.Select(option => Enum.GetName(option) ?? option.ToString()), page);
            Message sent = tg.ReplyToMessageWithKeyboard(message, MessageHelper.GetNodeOptionsMessage(node, options, page), replyMarkup);
            Program.AddListener(new ReplyListener(sent, "select_node_option"));
        }
        private static readonly Regex selectNodeRegex = new(@"^Nodes \(Page (?<page>\d+)\)");
        [Listener("select_node")]
        public static bool SelectNode(Message message, BotClient tg)
        {
            if (!BotCommands.EnsureProxmoxContext(message, tg, out PveClient pve)) return true;
            if (message.Text == null) return false;
            string text = message.Text;
            Match match;
            if (message.ReplyToMessage?.Text == null
                || !(match = selectNodeRegex.Match(message.ReplyToMessage.Text)).Success
                || !int.TryParse(match.Groups["page"].Value, out int page))
            {
                Logger.Error("Listener select_node got called with an invalid ReplyToMessage: {0}",
                    JsonConvert.SerializeObject(message.ReplyToMessage));
                return true;
            }
            page--; // go from user-readable to 0-based index
            if (text == KeyboardHelper.ArrowLeft)
            {
                SendNodesMessage(page - 1, message, tg, pve);
                return true;
            }
            else if (text == KeyboardHelper.ArrowRight)
            {
                SendNodesMessage(page + 1, message, tg, pve);
                return true;
            }
            else
            {
                pve.Nodes[text].Index().ContinueWith(task =>
                {
                    if (task.IsCompletedSuccessfully && task.Result.IsSuccessStatusCode)
                    {
                        SendNodeOptions(0, text, message, tg, pve);
                    }
                    else
                    {
                        tg.ReplyToMessage(message, "I don't recognize this node. Please try again.");
                        SendNodesMessage(page, message, tg, pve);
                    }
                });

                return true;
            }
        }

        private static readonly Regex selectNodeOptionRegex = new(@"^(?<node>.+) \(Page (?<page>\d+)\)");
        [Listener("select_node_option")]
        public static bool SelectNodeOption(Message message, BotClient tg)
        {
            if (!BotCommands.EnsureProxmoxContext(message, tg, out PveClient pve)) return true;
            if (message.Text == null) return false;
            string text = message.Text;
            Match match;
            if (message.ReplyToMessage?.Text == null
                || !(match = selectNodeRegex.Match(message.ReplyToMessage.Text)).Success
                || !int.TryParse(match.Groups["page"].Value, out int page))
            {
                Logger.Error("Listener select_node_option got called with an invalid ReplyToMessage: {0}",
                    JsonConvert.SerializeObject(message.ReplyToMessage));
                return true;
            }
            page--; // go from user-readable to 0-based index
            string node = match.Groups["node"].Value;
            if (text == KeyboardHelper.ArrowLeft)
            {
                SendNodeOptions(page - 1, node, message, tg, pve);
                return true;
            }
            else if (text == KeyboardHelper.ArrowRight)
            {
                SendNodeOptions(page + 1, node, message, tg, pve);
                return true;
            }
            else try
                {
                    NodeOption option = Enum.Parse<NodeOption>(text);
                    switch (option)
                    {
                        case NodeOption.Qemu:
                            QemuCommands.SendQemuVms(0, node, message, tg, pve);
                            return true;
                        case NodeOption.Lxc:
                            Lxc(node, message, tg, pve);
                            return true;
                        case NodeOption.Services:
                            Services(node, message, tg, pve);
                            return true;
                        default:
                            throw new NotImplementedException($"Missing option {option}!");
                    }
                }
                catch (ArgumentException)
                {
                    tg.ReplyToMessage(message, "I don't recognize this option. Please try again.");
                    SendNodeOptions(page, node, message, tg, pve);
                    return true;
                }
        }
    }
}
