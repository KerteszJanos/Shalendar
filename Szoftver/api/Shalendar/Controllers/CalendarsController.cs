using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Shalendar.Contexts;
using Shalendar.Functions;
using Shalendar.Functions.Interfaces;
using Shalendar.Models;
using Shalendar.Services.Interfaces;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Shalendar.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	[Authorize]
	public class CalendarsController : ControllerBase
	{
		private readonly ShalendarDbContext _context;
		private readonly IJwtHelper _jwtHelper;
		private readonly DeleteCalendarHelper _deleteCalendarHelper;
		private readonly CopyTicketHelper _copyTicketHelper;

		private readonly IGroupManagerService _groupManager;
		private readonly IHubContext<CalendarHub> _calendarHub;

		public CalendarsController(ShalendarDbContext context, IJwtHelper jwtHelper, DeleteCalendarHelper deleteCalendarHelper, CopyTicketHelper copyTicketHelper, IGroupManagerService groupManager, IHubContext<CalendarHub> calendarHub)
		{
			_context = context;
			_jwtHelper = jwtHelper;
			_deleteCalendarHelper = deleteCalendarHelper;
			_copyTicketHelper = copyTicketHelper;
			_groupManager = groupManager;
			_calendarHub = calendarHub;
		}

		#region Gets

		// GET: api/Calendars/5
		[HttpGet("{id}")]
		public async Task<ActionResult<Calendar>> GetCalendar(int id)
		{
			var requiredPermission = "read";
			var hasPermission = await _jwtHelper.HasCalendarPermission(HttpContext, requiredPermission);

			var calendar = await _context.Calendars.FindAsync(id);

			if (calendar == null)
			{
				return NotFound();
			}

			if (!hasPermission)
			{
				return new ObjectResult(new { message = $"Required permission: {requiredPermission} for the calendar named: {calendar.Name}" })
				{
					StatusCode = StatusCodes.Status403Forbidden
				};
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


		// GET: api/Calendars/accessible
		[HttpGet("accessible")]
		public async Task<ActionResult<IEnumerable<object>>> GetUserAccessibleCalendars()
		{
			var userIdClaim = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
			if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
			{
				return BadRequest();
			}

			var accessibleCalendars = await _context.CalendarPermissions
				.Where(cp => cp.UserId == userId && (cp.PermissionType == "owner" || cp.PermissionType == "write"))
				.Join(_context.Calendars,
					  cp => cp.CalendarId,
					  c => c.Id,
					  (cp, c) => new { c.Id, c.Name })
				.ToListAsync();

			if (!accessibleCalendars.Any())
			{
				return NotFound(new { message = "No calendars found with owner or write permissions." });
			}

			return Ok(accessibleCalendars);
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
				_context.Calendars.Add(calendar);
				await _context.SaveChangesAsync();

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
					Color = "#45DFB1"
				};

				_context.CalendarPermissions.Add(ownerPermission);
				_context.CalendarLists.Add(calendarList);
				await _context.SaveChangesAsync();

				await transaction.CommitAsync();

				return CreatedAtAction(nameof(GetCalendar), new { id = calendar.Id }, calendar);
			}
			catch (Exception ex)
			{
				await transaction.RollbackAsync();
				return StatusCode(500, $"Hiba történt a naptár létrehozásakor: {ex.Message}");
			}
		}


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


		// POST: api/Calendars/copy-all-tickets
		[HttpPost("copy-all-tickets")]
		public async Task<IActionResult> CopyAllTickets(int calendarId)
		{
			if (!HttpContext.Request.Headers.TryGetValue("X-Calendar-Id", out var calendarIdHeader) || !int.TryParse(calendarIdHeader, out int originalCalendarId))
			{
				return BadRequest();
			}

			var requiredPermission = "read";
			var hasPermission = await _jwtHelper.HasCalendarPermission(HttpContext, requiredPermission);

			if (!hasPermission)
			{
				var calendarName = await _context.Calendars
						.Where(c => c.Id == originalCalendarId)
						.Select(c => c.Name)
						.FirstOrDefaultAsync();

				return new ObjectResult(new { message = $"Required permission: {requiredPermission} for calendar: '{calendarName}'" })
				{
					StatusCode = StatusCodes.Status403Forbidden
				};

			}

			requiredPermission = "write";
			hasPermission = await _jwtHelper.HasCalendarPermission(HttpContext, requiredPermission, calendarId);

			if (!hasPermission)
			{
				var calendarName = await _context.Calendars.Where(c => c.Id == calendarId).Select(c => c.Name).FirstOrDefaultAsync();
				return new ObjectResult(new { message = $"Required permission: {requiredPermission} for calendar: '{calendarName}'" })
				{
					StatusCode = StatusCodes.Status403Forbidden
				};
			}

			var days = await _context.Days
				.Where(d => d.CalendarId == originalCalendarId)
				.ToListAsync();

			foreach (var day in days)
			{
				var dayTickets = await _context.Tickets
					.Where(t => t.ParentId == day.Id)
					.Select(t => t.Id)
					.ToListAsync();

				foreach (var ticketId in dayTickets)
				{
					await _copyTicketHelper.CopyTicketAsync(_context, ticketId, calendarId, day.Date);
				}
			}

			var calendarLists = await _context.CalendarLists
				.Where(cl => cl.CalendarId == originalCalendarId)
				.ToListAsync();

			foreach (var list in calendarLists)
			{
				var listTickets = await _context.Tickets
					.Where(t => t.CalendarListId == list.Id && !days.Select(d => d.Id).Contains(t.ParentId))
					.ToListAsync();

				foreach (var ticket in listTickets)
				{
					await _copyTicketHelper.CopyTicketAsync(_context, ticket.Id, calendarId, null);
				}
			}

			await _calendarHub.Clients.Group(calendarId.ToString()).SendAsync("CalendarCopied");

			return Ok("All tickets successfully copied.");
		}

		#endregion



		#region Puts

		#endregion



		#region Deletes

		// DELETE: api/Calendars/{calendarId}/permissions/{email}
		[HttpDelete("{calendarId}/permissions/{email}")]
		public async Task<IActionResult> DeleteCalendarPermission(int calendarId, string email)
		{
			var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
			if (user == null)
			{
				return NotFound(new { message = "User not found." });
			}

			var permission = await _context.CalendarPermissions
				.FirstOrDefaultAsync(cp => cp.CalendarId == calendarId && cp.UserId == user.Id);

			if (permission == null)
			{
				return NotFound(new { message = "Permission not found." });
			}

			_context.CalendarPermissions.Remove(permission);
			await _context.SaveChangesAsync();

			return Ok();
		}


		// DELETE: api/Calendars/{calendarId}
		[HttpDelete("{calendarId}")]
		public async Task<IActionResult> DeleteCalendar(int calendarId)
		{
			var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
			if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
			{
				return Unauthorized(new { message = "User not authenticated." });
			}

			bool shouldDelete = await _deleteCalendarHelper.ShouldDeleteCalendar(userId, calendarId);

			if (!shouldDelete)
			{
				return Ok(new { message = "User permission removed." });
			}

			await _deleteCalendarHelper.DeleteCalendar(calendarId);
			return Ok(new { message = "Calendar deleted." });
		}

		#endregion
	}
}
