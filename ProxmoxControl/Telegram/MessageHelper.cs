using Corsinvest.ProxmoxVE.Api.Shared.Models.Node;
using Corsinvest.ProxmoxVE.Api.Shared.Models.Vm;
using ProxmoxControl.Commands;
using ProxmoxControl.Commands.Interactive;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProxmoxControl.Telegram
{
    public class MessageHelper
    {
        public const int ItemsPerPage = 5;

        private static void AppendOptions<T>(IEnumerable<T> options, int page, StringBuilder sb, Func<T, string> getName)
        {
            for (int i = page * ItemsPerPage; i < (page + 1) * ItemsPerPage; i++)
            {
                if (i >= options.Count()) break;
                T option = options.ElementAt(i);
                sb.AppendLine(getName(option));
            }
        }
        private static void AppendOptions<T>(IEnumerable<T> options, int page, StringBuilder sb) where T : struct, Enum
        {
            AppendOptions(options, page, sb, option => $"- {Enum.GetName(option)}");
        }

        public static string GetNodesMessage(IEnumerable<NodeItem> nodes, int page)
        {
            StringBuilder sb = new();
            sb.AppendLine($"<b>Nodes</b> (Page {page + 1})");
            AppendOptions(nodes, page, sb, node => $"<code>{node.Node}</code>: {node.Status}");
            return sb.ToString();
        }

        public static string GetNodeOptionsMessage(string node, IEnumerable<NodeOption> options, int page)
        {
            StringBuilder sb = new();
            sb.AppendLine($"<b>{node.HtmlEscape()}</b> (Page {page + 1})");
            AppendOptions(options, page, sb);
            return sb.ToString();
        }

        public static string GetQemuVmsMessage(string node, IEnumerable<NodeVmQemu> vms, int page)
        {
            StringBuilder sb = new();
            sb.AppendLine($"<b>Qemu VMs on {node.HtmlEscape()}</b> (Page {page + 1})");
            AppendOptions(vms, page, sb, vm => $"<code>{vm.VmId}@{node.HtmlEscape()}</code> ({vm.Name.HtmlEscape()}): {vm.Status}");
            return sb.ToString();
        }

        public static string GetQemuOptionsMessage(string node, int vmid, IEnumerable<QemuOption> options, int page)
        {
            StringBuilder sb = new();
            sb.AppendLine($"<b>{vmid}@{node.HtmlEscape()}</b> (Page {page + 1})");
            AppendOptions(options, page, sb);
            return sb.ToString();
        }

        public static string GetQemuStatusOptionsMessage(string node, int vmid, VmQemuStatusCurrent status, IEnumerable<QemuStatusOption> options, int page)
        {
            StringBuilder sb = new();
            sb.AppendLine($"<b>{vmid}@{node.HtmlEscape()} Status</b> (Page {page + 1})");
            if (!string.IsNullOrEmpty(status.Name))
                sb.AppendLine($"Name: {status.Name.HtmlEscape()}");
            sb.AppendLine($"Status: {status.Status}");
            sb.AppendLine($"GuestAgent: {status.Agent}");
            if (!string.IsNullOrWhiteSpace(status.CpuInfo))
                sb.AppendLine($"CPU: {status.CpuInfo.HtmlEscape()}");
            if (!string.IsNullOrWhiteSpace(status.MemoryInfo))
                sb.AppendLine($"Memory: {status.MemoryInfo.HtmlEscape()}");
            if (!string.IsNullOrWhiteSpace(status.RunningMachine))
                sb.AppendLine($"Running machine: {status.RunningMachine.HtmlEscape()}");
            if (!string.IsNullOrWhiteSpace(status.RunningQemu))
                sb.AppendLine($"Running Qemu version: {status.RunningQemu}");
            if (status.Uptime != 0)
                sb.AppendLine($"Uptime: {TimeSpan.FromSeconds(status.Uptime).ToString(@"d\.hh\:mm\:ss")}");
            AppendOptions(options, page, sb);
            return sb.ToString();
        }
    }
}
