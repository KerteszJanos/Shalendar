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
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Shalendar.Functions;
using Microsoft.AspNetCore.SignalR;
using System.Net.Sockets;

namespace Shalendar.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	[Authorize]
	public class CalendarListsController : ControllerBase
	{
		private readonly ShalendarDbContext _context;

		private readonly JwtHelper _jwtHelper;
		private readonly GetCalendarIdHelper _getCalendarIdHelper;

		private readonly IHubContext<CalendarHub> _calendarHub;

		private readonly GroupManagerService _groupManager;

		public CalendarListsController(ShalendarDbContext context, JwtHelper jwtHelper, GetCalendarIdHelper getCalendarIdHelper, GroupManagerService groupManager, IHubContext<CalendarHub> calendarHub)
		{
			_context = context;
			_jwtHelper = jwtHelper;
			_getCalendarIdHelper = getCalendarIdHelper;
			_groupManager = groupManager;
			_calendarHub = calendarHub;
		}



		#region Gets

		// GET: api/CalendarLists/calendar/{calendarId}
		[HttpGet("calendar/{calendarId}")]
		public async Task<ActionResult<IEnumerable<object>>> GetCalendarListsByCalendarId(int calendarId)
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

			var lists = await _context.CalendarLists
				.Where(list => list.CalendarId == calendarId)
				.Select(list => new
				{
					list.Id,
					list.Name,
					list.Color,
					list.CalendarId,
					Tickets = _context.Tickets
						.Where(ticket => ticket.CurrentParentType == "CalendarList" && ticket.ParentId == list.Id)
						.Select(ticket => new
						{
							ticket.Id,
							ticket.Name,
							ticket.Description,
							ticket.StartTime,
							ticket.EndTime,
							ticket.CurrentPosition,
							ticket.Priority,
							ticket.IsCompleted,
						})
						.ToList()
				})
				.ToListAsync();

			return Ok(lists);
		}

		#endregion



		#region Posts

		// POST: api/CalendarLists
		[HttpPost]
		public async Task<ActionResult<CalendarList>> PostCalendarList(CalendarList calendarList)
		{
			if (!_getCalendarIdHelper.TryGetCalendarId(HttpContext, out int calendarId))
			{
				return BadRequest("Invalid or missing X-Calendar-Id header.");
			}

			var requiredPermission = "owner";
			var hasPermission = await _jwtHelper.HasCalendarPermission(HttpContext, requiredPermission, calendarId);

			if (!hasPermission)
			{
				return new ObjectResult(new { message = $"Required permission: {requiredPermission}" })
				{
					StatusCode = StatusCodes.Status403Forbidden
				};
			}

			var calendarExists = await _context.Calendars.AnyAsync(c => c.Id == calendarList.CalendarId);
			if (!calendarExists)
			{
				return BadRequest("The specified CalendarId does not exist.");
			}

			_context.CalendarLists.Add(calendarList);
			await _context.SaveChangesAsync();

			if (!_groupManager.IsUserAloneInGroup(calendarId.ToString()))
			{
				await _calendarHub.Clients.Group(calendarId.ToString()).SendAsync("CalendarListCreated");
			}

			return CreatedAtAction(nameof(GetCalendarListsByCalendarId), new { calendarId = calendarList.CalendarId }, calendarList);
		}

		#endregion



		#region Puts

		// PUT: api/CalendarLists/{id}
		[HttpPut("{id}")]
		public async Task<IActionResult> UpdateCalendarList(int id, [FromBody] CalendarList updatedList)
		{
			if (!_getCalendarIdHelper.TryGetCalendarId(HttpContext, out int calendarId))
			{
				return BadRequest("Invalid or missing X-Calendar-Id header.");
			}

			var requiredPermission = "owner";
			var hasPermission = await _jwtHelper.HasCalendarPermission(HttpContext, requiredPermission, calendarId);

			if (!hasPermission)
			{
				return new ObjectResult(new { message = $"Required permission: {requiredPermission}" })
				{
					StatusCode = StatusCodes.Status403Forbidden
				};
			}

			if (id != updatedList.Id)
			{
				return BadRequest("ID mismatch.");
			}

			var existingList = await _context.CalendarLists.FindAsync(id);
			if (existingList == null)
			{
				return NotFound("Calendar list not found.");
			}

			existingList.Name = updatedList.Name;
			existingList.Color = updatedList.Color;

			try
			{
				await _context.SaveChangesAsync();
			}
			catch (DbUpdateConcurrencyException)
			{
				return StatusCode(500, "Error updating the list.");
			}

			if (!_groupManager.IsUserAloneInGroup(calendarId.ToString()))
			{
				await _calendarHub.Clients.Group(calendarId.ToString()).SendAsync("CalendarListUpdated");
			}

			return NoContent();
		}

		#endregion



		#region Deletes

		// DELETE: api/CalendarLists/{id}
		[HttpDelete("{id}")]
		public async Task<IActionResult> DeleteCalendarList(int id)
		{
			if (!_getCalendarIdHelper.TryGetCalendarId(HttpContext, out int calendarId))
			{
				return BadRequest("Invalid or missing X-Calendar-Id header.");
			}

			var requiredPermission = "owner";
			var hasPermission = await _jwtHelper.HasCalendarPermission(HttpContext, requiredPermission, calendarId);

			if (!hasPermission)
			{
				return new ObjectResult(new { message = $"Required permission: {requiredPermission}" })
				{
					StatusCode = StatusCodes.Status403Forbidden
				};
			}

			using var transaction = await _context.Database.BeginTransactionAsync();

			try
			{
				var list = await _context.CalendarLists.FindAsync(id);
				if (list == null)
				{
					return NotFound("Calendar list not found.");
				}

				var relatedTickets = await _context.Tickets
					.Where(t => t.CalendarListId == id)
					.ToListAsync();

				if (relatedTickets.Any())
				{
					_context.Tickets.RemoveRange(relatedTickets);
					await _context.SaveChangesAsync();
				}

				_context.CalendarLists.Remove(list);
				await _context.SaveChangesAsync();

				await transaction.CommitAsync();

				if (!_groupManager.IsUserAloneInGroup(calendarId.ToString()))
				{
					await _calendarHub.Clients.Group(calendarId.ToString()).SendAsync("CalendarListDeleted");
				}

				return NoContent();
			}
			catch (DbUpdateException ex)
			{
				await transaction.RollbackAsync();
				return StatusCode(500, $"Error deleting list and related tickets: {ex.Message}");
			}
		}

		#endregion
	}
}
