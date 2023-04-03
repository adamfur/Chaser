using System.Diagnostics;
using Shared;

namespace Chaser;

class Program
{
    static async Task Main(string[] args)
    {
        await Create();
        await Purge();
        await using var ctx = new PostgresContext("Host=postgres;Database=postgres;Username=postgres;Password=password");

        var checkpoint = 0;
        var from = DateOnly.FromDateTime(DateTime.MinValue);
        var first = true;
        long prev = 0;
        long acc = 0;
        var timer = Stopwatch.StartNew();
        

        Console.WriteLine("Starting");
        await foreach (var item in ctx.QueryTransactions(from, 0, checkpoint))
        {
            acc += item.Checkpoint;
            
            if (first)
            {
                first = false;
            }
            else
            {
                var expected = prev + 1;
                
                if (item.Checkpoint != expected)
                {
                    Console.WriteLine($"Acc: {acc}, Gap ({Math.Abs(item.Checkpoint - prev)}): prev: {prev}, got: {item.Checkpoint} expected: {expected}");
                    
                    if (timer.Elapsed > TimeSpan.FromSeconds(10))
                    {
                        prev = item.Checkpoint;
                        break;
                    }
                }
            }
            
            prev = item.Checkpoint;
            // Console.WriteLine($"{item.Booked:yyyy-MM-dd} / {item.Checkpoint}");
        }
        
        await Task.Delay(TimeSpan.FromSeconds(1));

        var calculated = ctx.References.Where(x => x.Checkpoint <= prev).Sum(x => x.Checkpoint);
        
        Console.WriteLine($"Acc: {acc} vs. {calculated}");

        if (acc != calculated)
        {
            await Task.Delay(TimeSpan.FromDays(30));
        }
    }
    
    private static async Task Create()
    {
        await using var ctx = new PostgresContext("Host=postgres;Database=postgres;Username=postgres;Password=password");
        await ctx.Database.EnsureCreatedAsync();
    }
    
    private static async Task Purge()
    {
        await using var ctx = new PostgresContext("Host=postgres;Database=postgres;Username=postgres;Password=password");
        ctx.References.RemoveRange(ctx.References);
        await ctx.SaveChangesAsync();
    }
}