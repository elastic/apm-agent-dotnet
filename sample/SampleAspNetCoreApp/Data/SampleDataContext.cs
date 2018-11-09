using System;
using Microsoft.EntityFrameworkCore;

namespace SampleAspNetCoreApp.Data
{
    public class SampleDataContext : DbContext
    {
        public SampleDataContext(DbContextOptions<SampleDataContext> dbContextOptions):base(dbContextOptions){ }

        public DbSet<User> Users { get; set; }
    }
}