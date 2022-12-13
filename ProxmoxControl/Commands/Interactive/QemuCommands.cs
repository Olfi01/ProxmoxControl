using Corsinvest.ProxmoxVE.Api;
using Corsinvest.ProxmoxVE.Api.Shared.Models.Node;
using ProxmoxControl.Commands.Pve;
using ProxmoxControl.Telegram;
using System.Text.RegularExpressions;
using Telegram.BotAPI;
using Telegram.BotAPI.AvailableTypes;

namespace ProxmoxControl.Commands.Interactive
{
    [Commands]
    public class QemuCommands
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        internal static void SendQemuVms(int page, string node, Message message, BotClient tg, PveClient pve)
        {
            pve.Nodes[node].Qemu.GetSorted(full: true).ContinueWith(task =>
            {
                if (task.IsCompletedSuccessfully)
                {
                    IEnumerable<NodeVmQemu> vms = task.Result;
                    ReplyKeyboardMarkup replyMarkup = KeyboardHelper.GetReplyMarkupPage(vms.Select(vm => $"{vm.VmId}@{node.HtmlEscape()}"), page);
                    Message sent = tg.ReplyToMessageWithKeyboard(message, MessageHelper.GetQemuVmsMessage(node, vms, page), replyMarkup);
                    Program.AddListener(new ReplyListener(sent, "select_qemu_vm"));
                }
                else
                {
                    tg.ReplyToMessage(message, "Something went wrong contacting your server. Please check its availability.");
                }
            });
        }
        private static readonly Regex qemuRegex = new(@"^(/qemu(@\S+bot)? )?(?<node>.+)");
        [Command("/qemu")]
        [Listener("qemu")]
        public static bool Qemu(Message message, BotClient tg)
        {
            if (!BotCommands.EnsureProxmoxContext(message, tg, out PveClient pve)) return true;
            if (message.Text == null) return false;
            Match match = qemuRegex.Match(message.Text);
            if (!match.Success)
            {
                Logger.Error("Failed to match regex in Qemu command.");
                return false;
            }
            string node = match.Groups["node"].Value;
            pve.Nodes[node].Index().ContinueWith(task =>
            {
                if (task.IsCompletedSuccessfully && task.Result.IsSuccessStatusCode)
                {
                    SendQemuVms(0, node, message, tg, pve);
                }
                else
                {
                    Message sent = tg.ReplyToMessageForceReply(message, "I don't recognize this node. Please try again.");
                    Program.AddListener(new ReplyListener(sent, "qemu"));
                }
            });
            return true;
        }

        private static void SendQemuOptions(int page, string node, int vmid, Message message, BotClient tg, PveClient pve)
        {
            IEnumerable<QemuOption> options = Enum.GetValues(typeof(QemuOption)).Cast<QemuOption>();
            ReplyKeyboardMarkup replyMarkup = KeyboardHelper.GetReplyMarkupPage(options.Select(option => Enum.GetName(option) ?? option.ToString()), page);
            Message sent = tg.ReplyToMessageWithKeyboard(message, MessageHelper.GetQemuOptionsMessage(node, vmid, options, page), replyMarkup);
            Program.AddListener(new ReplyListener(sent, "select_qemu_option"));
        }
        private static readonly Regex selectQemuRegex = new(@"^(/show_vm(@\S+bot)? )?(?<vmid>\d+)@(?<node>.+)");
        private static readonly Regex selectQemuReplyRegex = new(@"Qemu VMs on (?<node>.+) \(Page (?<page>\d+)\)");
        [Command("/show_vm")]
        [Listener("select_qemu_vm")]
        public static bool SelectQemuVm(Message message, BotClient tg)
        {
            if (!BotCommands.EnsureProxmoxContext(message, tg, out PveClient pve)) return true;
            if (message.Text == null) return false;
            string text = message.Text;
            int page = 0;
            string node = "";
            if (message.ReplyToMessage?.Text != null)
            {
                Match match1 = selectQemuReplyRegex.Match(message.ReplyToMessage.Text);
                if (match1.Success && int.TryParse(match1.Groups["page"].Value, out page))
                {
                    page--; // convert human readable page to 0-based index
                    node = match1.Groups["node"].Value;
                }
            }
            if (text == KeyboardHelper.ArrowLeft)
            {
                SendQemuVms(page - 1, node, message, tg, pve);
                return true;
            }
            else if (text == KeyboardHelper.ArrowRight)
            {
                SendQemuVms(page + 1, node, message, tg, pve);
                return true;
            }
            Match match = selectQemuRegex.Match(text);
            if (!match.Success)
            {
                Message sent = tg.ReplyToMessageForceReply(message, "I don't recognize this VM. Please try again.");
                Program.AddListener(new ReplyListener(sent, "select_qemu_vm"));
                return true;
            }
            int vmid = int.Parse(match.Groups["vmid"].Value);
            node = match.Groups["node"].Value;
            pve.Nodes[node].Qemu[vmid].Vmdiridx().ContinueWith(task =>
            {
                if (task.IsCompletedSuccessfully && task.Result.IsSuccessStatusCode)
                {
                    SendQemuOptions(page, node, vmid, message, tg, pve);
                }
                else
                {
                    Message sent = tg.ReplyToMessageForceReply(message, "I don't recognize this VM. Please try again.");
                    SendQemuVms(page, node, message, tg, pve);
                }
            });
            return true;
        }

        private static readonly Regex selectQemuOptionRegex = new(@"^(?<vmid>\d+)@(?<node>.+) \(Page (?<page>\d+)\)");
        [Listener("select_qemu_option")]
        public static bool SelectQemuOption(Message message, BotClient tg)
        {
            if (!BotCommands.EnsureProxmoxContext(message, tg, out PveClient pve)) return true;
            if (message.Text == null) return false;
            string text = message.Text;
            Match match;
            int vmid;
            int page;
            if (message.ReplyToMessage?.Text == null 
                || !(match = selectQemuOptionRegex.Match(text)).Success
                || !int.TryParse(match.Groups["vmid"].Value, out vmid)
                || !int.TryParse(match.Groups["page"].Value, out page))
            {
                Logger.Error("Failed to match regex in SelectQemuOption");
                return false;
            }
            page--; // convert human readable page to 0-based index
            string node = match.Groups["node"].Value;
            if (text == KeyboardHelper.ArrowLeft)
            {
                SendQemuOptions(page - 1, node, vmid, message, tg, pve);
                return true;
            }
            else if (text == KeyboardHelper.ArrowRight)
            {
                SendQemuOptions(page + 1, node, vmid, message, tg, pve);
                return true;
            }
            try
            {
                QemuOption option = Enum.Parse<QemuOption>(text);
                switch (option)
                {
                    case QemuOption.Status:
                        QemuStatusCommands.QemuStatus(0, node, vmid, message, tg, pve);
                        return true;
                    default:
                        throw new NotImplementedException($"Missing Qemu option {option}!");
                }
            } 
            catch (ArgumentException)
            {
                tg.ReplyToMessage(message, "I don't recognize this option. Please try again.");
                SendQemuOptions(page, node, vmid, message, tg, pve);
                return true;
            }
        }
    }
}
