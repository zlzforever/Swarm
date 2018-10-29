using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using Swarm.Basic.Entity;

// ReSharper disable once CheckNamespace
namespace Swarm
{
    public class SwarmDbContext : DbContext, IDesignTimeDbContextFactory<SwarmDbContext>
    {
        public DbSet<Job> Job { get; set; }
        public DbSet<JobProperty> JobProperty { get; set; }
        public DbSet<Client> Client { get; set; }
        public DbSet<JobState> JobState { get; set; }
        public DbSet<Log> Log { get; set; }

        public SwarmDbContext()
        {
        }

        public SwarmDbContext(DbContextOptions options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Job>().HasIndex(x => x.Group);
            modelBuilder.Entity<Job>().HasIndex(x => x.Name);
            modelBuilder.Entity<Job>().HasIndex(x => new {x.Name, x.Group});
            modelBuilder.Entity<Job>().HasIndex(x => x.Owner);
            modelBuilder.Entity<Job>().HasIndex(x => x.CreationTime);

            modelBuilder.Entity<JobProperty>().HasIndex(x => x.JobId);
            modelBuilder.Entity<JobProperty>().HasIndex(x => new {x.JobId, x.Name}).IsUnique();

            modelBuilder.Entity<Client>().HasIndex(x => new {x.Name, x.Group}).IsUnique();
            modelBuilder.Entity<Client>().HasIndex(x => x.ConnectionId).IsUnique();
            modelBuilder.Entity<Client>().HasIndex(x => x.CreationTime);

            modelBuilder.Entity<JobState>().HasIndex(x => new {x.Sharding, x.JobId, x.TraceId, x.Client}).IsUnique();
            modelBuilder.Entity<JobState>().HasIndex(x => x.JobId);
            modelBuilder.Entity<JobState>().HasIndex(x => new {x.JobId, x.TraceId});
            modelBuilder.Entity<JobState>().HasIndex(x => new {x.Sharding, x.TraceId, x.Client}).IsUnique();
            modelBuilder.Entity<JobState>().HasIndex(x => x.CreationTime);

            modelBuilder.Entity<Log>().HasIndex(x => x.JobId);
            modelBuilder.Entity<Log>().HasIndex(x => x.CreationTime);
            modelBuilder.Entity<Log>().HasIndex(x => new {x.JobId, x.TraceId});
        }

        public SwarmDbContext CreateDbContext(string[] args)
        {
            var builder = new DbContextOptionsBuilder<SwarmDbContext>();
            builder.UseSqlServer(GetConnectionString());
            return new SwarmDbContext(builder.Options);
        }

        private string GetConnectionString()
        {
            var builder = new ConfigurationBuilder();
            builder.AddJsonFile("appsettings.json", optional: false);

            var configuration = builder.Build();

            return configuration.GetConnectionString("DefaultConnection");
        }
    }
}