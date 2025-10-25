using Api.Enums;

namespace Api.Entities;

public class User : IComparable<User> // Usage of standard DOTNET interface
{
   public Guid Id { get; set; } = Guid.Empty;
   public string Name { get; set; } = string.Empty;
   public UserRole Role { get; set; } = UserRole.Guest;
   public int Wins { get; set; } = 0;
   public int CompareTo(User? other)
   {
      if (other is null)
         return 1;

      return Wins.CompareTo(other.Wins);
   }
}
