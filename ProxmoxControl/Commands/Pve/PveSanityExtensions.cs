using Corsinvest.ProxmoxVE.Api;
using Corsinvest.ProxmoxVE.Api.Shared.Models.Node;
using Corsinvest.ProxmoxVE.Api.Extension;

namespace ProxmoxControl.Commands.Pve
{
    public static class PveSanityExtensions
    {
        public static async Task<IEnumerable<NodeVmQemu>> GetSorted(this PveClient.PveNodes.PveNodeItem.PveQemu qemu, bool? full = null)
        {
            return (await qemu.Get(full)).OrderBy(vm => vm.VmId);
        }

        public static async Task<IEnumerable<NodeItem>> GetSorted(this PveClient.PveNodes nodes)
        {
            return (await nodes.Get()).OrderBy(node => node.Id);
        }
    }
}
