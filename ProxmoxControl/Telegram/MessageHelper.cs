using Corsinvest.ProxmoxVE.Api.Shared.Models.Node;
using ProxmoxControl.Commands;
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

        public static string GetNodesMessage(IEnumerable<NodeItem> nodes, int page)
        {
            StringBuilder sb = new();
            sb.AppendLine($"<b>Nodes</b> (Page {page + 1})");
            for (int i = page * ItemsPerPage; i < (page + 1) * ItemsPerPage; i++)
            {
                if (i >= nodes.Count()) break;
                NodeItem node = nodes.ElementAt(i);
                sb.AppendLine($"<code>{node.Node}</code>: {node.Status}");
            }
            return sb.ToString();
        }

        public static string GetNodeOptionsMessage(string node, IEnumerable<NodeOption> options, int page)
        {
            StringBuilder sb = new();
            sb.AppendLine($"<b>{node.HtmlEscape()}</b> (Page {page + 1})");
            for (int i = page * ItemsPerPage; i < (page + 1) * ItemsPerPage; i++)
            {
                if (i >= options.Count()) break;
                NodeOption option = options.ElementAt(i);
                sb.AppendLine($"- {Enum.GetName(option)}");
            }
            return sb.ToString();
        }

        public static string GetQemuVmsMessage(string node, IEnumerable<NodeVmQemu> vms, int page)
        {
            StringBuilder sb = new();
            sb.AppendLine($"<b>Qemu VMs on {node.HtmlEscape()}</b> (Page {page + 1})");
            for (int i = page * ItemsPerPage; i < (page + 1) * ItemsPerPage; i++)
            {
                if (i >= vms.Count()) break;
                NodeVmQemu vm = vms.ElementAt(i);
                sb.AppendLine($"<code>{vm.VmId}@{node.HtmlEscape()}</code> ({vm.Name.HtmlEscape()}): {vm.Status}");
            }
            return sb.ToString();
        }
    }
}
