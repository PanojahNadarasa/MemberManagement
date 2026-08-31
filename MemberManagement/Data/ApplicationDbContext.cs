using MemberManagement.Entity;
using Microsoft.EntityFrameworkCore;

namespace MemberManagement.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(
            DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<MemberEntity> members { get; set; }
    }
}
