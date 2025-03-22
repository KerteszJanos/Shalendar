using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shalendar.Contexts;
using Shalendar.Functions.Interfaces;
using Shalendar.Models;
using Shalendar.Models.Dtos;

namespace Shalendar.Controllers
{
    [Route("api/[controller]")]
	[ApiController]
	[Authorize]
	public class DaysController : ControllerBase
	{
		private readonly ShalendarDbContext _context;
		private readonly IJwtHelper _jwtHelper;

		public DaysController(ShalendarDbContext context, IJwtHelper jwtHelper)
		{
			_context = context;
			_jwtHelper = jwtHelper;
		}

		#region Gets

		// GET: api/Days/{date}/{calendarId}
		[HttpGet("{date}/{calendarId}")]
		public async Task<IActionResult> GetDayId(string date, int calendarId)
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

			if (!DateTime.TryParse(date, out DateTime parsedDate))
			{
				return BadRequest("Invalid date format.");
			}

			var day = await _context.Days
				.FirstOrDefaultAsync(d => d.CalendarId == calendarId && d.Date.Date == parsedDate.Date);

			return Ok(new { id = day?.Id });
		}


		// GET: api/Days/range/{startDate}/{endDate}/{calendarId}
		[HttpGet("range/{startDate}/{endDate}/{calendarId}")]
		public async Task<IActionResult> GetExistingDaysInRange(string startDate, string endDate, int calendarId)
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

			if (!DateTime.TryParse(startDate, out DateTime parsedStartDate) || !DateTime.TryParse(endDate, out DateTime parsedEndDate))
			{
				return BadRequest("Invalid date format.");
			}

			var days = await _context.Days
				.Where(d => d.CalendarId == calendarId && d.Date.Date >= parsedStartDate.Date && d.Date.Date <= parsedEndDate.Date)
				.Select(d => new { d.Id, Date = d.Date.ToString("yyyy-MM-dd") })
				.ToListAsync();

			return Ok(new { days });
		}

		#endregion



		#region Posts

		// POST: api/Days/create
		[HttpPost("create")]
		public async Task<IActionResult> CreateDay([FromBody] CreateDayDto request)
		{
			if (!DateTime.TryParse(request.Date, out DateTime parsedDate))
			{
				return BadRequest("Invalid date format.");
			}

			var existingDay = await _context.Days
				.FirstOrDefaultAsync(d => d.CalendarId == request.CalendarId && d.Date.Date == parsedDate.Date);

			if (existingDay != null)
			{
				return Ok(new { id = existingDay.Id });
			}

			var newDay = new Day
			{
				CalendarId = request.CalendarId,
				Date = parsedDate
			};

			_context.Days.Add(newDay);
			await _context.SaveChangesAsync();

			return Ok(new { id = newDay.Id });
		}

		#endregion



		#region Puts

		#endregion



		#region Deletes

		// DELETE: api/Days/{calendarId}/{date}
		[HttpDelete("{calendarId}/{date}")]
		public async Task<IActionResult> DeleteDayIfNoTickets(int calendarId, string date)
		{
			if (!DateTime.TryParse(date, out DateTime parsedDate))
			{
				return BadRequest("Invalid date format.");
			}

			var day = await _context.Days
				.FirstOrDefaultAsync(d => d.CalendarId == calendarId && d.Date.Date == parsedDate.Date);

			if (day == null)
			{
				return NotFound("Day not found.");
			}

			bool hasTickets = await _context.Tickets
				.AnyAsync(t => (t.CurrentParentType == "ScheduledList" || t.CurrentParentType == "TodoList")
							   && t.ParentId == day.Id);

			if (!hasTickets)
			{
				_context.Days.Remove(day);
				await _context.SaveChangesAsync();
				return Ok("Day deleted successfully.");
			}

			return Ok("Day was not deleted as it still has tickets.");
		}

		#endregion
	}
}