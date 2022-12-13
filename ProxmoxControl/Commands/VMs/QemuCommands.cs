using Corsinvest.ProxmoxVE.Api;
using ProxmoxControl.Telegram;
using System.Text.RegularExpressions;
using Telegram.BotAPI;
using Telegram.BotAPI.AvailableTypes;

namespace ProxmoxControl.Commands.VMs
{
    [Commands]
    public class QemuCommands
    {
        private static readonly Regex vmIdRegex = new(@"^(((/startvm)|(/stopvm))(@\S+bot)? )?(?<vmid>\d+)@(?<node>.*)$", RegexOptions.IgnoreCase);
        [Command("/startvm")]
        [Listener("start_vm")]
        public static bool StartVM(Message message, BotClient tg)
        {
            if (!BotCommands.EnsureProxmoxContext(message, tg, out PveClient pve)) return true;
            if (message.Text == null) return false;
            Match match = vmIdRegex.Match(message.Text);
            if (!match.Success)
            {
                Message sent = tg.ReplyToMessageForceReply(message, "I don't recognize this VM. Please try again.");
                Program.AddListener(new ReplyListener(sent, "start_vm"));
                return true;
            }
            string node = match.Groups["node"].Value;
            int vmid = int.Parse(match.Groups["vmid"].Value);
            tg.ReplyToMessage(message, $"Trying to start VM {vmid} on node {node}...");
            pve.Nodes[node].Qemu[vmid].Status.Start.VmStart().ContinueWith(task =>
            {
                if (task.Status == TaskStatus.RanToCompletion && task.Result.IsSuccessStatusCode)
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
            if (!BotCommands.EnsureProxmoxContext(message, tg, out PveClient pve)) return true;
            if (message.Text == null) return false;
            Match match = vmIdRegex.Match(message.Text);
            if (!match.Success)
            {
                Message sent = tg.ReplyToMessageForceReply(message, "I don't recognize this VM. Please try again.");
                Program.AddListener(new ReplyListener(sent, "stop_vm"));
                return true;
            }
            string node = match.Groups["node"].Value;
            int vmid = int.Parse(match.Groups["vmid"].Value);
            tg.ReplyToMessage(message, $"Trying to stop VM {vmid} on node {node}...");
            pve.Nodes[node].Qemu[vmid].Status.Stop.VmStop().ContinueWith(task =>
            {
                if (task.Status == TaskStatus.RanToCompletion && task.Result.IsSuccessStatusCode)
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
