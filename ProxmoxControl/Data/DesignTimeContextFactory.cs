using Microsoft.EntityFrameworkCore.Design;

namespace ProxmoxControl.Data
{
    internal class DesignTimeContextFactory : IDesignTimeDbContextFactory<ProxmoxControlDbContext>
    {
        public ProxmoxControlDbContext CreateDbContext(string[] args)
        {
            return ProxmoxControlDbContext.Instance;
        }
    }
}
