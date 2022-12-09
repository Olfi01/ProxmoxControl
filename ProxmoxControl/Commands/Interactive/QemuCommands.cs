using Corsinvest.ProxmoxVE.Api;
using Corsinvest.ProxmoxVE.Api.Extension;
using Corsinvest.ProxmoxVE.Api.Shared.Models.Node;
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
    public class QemuCommands
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        internal static void SendQemuVms(int page, string node, Message message, BotClient tg, PveClient pve)
        {
            pve.Nodes[node].Qemu.Get(full: true).ContinueWith(task =>
            {
                if (task.IsCompletedSuccessfully)
                {
                    IEnumerable<NodeVmQemu> vms = task.Result;
                    ReplyKeyboardMarkup replyMarkup = KeyboardHelper.GetReplyMarkupPage(vms.Select(vm => vm.VmId.ToString()), page);
                    Message sent = tg.ReplyToMessageWithKeyboard(message, MessageHelper.GetQemuVmsMessage(node, vms, page), replyMarkup);
                    Program.AddListener(new ReplyListener(sent, "select_qemu_vm")); // TODO build listener
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
    }
}
