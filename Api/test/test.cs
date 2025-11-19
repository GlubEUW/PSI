using Api.Data;
using Api.Entities;

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TestController(DatabaseContext context) : ControllerBase
{
   private readonly DatabaseContext _context = context;

   [HttpPost("insert-sample")]
   public async Task<IActionResult> InsertSample()
   {
      try
      {
         var user = new RegisteredUser
         {
            Id = Guid.NewGuid(),
            Name = "TestUser_" + DateTime.UtcNow.Ticks,
            TotalWins = 5
         };

         await _context.Users.AddAsync(user);
         await _context.SaveChangesAsync();

         return Ok(new
         {
            message = "User inserted successfully!",
            user = new
            {
               user.Id,
               user.Name,
               user.TotalWins
            }
         });
      }
      catch (Exception ex)
      {
         return StatusCode(500, new { error = ex.Message, stackTrace = ex.StackTrace });
      }
   }

   [HttpGet("read-all")]
   public async Task<IActionResult> ReadAll()
   {
      try
      {
         var users = await _context.Users.ToListAsync();

         return Ok(new
         {
            count = users.Count,
            users = users.Select(u => new
            {
               u.Id,
               u.Name,
               u.TotalWins
            })
         });
      }
      catch (Exception ex)
      {
         return StatusCode(500, new { error = ex.Message, stackTrace = ex.StackTrace });
      }
   }

   [HttpGet("test-connection")]
   public async Task<IActionResult> TestConnection()
   {
      try
      {
         var canConnect = await _context.Database.CanConnectAsync();
         return Ok(new { connected = canConnect, message = "Database connection test" });
      }
      catch (Exception ex)
      {
         return StatusCode(500, new { error = ex.Message });
      }
   }

   [HttpDelete("clear-all")]
   public async Task<IActionResult> ClearAll()
   {
      try
      {
         var users = await _context.Users.ToListAsync();
         _context.Users.RemoveRange(users);
         await _context.SaveChangesAsync();

         return Ok(new { message = $"Deleted {users.Count} users" });
      }
      catch (Exception ex)
      {
         return StatusCode(500, new { error = ex.Message });
      }
   }
}
