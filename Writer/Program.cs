using Microsoft.EntityFrameworkCore;
using Shared;

namespace Writer;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("Starting");
            
        while (true)
        {
            await using var ctx = new PostgresContext("Host=postgres;Database=postgres;Username=postgres;Password=password");

            try
            {
                ctx.Put();
                await ctx.SaveChangesAsync();
                // Console.WriteLine("*");
            }
            catch (DbUpdateConcurrencyException)
            {
                // Console.WriteLine("* DbUpdateConcurrencyException");
            }
        }
    }
}