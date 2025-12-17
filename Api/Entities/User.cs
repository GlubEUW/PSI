
namespace Api.Entities;

public abstract class User : IComparable<User>
{
   public Guid Id { get; set; } = Guid.Empty;
   public string Name { get; set; } = string.Empty;

   public int CompareTo(User? other)
   {
      if (other is null)
         return 1;

      return string.Compare(Name, other.Name, StringComparison.Ordinal);
   }
}

public class Guest : User { }

public class RegisteredUser : User
{
   public string PasswordHash { get; set; } = string.Empty;
}