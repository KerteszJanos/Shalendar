using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shalendar.Contexts;
using Shalendar.Models;
using Microsoft.AspNetCore.Identity;

namespace Shalendar.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly ShalendarDbContext _context;

        public UsersController(ShalendarDbContext context)
        {
            _context = context;
        }

        // GET: api/Users
        [HttpGet]
        public async Task<ActionResult<IEnumerable<User>>> GetUsers()
        {
            return await _context.Users.ToListAsync();
        }

        // GET: api/Users/5
        [HttpGet("{id}")]
        public async Task<ActionResult<User>> GetUser(int id)
        {
            var user = await _context.Users.FindAsync(id);

            if (user == null)
            {
                return NotFound();
            }

            return user;
        }

        // PUT: api/Users/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutUser(int id, User user)
        {
            if (id != user.Id)
            {
                return BadRequest();
            }

            _context.Entry(user).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!UserExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // POST: api/Users
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
		public async Task<ActionResult<User>> PostUser(User user)
		{
			using var transaction = await _context.Database.BeginTransactionAsync();
			try
			{
				var passwordHasher = new PasswordHasher<User>();
				user.PasswordHash = passwordHasher.HashPassword(user, user.PasswordHash);

				var defaultCalendar = new Calendar
				{
					Name = $"{user.Username}'s Default Calendar"
				};

				_context.Calendars.Add(defaultCalendar);
				await _context.SaveChangesAsync();

				user.DefaultCalendarId = defaultCalendar.Id;
				_context.Users.Add(user);
				await _context.SaveChangesAsync();

				var calendarList = new CalendarList
				{
					Name = "Default List",
					CalendarId = defaultCalendar.Id,
					Color = "#CCCCCC"
				};

				_context.CalendarLists.Add(calendarList);
				await _context.SaveChangesAsync();

				await transaction.CommitAsync();
				return CreatedAtAction("GetUser", new { id = user.Id }, user);
			}
			catch (Exception ex)
			{
				await transaction.RollbackAsync();
				return StatusCode(500, $"Error creating user: {ex.Message}");
			}
		}

		// GET: api/Users/login
		[HttpPost("login")]
		public async Task<IActionResult> LoginUser([FromBody] LoginModel loginModel)
		{
			var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == loginModel.Email);

			if (user == null)
			{
				return Unauthorized("Invalid email or password.");
			}

			var passwordHasher = new PasswordHasher<User>();
			var result = passwordHasher.VerifyHashedPassword(user, user.PasswordHash, loginModel.Password);

			if (result == PasswordVerificationResult.Failed)
			{
				return Unauthorized("Invalid email or password.");
			}

			return Ok();
		}

		// DELETE: api/Users/5
		[HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool UserExists(int id)
        {
            return _context.Users.Any(e => e.Id == id);
        }
    }
}
