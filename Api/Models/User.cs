namespace PSI.Api.Models
{
   public class User
   {
      public string Id { get; set; }
      public string ConnectionId { get; set; }

      public User(string id, string connectionId)
      {
         Id = id;
         ConnectionId = connectionId;
      }
   }
}
