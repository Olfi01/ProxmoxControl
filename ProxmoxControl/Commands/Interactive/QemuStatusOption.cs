using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProxmoxControl.Commands.Interactive
{
    public enum QemuStatusOption
    {
        Start,
        Shutdown,
        Stop,
        Reboot,
        Reset,
        Suspend,
        Resume,
    }
}
