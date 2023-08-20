using Microsoft.EntityFrameworkCore;

namespace Coflnet.Sky.Chat.Models
{
    /// <summary>
    /// <see cref="DbContext"/> For flip tracking
    /// </summary>
    public class ChatDbContext : DbContext
    {
        public DbSet<DbMessage> Messages { get; set; }
        public DbSet<Report> Reports { get; set; }
        public DbSet<ModelClient> Clients { get; set; }
        public DbSet<Mute> Mute { get; set; }

        /// <summary>
        /// Creates a new instance of <see cref="ChatDbContext"/>
        /// </summary>
        /// <param name="options"></param>
        public ChatDbContext(DbContextOptions<ChatDbContext> options)
        : base(options)
        {
        }

        /// <summary>
        /// Configures additional relations and indexes
        /// </summary>
        /// <param name="modelBuilder"></param>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<DbMessage>(entity =>
            {
                entity.HasIndex(e => new { e.Sender });
            });
            modelBuilder.Entity<Mute>(entity =>
            {
                entity.HasIndex(e => new { e.Uuid, e.Expires, e.Status });
            });
        }
    }
}