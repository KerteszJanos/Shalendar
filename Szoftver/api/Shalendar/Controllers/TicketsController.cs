using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shalendar.Contexts;
using Shalendar.Models;

namespace Shalendar.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	[Authorize]
	public class TicketsController : ControllerBase
	{
		private readonly ShalendarDbContext _context;

		public TicketsController(ShalendarDbContext context)
		{
			_context = context;
		}

		#region Gets
		[HttpGet("todolist/{date}/{calendarId}")]
		public async Task<ActionResult<IEnumerable<object>>> GetTodoListTicketsByDateAndCalendar(string date, int calendarId)
		{
			if (!DateTime.TryParse(date, out DateTime parsedDate))
			{
				return BadRequest("Invalid date format.");
			}

			DateTime selectedDate = parsedDate.Date;

			var tickets = await _context.Tickets
				.Where(t => t.CurrentParentType == "TodoList"
							&& t.StartTime == null
							&& _context.Days
								.Any(d => d.Id == t.ParentId
										  && d.CalendarId == calendarId
										  && EF.Functions.DateDiffDay(d.Date, selectedDate) == 0))
				.Select(t => new
				{
					t.Id,
					t.Name,
					t.Description,
					t.Priority,
					t.CalendarListId,
					Color = _context.CalendarLists
								.Where(c => c.Id == t.CalendarListId)
								.Select(c => c.Color)
								.FirstOrDefault()
				})
				.ToListAsync();

			return Ok(tickets);
		}

		#endregion

		#region Posts

		[HttpPost]
		public async Task<ActionResult<Ticket>> CreateTicket([FromBody] Ticket ticket)
		{
			if (string.IsNullOrWhiteSpace(ticket.Name))
			{
				return BadRequest("Ticket name is required.");
			}

			_context.Tickets.Add(ticket);
			await _context.SaveChangesAsync();

			return CreatedAtAction(nameof(CreateTicket), new { id = ticket.Id }, ticket);
		}

		#endregion

		#region Deletes

		[HttpDelete("{id}")]
		public async Task<IActionResult> DeleteTicket(int id)
		{
			var ticket = await _context.Tickets.FindAsync(id);
			if (ticket == null)
			{
				return NotFound();
			}

			_context.Tickets.Remove(ticket);
			await _context.SaveChangesAsync();

			return NoContent();
		}

		#endregion
	}
}
