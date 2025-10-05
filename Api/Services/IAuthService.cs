using PSI.Api.Models;

namespace PSI.Api.Services
{
   public interface IAuthService
   {
      string? GuestCreate(GuestDto request);
   }
}
