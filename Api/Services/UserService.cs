using Api.Entities;

namespace Api.Services;

public class UserService : IUserService
{
   public static User CreateUser(string name, Guid id, string role)
   {
      return (role == "Guest") ? new Guest() : new RegisteredUser()
      {
         Name = name,
         Id = id
      };
   }
}