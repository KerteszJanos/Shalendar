using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shalendar.Contexts;
using Shalendar.Models;

namespace Shalendar.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	[Authorize]
	public class CalendarsController : ControllerBase
	{
		private readonly ShalendarDbContext _context;
		private readonly JwtHelper _jwtHelper;

		public CalendarsController(ShalendarDbContext context, JwtHelper jwtHelper)
		{
			_context = context;
			_jwtHelper = jwtHelper;
		}

		#region Gets

		// GET: api/Calendars/5
		[HttpGet("{id}")]
		public async Task<ActionResult<Calendar>> GetCalendar(int id)
		{
			var requiredPermission = "read";
			var hasPermission = await _jwtHelper.HasCalendarPermission(HttpContext, requiredPermission);

			if (!hasPermission)
			{
				return new ObjectResult(new { message = $"Required permission: {requiredPermission}" })
				{
					StatusCode = StatusCodes.Status403Forbidden
				};
			}

			var calendar = await _context.Calendars.FindAsync(id);

			if (calendar == null)
			{
				return NotFound();
			}

			return calendar;
		}

		// GET: api/Calendars/5
		[HttpGet("noPermissionNeeded/{id}")]
		public async Task<ActionResult<Calendar>> GetCalendarNoPermissionNeeded(int id)
		{
			var calendar = await _context.Calendars.FindAsync(id);

			if (calendar == null)
			{
				return NotFound();
			}

			return calendar;
		}

		// GET: api/Calendars/user/{userId}
		[HttpGet("user/{userId}")]
		public async Task<ActionResult<IEnumerable<CalendarPermission>>> GetUserCalendarPermissions(int userId)
		{
			var permissions = await _context.CalendarPermissions
				.Where(cp => cp.UserId == userId)
				.ToListAsync();

			return Ok(permissions);
		}

		// GET: api/Calendars/{calendarId}/permissions
		[HttpGet("{calendarId}/permissions")]
		public async Task<ActionResult<IEnumerable<object>>> GetCalendarPermissions(int calendarId)
		{
			var requiredPermission = "owner";
			var hasPermission = await _jwtHelper.HasCalendarPermission(HttpContext, requiredPermission, calendarId);

			if (!hasPermission)
			{
				return new ObjectResult(new { message = $"Required permission: {requiredPermission}" })
				{
					StatusCode = StatusCodes.Status403Forbidden
				};
			}

			var permissions = await _context.CalendarPermissions
				.Where(cp => cp.CalendarId == calendarId)
				.Join(_context.Users,
					  cp => cp.UserId,
					  u => u.Id,
					  (cp, u) => new
					  {
						  Email = u.Email,
						  PermissionType = cp.PermissionType
					  })
				.ToListAsync();

			if (!permissions.Any())
			{
				return NotFound("No permissions found for this calendar.");
			}

			return Ok(permissions);
		}

		#endregion

		#region Posts

		// POST: api/Calendars
		[HttpPost]
		public async Task<ActionResult<Calendar>> CreateCalendar(int userId, [FromBody] Calendar calendar)
		{
			using var transaction = await _context.Database.BeginTransactionAsync();
			try
			{
				// Létrehozzuk az új naptárat
				_context.Calendars.Add(calendar);
				await _context.SaveChangesAsync();

				// Hozzáadjuk a tulajdonosi engedélyt a létrehozónak
				var ownerPermission = new CalendarPermission
				{
					CalendarId = calendar.Id,
					UserId = userId,
					PermissionType = "owner"
				};

				var calendarList = new CalendarList
				{
					Name = "Default List",
					CalendarId = calendar.Id,
					Color = "#CCCCCC"
				};

				_context.CalendarPermissions.Add(ownerPermission);
				_context.CalendarLists.Add(calendarList); // Hozzáadtam a listát is, mert kimaradt
				await _context.SaveChangesAsync();

				// Ha minden rendben, commitáljuk a tranzakciót
				await transaction.CommitAsync();

				return CreatedAtAction(nameof(GetCalendar), new { id = calendar.Id }, calendar);
			}
			catch (Exception ex)
			{
				// Hiba esetén rollback, hogy ne maradjanak félkész adatok
				await transaction.RollbackAsync();
				return StatusCode(500, $"Hiba történt a naptár létrehozásakor: {ex.Message}");
			}
		}

		// GPT generated
		// POST: api/Calendars/{calendarId}/permissions/{email}/{permissionType}
		[HttpPost("{calendarId}/permissions/{email}/{permissionType}")]
		public async Task<IActionResult> AddCalendarPermission(int calendarId, string email, string permissionType)
		{
			var requiredPermission = "owner";
			var hasPermission = await _jwtHelper.HasCalendarPermission(HttpContext, requiredPermission, calendarId);

			if (!hasPermission)
			{
				return new ObjectResult(new { message = $"Required permission: {requiredPermission}" })
				{
					StatusCode = StatusCodes.Status403Forbidden
				};
			}

			var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
			if (user == null)
			{
				return NotFound(new { message = "User not found." });
			}

			var existingPermission = await _context.CalendarPermissions
				.FirstOrDefaultAsync(cp => cp.CalendarId == calendarId && cp.UserId == user.Id);

			if (existingPermission != null)
			{
				existingPermission.PermissionType = permissionType;
			}
			else
			{
				var newPermission = new CalendarPermission
				{
					CalendarId = calendarId,
					UserId = user.Id,
					PermissionType = permissionType
				};
				_context.CalendarPermissions.Add(newPermission);
			}

			await _context.SaveChangesAsync();

			return Ok();
		}


		#endregion

		#region Puts

		#endregion

		#region Deletes

		// DELETE: api/Calendars/{calendarId}/permissions/{email}
		[HttpDelete("permissions/{email}")]
		public async Task<IActionResult> DeleteCalendarPermission(string email)
		{
			var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
			if (user == null)
			{
				return NotFound(new { message = "User not found." });
			}

			var permission = await _context.CalendarPermissions
				.FirstOrDefaultAsync(cp => cp.UserId == user.Id);

			if (permission == null)
			{
				return NotFound(new { message = "Permission not found." });
			}

			_context.CalendarPermissions.Remove(permission);
			await _context.SaveChangesAsync();

			return Ok(new { message = "Permission deleted successfully." });
		}

		#endregion
	}
}
