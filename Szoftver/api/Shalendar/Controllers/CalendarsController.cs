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

		public CalendarsController(ShalendarDbContext context)
		{
			_context = context;
		}

		#region Gets

		// GET: api/Calendars/5
		[HttpGet("{id}")]
		public async Task<ActionResult<Calendar>> GetCalendar(int id)
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


		#endregion

		#region Puts

		#endregion

		#region Deletes

		#endregion
	}
}
