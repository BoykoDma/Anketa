using Microsoft.EntityFrameworkCore;

namespace Anketa.Models.ConnectionDB
{
    public class Context : DbContext
    {
        public Context(DbContextOptions<Context> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }

    }
}
