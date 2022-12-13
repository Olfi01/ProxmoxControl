using Microsoft.EntityFrameworkCore;

namespace ProxmoxControl.Data
{
    public class ProxmoxControlDbContext : DbContext
    {
        private static readonly string sqliteFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "proxmox_control", "db.sqlite");

        public static readonly ProxmoxControlDbContext Instance = new();
        private ProxmoxControlDbContext() 
        {
            string? directory = Path.GetDirectoryName(sqliteFilePath);
            if (directory != null) Directory.CreateDirectory(directory);
            Database.Migrate();
        }
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite($"Data Source={sqliteFilePath}");
        }


        public DbSet<RegisteredChat> RegisteredChats { get; set; }
    }
}
