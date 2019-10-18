using Microsoft.EntityFrameworkCore;

namespace WithDatabase.Database
{
    public class DatabaseContext : DbContext
    {

        public DatabaseContext(DbContextOptions<DatabaseContext> options) : base(options)
        {
        }

        public DbSet<SqrlUser> SqrlUser { get; set; }

        public DbSet<User> User { get; set; }

        public DbSet<NutInfoData> Nuts { get; set; }
        
        public void UpdateDatabase()
        {
            this.Database.Migrate();
        }

    }
}
