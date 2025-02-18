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
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Shalendar.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class UsersController : ControllerBase
	{
		private readonly ShalendarDbContext _context;
		private readonly IConfiguration _configuration;


		public UsersController(ShalendarDbContext context, IConfiguration configuration)
		{
			_context = context;
			_configuration = configuration;
		}


		//TODO: kell auth!
		// GET: api/Users
		[HttpGet]
		public async Task<ActionResult<IEnumerable<User>>> GetUsers()
		{
			return await _context.Users.ToListAsync();
		}

		//TODO: kell auth!
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

		//TODO: kell auth!
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
			bool emailExists = await _context.Users.AnyAsync(u => u.Email == user.Email);
			if (emailExists)
			{
				return BadRequest("An account with this email already exists.");
			}

			var passwordValidationMessage = ValidatePassword(user.PasswordHash);
			if (passwordValidationMessage != null)
			{
				return BadRequest(passwordValidationMessage);
			}

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


		[HttpPost("login")]
		public async Task<IActionResult> LoginUser([FromBody] LoginModel loginModel)
		{
			var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == loginModel.Email);

			if (user == null)
			{
				return Unauthorized("This email address is not registered.");
			}

			var passwordHasher = new PasswordHasher<User>();
			var result = passwordHasher.VerifyHashedPassword(user, user.PasswordHash, loginModel.Password);

			if (result == PasswordVerificationResult.Failed)
			{
				return Unauthorized("Invalid password.");
			}

			var token = GenerateJwtToken(user);

			return Ok(new
			{
				user = new
				{
					userId = user.Id,
					username = user.Username,
					email = user.Email,
					defaultCalendarId = user.DefaultCalendarId,
				},
				token
			});
		}


		//TODO: kell auth!
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

		private string GenerateJwtToken(User user)
		{
			var jwtSettings = _configuration.GetSection("JwtSettings");
			var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["SecretKey"]));
			var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

			var claims = new[]
			{
		new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
		new Claim(JwtRegisteredClaimNames.Email, user.Email),
		new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
	};

			var token = new JwtSecurityToken(
				issuer: jwtSettings["Issuer"],
				audience: jwtSettings["Audience"],
				claims: claims,
				expires: DateTime.UtcNow.AddMinutes(Convert.ToInt32(jwtSettings["ExpirationInMinutes"])),
				signingCredentials: credentials
			);

			return new JwtSecurityTokenHandler().WriteToken(token);
		}

		private string? ValidatePassword(string password)
		{
			if (string.IsNullOrEmpty(password) || password.Length < 8)
				return "Password must be at least 8 characters long.";

			bool hasUpperCase = password.Any(char.IsUpper);
			bool hasDigit = password.Any(char.IsDigit);

			if (!hasUpperCase)
				return "Password must contain at least one uppercase letter.";

			if (!hasDigit)
				return "Password must contain at least one number.";

			return null;
		}
	}
}
