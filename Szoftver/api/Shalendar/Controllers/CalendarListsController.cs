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
	public class CalendarListsController : ControllerBase
    {
        private readonly ShalendarDbContext _context;

        public CalendarListsController(ShalendarDbContext context)
        {
            _context = context;
        }



		#region Gets

		// GET: api/CalendarLists/calendar/{calendarId}
		[HttpGet("calendar/{calendarId}")]
		public async Task<ActionResult<IEnumerable<object>>> GetCalendarListsByCalendarId(int calendarId)
		{
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
							ticket.Priority
						})
						.ToList()
				})
				.ToListAsync();

			if (lists == null || lists.Count == 0)
			{
				return NotFound("No list found for this calendar");
			}

			return Ok(lists);
		}


		#endregion



		#region Posts

		// POST: api/CalendarLists
		[HttpPost]
		public async Task<ActionResult<CalendarList>> PostCalendarList(CalendarList calendarList)
		{
			// Ellenőrizzük, hogy a naptár létezik-e
			var calendarExists = await _context.Calendars.AnyAsync(c => c.Id == calendarList.CalendarId);
			if (!calendarExists)
			{
				return BadRequest("The specified CalendarId does not exist.");
			}

			// Új lista mentése
			_context.CalendarLists.Add(calendarList);
			await _context.SaveChangesAsync();

			return CreatedAtAction(nameof(GetCalendarListsByCalendarId), new { calendarId = calendarList.CalendarId }, calendarList);
		}

		#endregion



		#region Puts



		#endregion



		#region Deletes



		#endregion



		#region private methods



		#endregion
	}
}
