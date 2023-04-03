using System.ComponentModel.DataAnnotations;

namespace Shared.Models;

public class Versioned
{
    public int Id { get; set; } = 1;
    [ConcurrencyCheck]
    public Guid Version { get; set; }
}
