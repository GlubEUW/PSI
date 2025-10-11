using System.Collections.Concurrent;

namespace Api.Entities;

public class Guest
{
   public Guid Id { get; set; } = Guid.NewGuid();
   public string Name { get; set; } = string.Empty;
}
