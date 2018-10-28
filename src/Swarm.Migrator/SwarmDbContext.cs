using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using Swarm.Basic;
using Swarm.Basic.Entity;

namespace Swarm.Migrator
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
            modelBuilder.Entity<Job>().HasIndex(x => x.Owner);

            modelBuilder.Entity<JobProperty>().HasIndex(x => x.JobId);
            modelBuilder.Entity<JobProperty>().HasKey(x => new {x.JobId, x.Name});

            modelBuilder.Entity<Client>().HasIndex(x => new {x.Name, x.Group}).IsUnique();

            modelBuilder.Entity<JobState>().HasKey(x => new {x.JobId, x.TraceId, x.Client});
            modelBuilder.Entity<JobState>().HasIndex(x => x.JobId);
            modelBuilder.Entity<JobState>().HasIndex(x => new {x.JobId, x.TraceId});
            modelBuilder.Entity<JobState>().HasIndex(x => new {x.TraceId, x.Client}).IsUnique();
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