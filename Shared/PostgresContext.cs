using System.Transactions;
using Microsoft.EntityFrameworkCore;
using Shared.Models;

namespace Shared;

public class PostgresContext : DbContext
{
    private readonly string _connectionString;

    public PostgresContext(string connectionString)
    {
        _connectionString = connectionString;
    }
        
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseNpgsql(_connectionString);
    }

    public DbSet<Reference> References { get; set; }
    public DbSet<Versioned> Versions { get; set; }

    public async IAsyncEnumerable<Reference> QueryTransactions(DateOnly from, long at, long checkpoint)
    {
        while (true)
        {
            // using var scope = new TransactionScope(TransactionScopeOption.Required, new TransactionOptions { IsolationLevel = IsolationLevel.RepeatableRead });
            var any = false;

            foreach (var item in References.Where(x => x.Booked >= from && x.Checkpoint > at && x.Checkpoint > checkpoint)
                         .OrderBy(x => x.Booked)
                         .ThenBy(x => x.Checkpoint)
                         .Take(25))
            {
                any = true;
                from = item.Booked;
                at = item.Checkpoint;
                yield return item;
            }

            if (!any)
            {
                await Task.Delay(TimeSpan.FromMicroseconds(100));
            }
        }
    }

    public void Put()
    {
        // using var scope = new TransactionScope(TransactionScopeOption.Required, new TransactionOptions { IsolationLevel = IsolationLevel.Serializable });
        using var scope = new TransactionScope();
        var version = Versions.FirstOrDefault();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        if (version is null)
        {
            version = new Versioned();
            Versions.Add(version);
        }

        References.Add(new()
        {
            Booked = today,
        });

        version.Version = Guid.NewGuid();
    }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Reference>(t =>
        {
            t.HasKey(c => new { Date = c.Booked, c.Checkpoint });
            t.Property(c => c.Checkpoint).ValueGeneratedOnAdd();
        });
            
        modelBuilder.Entity<Versioned>(t =>
        {
            t.HasKey(c => c.Id);
        });
                
        base.OnModelCreating(modelBuilder);
    }

    public long CurrentCheckpoint()
    {
        try
        {
            return References.Max(c => c.Checkpoint);
        }
        catch (InvalidOperationException)
        {
            return -1;
        }
    }
}