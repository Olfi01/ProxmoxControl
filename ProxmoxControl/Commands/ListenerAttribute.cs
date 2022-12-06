using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProxmoxControl.Commands
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    internal class ListenerAttribute : Attribute
    {
        public string ListenerID { get; }

        public ListenerAttribute(string listenerID)
        {
            ListenerID = listenerID;
        }
    }
}
