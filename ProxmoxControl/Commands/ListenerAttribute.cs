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
