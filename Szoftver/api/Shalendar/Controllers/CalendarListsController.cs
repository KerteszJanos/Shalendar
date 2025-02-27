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

		// PUT: api/CalendarLists/{id}
		[HttpPut("{id}")]
		public async Task<IActionResult> UpdateCalendarList(int id, [FromBody] CalendarList updatedList)
		{
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

			return NoContent();
		}

		#endregion



		#region Deletes

		// DELETE: api/CalendarLists/{id}
		[HttpDelete("{id}")]
		public async Task<IActionResult> DeleteCalendarList(int id)
		{
			using var transaction = await _context.Database.BeginTransactionAsync(); // Tranzakció biztosításához

			try
			{
				var list = await _context.CalendarLists.FindAsync(id);
				if (list == null)
				{
					return NotFound("Calendar list not found.");
				}

				// 1. Lekérjük és töröljük az összes kapcsolódó ticketet (ha van) (Gpt generated)
				var relatedTickets = await _context.Tickets
					.Where(t => t.CalendarListId == id)
					.ToListAsync();

				if (relatedTickets.Any())
				{
					_context.Tickets.RemoveRange(relatedTickets);
					await _context.SaveChangesAsync(); // Itt mentjük el először a ticket törléseket
				}

				// 2. Töröljük a listát, ha már nincs rá hivatkozás
				_context.CalendarLists.Remove(list);
				await _context.SaveChangesAsync();

				await transaction.CommitAsync(); // Tranzakció lezárása
				return NoContent();
			}
			catch (DbUpdateException ex)
			{
				await transaction.RollbackAsync(); // Ha hiba van, visszavonjuk a módosításokat
				return StatusCode(500, $"Error deleting list and related tickets: {ex.Message}");
			}
		}




		#endregion



		#region private methods



		#endregion
	}
}
