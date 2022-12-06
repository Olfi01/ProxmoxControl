using Microsoft.EntityFrameworkCore.Design;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
