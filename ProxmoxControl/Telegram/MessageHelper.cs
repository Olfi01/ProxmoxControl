﻿using Corsinvest.ProxmoxVE.Api.Shared.Models.Node;
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
            for (int i = page * ItemsPerPage; i < (page +1) * ItemsPerPage; i++)
            {
                if (i >= nodes.Count()) break;
                NodeItem node = nodes.ElementAt(i);
                sb.AppendLine($"<code>{node.Node}</code>: {node.Status}");
            }
            return sb.ToString();
        }
    }
}