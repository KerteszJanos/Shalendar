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
	public class DaysController : ControllerBase
	{
		private readonly ShalendarDbContext _context;

		public DaysController(ShalendarDbContext context)
		{
			_context = context;
		}

		#region Gets
		[HttpGet("{date}/{calendarId}")]
		public async Task<IActionResult> GetDayId(string date, int calendarId)
		{
			if (!DateTime.TryParse(date, out DateTime parsedDate))
			{
				return BadRequest("Invalid date format.");
			}

			var day = await _context.Days
				.FirstOrDefaultAsync(d => d.CalendarId == calendarId && d.Date.Date == parsedDate.Date);

			// Ha nincs találat, akkor 200 OK választ küldünk, de az ID null lesz
			return Ok(new { id = day?.Id });
		}

		#endregion

		#region Posts
		[HttpPost("create")]
		public async Task<IActionResult> CreateDay([FromBody] CreateDayRequest request)
		{
			if (!DateTime.TryParse(request.Date, out DateTime parsedDate))
			{
				return BadRequest("Invalid date format.");
			}

			// Ellenőrizzük, hogy létezik-e már a nap
			var existingDay = await _context.Days
				.FirstOrDefaultAsync(d => d.CalendarId == request.CalendarId && d.Date.Date == parsedDate.Date);

			if (existingDay != null)
			{
				return Ok(new { id = existingDay.Id });
			}

			// Ha nem létezik, létrehozunk egy új napot
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

		#region Deletes
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

		#region Updates
		// PUT methods will go here in the future
		#endregion
	}
}