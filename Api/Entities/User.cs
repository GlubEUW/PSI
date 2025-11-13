namespace Api.Entities;

public abstract class User : IComparable<User> // Usage of standard DOTNET interface
{
   public Guid Id { get; set; } = Guid.Empty;
   public string Name { get; set; } = string.Empty;
   public int Wins { get; set; } = 0;
   public int CompareTo(User? other)
   {
      if (other is null)
         return 1;

      return Wins.CompareTo(other.Wins);
   }
}

public class Guest : User { }

public class RegisteredUser : User
{
   public string PasswordHash { get; set; } = string.Empty;
}
