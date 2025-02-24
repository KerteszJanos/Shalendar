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
					t.CurrentPosition,
					Color = _context.CalendarLists
								.Where(c => c.Id == t.CalendarListId)
								.Select(c => c.Color)
								.FirstOrDefault()
				})
				.ToListAsync();

			return Ok(tickets);
		}

		[HttpGet("scheduled/{date}/{calendarId}")]
		public async Task<ActionResult<IEnumerable<object>>> GetScheduledListTicketsByDateAndCalendar(string date, int calendarId)
		{
			if (!DateTime.TryParse(date, out DateTime parsedDate))
			{
				return BadRequest("Invalid date format.");
			}

			DateTime selectedDate = parsedDate.Date;

			var tickets = await _context.Tickets
				.Where(t => t.CurrentParentType == "ScheduledList"
							&& t.StartTime != null
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
					t.StartTime,
					t.EndTime,
					t.CurrentPosition,
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


		[HttpPost("ScheduleTicket")]
		public async Task<IActionResult> ScheduleTicket([FromBody] ScheduleTicketDto dto)
		{
			// Validate input parameters
			if (dto == null || dto.TicketId <= 0)
			{
				return BadRequest("Invalid data.");
			}

			using (var transaction = await _context.Database.BeginTransactionAsync())
			{
				try
				{
					// Retrieve or create day record based on CalendarId and Date
					var dayRecord = await _context.Days
						.FirstOrDefaultAsync(d => d.CalendarId == dto.CalendarId && d.Date.Date == dto.Date.Date);

					if (dayRecord == null)
					{
						dayRecord = new Day
						{
							CalendarId = dto.CalendarId,
							Date = dto.Date.Date
						};
						_context.Days.Add(dayRecord);
						await _context.SaveChangesAsync();
					}

					// Retrieve the ticket by its Id
					var ticket = await _context.Tickets.FindAsync(dto.TicketId);
					if (ticket == null)
					{
						return NotFound("Ticket not found.");
					}

					// Update ticket properties using values from the DTO
					ticket.ParentId = dayRecord.Id;
					ticket.StartTime = dto.StartTime;
					ticket.EndTime = dto.EndTime;

					if (dto.StartTime.HasValue && dto.EndTime.HasValue)
					{
						ticket.CurrentParentType = "ScheduledList";
					}
					else
					{
						ticket.CurrentParentType = "TodoList";
					}

					await _context.SaveChangesAsync();
					await transaction.CommitAsync();

					return Ok(ticket);
				}
				catch (Exception ex)
				{
					await transaction.RollbackAsync();
					return StatusCode(500, "An error occurred: " + ex.Message);
				}
			}
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

		#region Updates
		[HttpPut("reorder")]
		public async Task<IActionResult> ReorderTickets([FromBody] List<TicketOrderUpdate> orderUpdates)
		{
			if (orderUpdates == null || !orderUpdates.Any())
			{
				return BadRequest("Invalid order update data.");
			}

			foreach (var update in orderUpdates)
			{
				var ticket = await _context.Tickets.FindAsync(update.TicketId);
				if (ticket != null)
				{
					ticket.CurrentPosition = update.NewPosition;
				}
			}
			await _context.SaveChangesAsync();
			return NoContent();
		}
		#endregion
	}
}
