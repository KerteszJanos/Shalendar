using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Shalendar.Contexts;
using Shalendar.Models;
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
		private readonly JwtHelper _jwtHelper;
		private readonly IConfiguration _configuration;


		public UsersController(ShalendarDbContext context,JwtHelper jwtHelper, IConfiguration configuration)
		{
			_context = context;
			_jwtHelper = jwtHelper;
			_configuration = configuration;
		}

		#region Gets

		[HttpGet("me")]
		[Authorize]
		public async Task<ActionResult<User>> GetCurrentUser()
		{
			var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
			var user = await _context.Users.FindAsync(userId);

			if (user == null)
			{
				return NotFound();
			}

			return Ok(new
			{
				userId = user.Id,
				username = user.Username,
				email = user.Email
			});
		}


		#endregion



		#region Posts

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

				var calendarPermission = new CalendarPermission
				{
					CalendarId = defaultCalendar.Id,
					UserId = user.Id,
					PermissionType = "owner"
				};

				_context.CalendarPermissions.Add(calendarPermission);
				await _context.SaveChangesAsync();

				await transaction.CommitAsync();
				return Ok();
			}
			catch (Exception ex)
			{
				await transaction.RollbackAsync();
				return StatusCode(500, $"Error creating user: {ex.Message}");
			}
		}

		// POST: api/login
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

		#endregion



		#region Puts

		[HttpPut("change-password")]
		[Authorize]
		public async Task<IActionResult> ChangePassword([FromBody] ChangePassword model)
		{
			var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
			var user = await _context.Users.FindAsync(userId);

			if (user == null)
			{
				return NotFound();
			}

			var passwordHasher = new PasswordHasher<User>();
			var passwordCheck = passwordHasher.VerifyHashedPassword(user, user.PasswordHash, model.OldPassword);

			if (passwordCheck == PasswordVerificationResult.Failed)
			{
				return BadRequest("Incorrect old password.");
			}

			var passwordValidationMessage = ValidatePassword(model.NewPassword);
			if (passwordValidationMessage != null)
			{
				return BadRequest(passwordValidationMessage);
			}

			user.PasswordHash = passwordHasher.HashPassword(user, model.NewPassword);
			await _context.SaveChangesAsync();

			return NoContent();
		}

		#endregion



		#region Deletes

		[HttpDelete("delete")]
		[Authorize]
		public async Task<IActionResult> DeleteCurrentUser()
		{

			//TODO: kitörölni mindent: naptárak, listák, jegyek, napok amihez a felhasználónak köze volt

			//	var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
			//	var user = await _context.Users.FindAsync(userId);

			//	if (user == null)
			//	{
			//		return NotFound();
			//	}

			//	_context.Users.Remove(user);
			//	await _context.SaveChangesAsync();

			return NoContent();
		}


		#endregion



		#region private methods

		private string GenerateJwtToken(User user)
		{
			var jwtSettings = _configuration.GetSection("JwtSettings");
			var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["SecretKey"]));
			var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

			var userPermissions = _context.CalendarPermissions
				.Where(p => p.UserId == user.Id)
				.Select(p => new { p.CalendarId, p.PermissionType })
				.ToList();

			var claims = new List<Claim>
			{
				new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString(), ClaimValueTypes.Integer32),
				new Claim(JwtRegisteredClaimNames.Email, user.Email),
				new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
			};



			foreach (var permission in userPermissions)
			{
				claims.Add(new Claim("CalendarPermission", $"{permission.CalendarId}:{permission.PermissionType}"));
			}

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

		#endregion
	}
}
