using System.Collections.Concurrent;

namespace PSI.Api.Entities
{
   public class Guest
   {
      public Guid Id { get; set; } = Guid.NewGuid();
      public string Name { get; set; } = string.Empty;
   }
}