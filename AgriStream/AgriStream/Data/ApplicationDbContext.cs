using Microsoft.EntityFrameworkCore;
using AgriStream.Models;

namespace AgriStream.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext (DbContextOptions<ApplicationDbContext> options) : base(options) 
        {
        }

        public DbSet<User>Users { get; set; }
    }
}
