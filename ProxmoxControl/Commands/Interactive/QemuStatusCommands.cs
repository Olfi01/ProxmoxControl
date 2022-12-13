using Corsinvest.ProxmoxVE.Api;
using Corsinvest.ProxmoxVE.Api.Extension;
using Corsinvest.ProxmoxVE.Api.Shared.Models.Vm;
using ProxmoxControl.Telegram;
using System.Text.RegularExpressions;
using Telegram.BotAPI;
using Telegram.BotAPI.AvailableTypes;

namespace ProxmoxControl.Commands.Interactive
{
    public class QemuStatusCommands
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        internal static void QemuStatus(int page, string node, int vmid, Message message, BotClient tg, PveClient pve)
        {
            pve.Nodes[node].Qemu[vmid].Status.Current.Get().ContinueWith(task =>
            {
                if (task.IsCompletedSuccessfully)
                {
                    VmQemuStatusCurrent status = task.Result;
                    IEnumerable<QemuStatusOption> options = Enum.GetValues(typeof(QemuStatusOption)).Cast<QemuStatusOption>();
                    ReplyKeyboardMarkup replyMarkup = KeyboardHelper.GetReplyMarkupPage(options.Select(option => Enum.GetName(option) ?? option.ToString()), page);
                    Message sent = tg.ReplyToMessageWithKeyboard(message, MessageHelper.GetQemuStatusOptionsMessage(node, vmid, status, options, page), replyMarkup);
                    Program.AddListener(new ReplyListener(sent, "select_qemu_status_option"));
                }
                else
                {
                    tg.ReplyToMessage(message, "Something went wrong contacting your server. Please check its availability.");
                }
            });
        }

        private static readonly Regex selectQemuStatusOptionRegex = new(@"^(?<vmid>\d+)@(?<node>.+) Status \(Page (?<page>\d+)\)");
        [Listener("select_qemu_status_option")]
        public static bool SelectQemuStatusOption(Message message, BotClient tg)
        {
            if (!BotCommands.EnsureProxmoxContext(message, tg, out PveClient pve)) return true;
            if (message.Text == null) return false;
            string text = message.Text;
            Match match;
            int vmid;
            int page;
            if (message.ReplyToMessage?.Text == null
                || !(match = selectQemuStatusOptionRegex.Match(text)).Success
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
                QemuStatus(page - 1, node, vmid, message, tg, pve);
                return true;
            }
            else if (text == KeyboardHelper.ArrowRight)
            {
                QemuStatus(page + 1, node, vmid, message, tg, pve);
                return true;
            }
            try
            {
                QemuStatusOption option = Enum.Parse<QemuStatusOption>(text);
                switch (option)
                {
                    case QemuStatusOption.Start:
                        Start(node, vmid, message, tg, pve);
                        return true;
                    case QemuStatusOption.Shutdown:
                        Shutdown(node, vmid, message, tg, pve);
                        return true;
                    case QemuStatusOption.Stop:
                        Stop(node, vmid, message, tg, pve);
                        return true;
                    case QemuStatusOption.Reboot:
                        Reboot(node, vmid, message, tg, pve);
                        return true;
                    case QemuStatusOption.Reset:
                        Reset(node, vmid, message, tg, pve);
                        return true;
                    case QemuStatusOption.Suspend:
                        Suspend(node, vmid, message, tg, pve);
                        return true;
                    case QemuStatusOption.Resume:
                        Resume(node, vmid, message, tg, pve);
                        return true;
                    default:
                        throw new NotImplementedException($"Missing Qemu status option {option}!");
                }
            }
            catch (ArgumentException)
            {
                tg.ReplyToMessage(message, "I don't recognize this option. Please try again.");
                QemuStatus(page, node, vmid, message, tg, pve);
                return true;
            }
        }

        private static void Start(string node, int vmid, Message message, BotClient tg, PveClient pve)
        {
            tg.ReplyToMessage(message, $"Trying to start {vmid}@{node.HtmlEscape()}...");
            pve.Nodes[node].Qemu[vmid].Status.Start.VmStart().ContinueWith(task =>
            {
                if (task.IsCompletedSuccessfully && task.Result.IsSuccessStatusCode)
                {
                    tg.ReplyToMessage(message, "VM started successfully!");
                    QemuStatus(0, node, vmid, message, tg, pve);
                } 
                else
                {
                    tg.ReplyToMessage(message, "Something went wrong contacting your server. Please check its availability.");
                }
            });
        }

        private static void Shutdown(string node, int vmid, Message message, BotClient tg, PveClient pve)
        {
            tg.ReplyToMessage(message, $"Trying to shutdown {vmid}@{node.HtmlEscape()}...");
            pve.Nodes[node].Qemu[vmid].Status.Shutdown.VmShutdown().ContinueWith(task =>
            {
                if (task.IsCompletedSuccessfully && task.Result.IsSuccessStatusCode)
                {
                    tg.ReplyToMessage(message, "VM shut down successfully!");
                    QemuStatus(0, node, vmid, message, tg, pve);
                }
                else
                {
                    tg.ReplyToMessage(message, "Something went wrong contacting your server. Please check its availability.");
                }
            });
        }

        private static void Stop(string node, int vmid, Message message, BotClient tg, PveClient pve)
        {
            tg.ReplyToMessage(message, $"Trying to stop {vmid}@{node.HtmlEscape()}...");
            pve.Nodes[node].Qemu[vmid].Status.Stop.VmStop().ContinueWith(task =>
            {
                if (task.IsCompletedSuccessfully && task.Result.IsSuccessStatusCode)
                {
                    tg.ReplyToMessage(message, "VM stopped successfully!");
                    QemuStatus(0, node, vmid, message, tg, pve);
                }
                else
                {
                    tg.ReplyToMessage(message, "Something went wrong contacting your server. Please check its availability.");
                }
            });
        }

        private static void Reboot(string node, int vmid, Message message, BotClient tg, PveClient pve)
        {
            tg.ReplyToMessage(message, $"Trying to reboot {vmid}@{node.HtmlEscape()}...");
            pve.Nodes[node].Qemu[vmid].Status.Reboot.VmReboot().ContinueWith(task =>
            {
                if (task.IsCompletedSuccessfully && task.Result.IsSuccessStatusCode)
                {
                    tg.ReplyToMessage(message, "VM rebooted successfully!");
                    QemuStatus(0, node, vmid, message, tg, pve);
                }
                else
                {
                    tg.ReplyToMessage(message, "Something went wrong contacting your server. Please check its availability.");
                }
            });
        }

        private static void Reset(string node, int vmid, Message message, BotClient tg, PveClient pve)
        {
            tg.ReplyToMessage(message, $"Trying to reset {vmid}@{node.HtmlEscape()}...");
            pve.Nodes[node].Qemu[vmid].Status.Reset.VmReset().ContinueWith(task =>
            {
                if (task.IsCompletedSuccessfully && task.Result.IsSuccessStatusCode)
                {
                    tg.ReplyToMessage(message, "VM reset successfully!");
                    QemuStatus(0, node, vmid, message, tg, pve);
                }
                else
                {
                    tg.ReplyToMessage(message, "Something went wrong contacting your server. Please check its availability.");
                }
            });
        }

        private static void Suspend(string node, int vmid, Message message, BotClient tg, PveClient pve)
        {
            tg.ReplyToMessage(message, $"Trying to suspend {vmid}@{node.HtmlEscape()}...");
            pve.Nodes[node].Qemu[vmid].Status.Suspend.VmSuspend().ContinueWith(task =>
            {
                if (task.IsCompletedSuccessfully && task.Result.IsSuccessStatusCode)
                {
                    tg.ReplyToMessage(message, "VM suspended successfully!");
                    QemuStatus(0, node, vmid, message, tg, pve);
                }
                else
                {
                    tg.ReplyToMessage(message, "Something went wrong contacting your server. Please check its availability.");
                }
            });
        }

        private static void Resume(string node, int vmid, Message message, BotClient tg, PveClient pve)
        {
            tg.ReplyToMessage(message, $"Trying to resume {vmid}@{node.HtmlEscape()}...");
            pve.Nodes[node].Qemu[vmid].Status.Resume.VmResume().ContinueWith(task =>
            {
                if (task.IsCompletedSuccessfully && task.Result.IsSuccessStatusCode)
                {
                    tg.ReplyToMessage(message, "VM resumed successfully!");
                    QemuStatus(0, node, vmid, message, tg, pve);
                }
                else
                {
                    tg.ReplyToMessage(message, "Something went wrong contacting your server. Please check its availability.");
                }
            });
        }
    }
}
