using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Shalendar.Contexts;
using Shalendar.Functions;
using Shalendar.Models;
using Shalendar.Models.Dtos;

namespace Shalendar.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	[Authorize]
	public class TicketsController : ControllerBase
	{
		private readonly ShalendarDbContext _context;
		private readonly JwtHelper _jwtHelper;
		private readonly CopyTicketHelper _copyTicketHelper;
		private readonly IHubContext<CalendarHub> _calendarHub;

		public TicketsController(ShalendarDbContext context, JwtHelper jwtHelper, CopyTicketHelper copyTicketHelper, IHubContext<CalendarHub> calendarHub)
		{
			_context = context;
			_jwtHelper = jwtHelper;
			_copyTicketHelper = copyTicketHelper;
			_calendarHub = calendarHub;
		}

		#region Gets

		// GET: api/Tickets/todolist/{date}/{calendarId}
		[HttpGet("todolist/{date}/{calendarId}")]
		public async Task<ActionResult<IEnumerable<object>>> GetTodoListTicketsByDateAndCalendar(string date, int calendarId)
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

			DateTime selectedDate = parsedDate.Date;

			bool dayExists = await _context.Days
				.AnyAsync(d => d.CalendarId == calendarId && EF.Functions.DateDiffDay(d.Date, selectedDate) == 0);

			if (!dayExists)
			{
				return Ok(new List<object>());
			}

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
					t.IsCompleted,
					Color = _context.CalendarLists
								.Where(c => c.Id == t.CalendarListId)
								.Select(c => c.Color)
								.FirstOrDefault()
				})
				.ToListAsync();

			return Ok(tickets);
		}

		// GET: api/Tickets/scheduled/{date}/{calendarId}
		[HttpGet("scheduled/{date}/{calendarId}")]
		public async Task<ActionResult<IEnumerable<object>>> GetScheduledListTicketsByDateAndCalendar(string date, int calendarId)
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

			DateTime selectedDate = parsedDate.Date;

			bool dayExists = await _context.Days
				.AnyAsync(d => d.CalendarId == calendarId && EF.Functions.DateDiffDay(d.Date, selectedDate) == 0);

			if (!dayExists)
			{
				return Ok(new List<object>());
			}

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
					t.IsCompleted,
					Color = _context.CalendarLists
								.Where(c => c.Id == t.CalendarListId)
								.Select(c => c.Color)
								.FirstOrDefault()
				})
				.ToListAsync();

			return Ok(tickets);
		}

		// GET: api/Tickets/AllDailyTickets/{date}/{calendarId}"
		[HttpGet("AllDailyTickets/{date}/{calendarId}")]
		public async Task<ActionResult<IEnumerable<object>>> GetAllDailyTicketsByDateAndCalendar(string date, int calendarId)
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

			DateTime selectedDate = parsedDate.Date;

			bool dayExists = await _context.Days
				.AnyAsync(d => d.CalendarId == calendarId && EF.Functions.DateDiffDay(d.Date, selectedDate) == 0);

			if (!dayExists)
			{
				return Ok(new List<object>());
			}

			var tickets = await _context.Tickets
				.Where(t => _context.Days
								.Any(d => d.Id == t.ParentId
										  && d.CalendarId == calendarId
										  && EF.Functions.DateDiffDay(d.Date, selectedDate) == 0))
				.Select(t => new
				{
					t.Id,
					t.Name,
					t.StartTime,
					t.CurrentPosition,
					t.IsCompleted,
					Color = _context.CalendarLists
								.Where(c => c.Id == t.CalendarListId)
								.Select(c => c.Color)
								.FirstOrDefault()
				})
				.ToListAsync();

			return Ok(tickets);
		}

		// GET: api/Tickets/AllDailyTickets/{dayId}
		[HttpGet("AllDailyTickets/{dayId}")]
		public async Task<ActionResult<IEnumerable<object>>> GetAllDailyTicketsByDayId(int dayId)
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

			var tickets = await _context.Tickets
				.Where(t => t.ParentId == dayId)
				.Select(t => new
				{
					t.Id,
					t.Name,
					t.StartTime,
					t.CurrentPosition,
					t.IsCompleted,
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

		// POST: api/Tickets
		[HttpPost]
		public async Task<ActionResult<Ticket>> CreateTicket([FromBody] Ticket ticket)
		{
			var requiredPermission = "write";
			var hasPermission = await _jwtHelper.HasCalendarPermission(HttpContext, requiredPermission);

			if (!hasPermission)
			{
				return new ObjectResult(new { message = $"Required permission: {requiredPermission}" })
				{
					StatusCode = StatusCodes.Status403Forbidden
				};
			}

			if (string.IsNullOrWhiteSpace(ticket.Name))
			{
				return BadRequest("Ticket name is required.");
			}

			_context.Tickets.Add(ticket);
			await _context.SaveChangesAsync();


			if (!HttpContext.Request.Headers.TryGetValue("X-Calendar-Id", out var calendarIdHeader) ||
			!int.TryParse(calendarIdHeader, out int calendarId))
			{
				return BadRequest();
			}

			if (ticket.CurrentParentType == "CalendarList")
			{
				await _calendarHub.Clients.Group(calendarId.ToString()).SendAsync("TicketCreatedInCalendarLists");
			}
			else
			{
				var day = await _context.Days.FindAsync(ticket.ParentId);

				if (day == null)
				{
					return BadRequest("Associated Day not found.");
				}

				await _calendarHub.Clients.Group(calendarId.ToString()).SendAsync("TicketCreatedInDayView", day.Date);
			}


			return CreatedAtAction(nameof(CreateTicket), new { id = ticket.Id }, ticket);
		}

		// POST: api/Tickets/ScheduleTicket
		[HttpPost("ScheduleTicket")]
		public async Task<IActionResult> ScheduleTicket([FromBody] ScheduleTicketDto dto)
		{
			var requiredPermission = "write";
			var hasPermission = await _jwtHelper.HasCalendarPermission(HttpContext, requiredPermission);

			if (!hasPermission)
			{
				return new ObjectResult(new { message = $"Required permission: {requiredPermission}" })
				{
					StatusCode = StatusCodes.Status403Forbidden
				};
			}

			if (dto == null || dto.TicketId <= 0)
			{
				return BadRequest("Invalid data.");
			}

			using (var transaction = await _context.Database.BeginTransactionAsync())
			{
				try
				{
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

					var ticket = await _context.Tickets.FindAsync(dto.TicketId);
					if (ticket == null)
					{
						return NotFound("Ticket not found.");
					}

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

					if (!HttpContext.Request.Headers.TryGetValue("X-Calendar-Id", out var calendarIdHeader) ||
						!int.TryParse(calendarIdHeader, out int calendarId))
					{
						return BadRequest();
					}
					await _calendarHub.Clients.Group(calendarId.ToString()).SendAsync("TicketScheduled", dayRecord.Date);

					return Ok(ticket);
				}
				catch (Exception ex)
				{
					await transaction.RollbackAsync();
					return StatusCode(500, "An error occurred: " + ex.Message);
				}
			}
		}

		// POST: api/Tickets/copy-ticket
		[HttpPost("copy-ticket")]
		public async Task<IActionResult> CopyTicket(int ticketId, int calendarId, DateTime? date = null)
		{
			var requiredPermission = "read";
			var hasPermission = await _jwtHelper.HasCalendarPermission(HttpContext, requiredPermission);

			if (!hasPermission)
			{
				if (!HttpContext.Request.Headers.TryGetValue("X-Calendar-Id", out var calendarIdHeader) || !int.TryParse(calendarIdHeader, out calendarId))
				{
					var calendarName = await _context.Calendars
						.Where(c => c.Id == calendarId)
						.Select(c => c.Name)
						.FirstOrDefaultAsync();

					return new ObjectResult(new { message = $"Required permission: {requiredPermission} for calendar: '{calendarName}'" })
					{
						StatusCode = StatusCodes.Status403Forbidden
					};
				}
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

			var result = await _copyTicketHelper.CopyTicketAsync(_context, ticketId, calendarId, date);

			if (date == null)
			{
				await _calendarHub.Clients.Group(calendarId.ToString()).SendAsync("TicketCopiedInCalendarLists");
			}
			else
			{
				await _calendarHub.Clients.Group(calendarId.ToString()).SendAsync("TicketCopiedInCalendar", date);
			}

			return result ? Ok("Ticket successfully copied or already existed.") : NotFound("Ticket or CalendarList not found.");
		}

		#endregion



		#region Puts

		// PUT: api/Tickets/reorder
		[HttpPut("reorder")]
		public async Task<IActionResult> ReorderTickets([FromBody] List<TicketOrderUpdateDto> orderUpdates)
		{
			var requiredPermission = "write";
			var hasPermission = await _jwtHelper.HasCalendarPermission(HttpContext, requiredPermission);

			if (!hasPermission)
			{
				return new ObjectResult(new { message = $"Required permission: {requiredPermission}" })
				{
					StatusCode = StatusCodes.Status403Forbidden
				};
			}

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

			if (!HttpContext.Request.Headers.TryGetValue("X-Calendar-Id", out var calendarIdHeader) ||
			!int.TryParse(calendarIdHeader, out int calendarId))
			{
				return BadRequest();
			}


			var firstTicketId = orderUpdates.FirstOrDefault()?.TicketId;
			if (firstTicketId == null)
			{
				return BadRequest("No valid tickets found.");
			}

			var firstTicket = await _context.Tickets
				.Where(t => t.Id == firstTicketId)
				.Select(t => new { t.CurrentParentType, t.ParentId })
				.FirstOrDefaultAsync();

			if (firstTicket?.CurrentParentType == "CalendarList")
			{
				await _calendarHub.Clients.Group(calendarId.ToString()).SendAsync("TicketReorderedInCalendarLists");
			}
			else
			{
				var day = await _context.Days.FindAsync(firstTicket?.ParentId);

				if (day == null)
				{
					return BadRequest("Associated Day not found.");
				}

				await _calendarHub.Clients.Group(calendarId.ToString()).SendAsync("TicketReorderedInDayView", day.Date);
			}

			return NoContent();
		}

		// PUT: api/Tickets/move-to-calendar/{ticketId}
		[HttpPut("move-to-calendar/{ticketId}")]
		public async Task<IActionResult> MoveTicketToCalendar(int ticketId)
		{
			var requiredPermission = "write";
			var hasPermission = await _jwtHelper.HasCalendarPermission(HttpContext, requiredPermission);

			if (!hasPermission)
			{
				return new ObjectResult(new { message = $"Required permission: {requiredPermission}" })
				{
					StatusCode = StatusCodes.Status403Forbidden
				};
			}

			var ticket = await _context.Tickets.FindAsync(ticketId);
			if (ticket == null)
			{
				return NotFound("Ticket not found.");
			}

			var prevParentId = ticket.ParentId;

			ticket.CurrentParentType = "CalendarList";
			ticket.ParentId = ticket.CalendarListId;
			ticket.StartTime = null;
			ticket.EndTime = null;

			try
			{
				await _context.SaveChangesAsync();

				if (!HttpContext.Request.Headers.TryGetValue("X-Calendar-Id", out var calendarIdHeader) ||
					!int.TryParse(calendarIdHeader, out int calendarId))
				{
					return BadRequest();
				}
				var day = await _context.Days.FindAsync(prevParentId);

				if (day == null)
				{
					return BadRequest("Associated Day not found.");
				}

				await _calendarHub.Clients.Group(calendarId.ToString()).SendAsync("TicketMovedBackToCalendar", day.Date);

				return Ok(ticket);
			}
			catch (Exception ex)
			{
				return StatusCode(500, $"An error occurred: {ex.Message}");
			}
		}

		// PUT: api/Tickets/updateTicket
		[HttpPut("updateTicket")]
		public async Task<IActionResult> UpdateTicket([FromBody] UpdateTicketDto updatedTicketDto)
		{
			var requiredPermission = "write";
			var hasPermission = await _jwtHelper.HasCalendarPermission(HttpContext, requiredPermission);

			if (!hasPermission)
			{
				return new ObjectResult(new { message = $"Required permission: {requiredPermission}" })
				{
					StatusCode = StatusCodes.Status403Forbidden
				};
			}

			var ticket = await _context.Tickets.FindAsync(updatedTicketDto.Id);
			if (ticket == null)
			{
				return NotFound("Ticket not found.");
			}

			var prevParentType = ticket.CurrentParentType;

			ticket.Name = updatedTicketDto.Name;
			ticket.Description = updatedTicketDto.Description;
			ticket.Priority = updatedTicketDto.Priority;
			ticket.StartTime = updatedTicketDto.StartTime;
			ticket.EndTime = updatedTicketDto.EndTime;

			if (ticket.CurrentParentType != "CalendarList")
			{
				if (ticket.StartTime.HasValue)
				{
					if (ticket.CurrentParentType != "ScheduledList")
					{
						ticket.CurrentParentType = "ScheduledList";
					}
				}
				else
				{
					if (ticket.CurrentParentType != "TodoList")
					{
						ticket.CurrentParentType = "TodoList";
					}
				}
			}

			try
			{
				await _context.SaveChangesAsync();

				if (!HttpContext.Request.Headers.TryGetValue("X-Calendar-Id", out var calendarIdHeader) ||
					!int.TryParse(calendarIdHeader, out int calendarId))
				{
					return BadRequest();
				}


				if (prevParentType == "CalendarList")
				{
					await _calendarHub.Clients.Group(calendarId.ToString()).SendAsync("TicketUpdatedInCalendarLists");
				}
				else
				{
					var day = await _context.Days.FindAsync(ticket.ParentId);

					if (day == null)
					{
						return BadRequest("Associated Day not found.");
					}

					await _calendarHub.Clients.Group(calendarId.ToString()).SendAsync("TicketUpdatedInDayView", day.Date);
				}

				return Ok(ticket);
			}
			catch (Exception ex)
			{
				return StatusCode(500, $"An error occurred while updating the ticket: {ex.Message}");
			}
		}

		// PUT: api/Tickets/updateTicketCompleted
		[HttpPut("updateTicketCompleted")]
		public async Task<IActionResult> UpdateTicketCompleted(int ticketId, bool isCompleted)
		{
			var requiredPermission = "write";
			var hasPermission = await _jwtHelper.HasCalendarPermission(HttpContext, requiredPermission);

			if (!hasPermission)
			{
				return new ObjectResult(new { message = $"Required permission: {requiredPermission}" })
				{
					StatusCode = StatusCodes.Status403Forbidden
				};
			}

			var ticket = await _context.Tickets.FindAsync(ticketId);
			if (ticket == null)
			{
				return NotFound(new { message = "Ticket not found" });
			}

			ticket.IsCompleted = isCompleted;
			await _context.SaveChangesAsync();

			if (!HttpContext.Request.Headers.TryGetValue("X-Calendar-Id", out var calendarIdHeader) ||
				!int.TryParse(calendarIdHeader, out int calendarId))
			{
				return BadRequest();
			}

			if (ticket.CurrentParentType == "CalendarList")
			{
				await _calendarHub.Clients.Group(calendarId.ToString()).SendAsync("TicketCompletedUpdatedInCalendarLists");
			}
			else
			{
				var day = await _context.Days.FindAsync(ticket.ParentId);

				if (day == null)
				{
					return BadRequest("Associated Day not found.");
				}

				await _calendarHub.Clients.Group(calendarId.ToString()).SendAsync("TicketCompletedUpdatedInDayView", day.Date);
			}

			return Ok();
		}

		// PUT: api/Tickets/changeDate/{ticketId}
		[HttpPut("changeDate/{ticketId}")]
		public async Task<IActionResult> ChangeTicketDate(int ticketId, [FromBody] string date)
		{
			var requiredPermission = "write";
			var hasPermission = await _jwtHelper.HasCalendarPermission(HttpContext, requiredPermission);

			if (!hasPermission)
			{
				return new ObjectResult(new { message = $"Required permission: {requiredPermission}" })
				{
					StatusCode = StatusCodes.Status403Forbidden
				};
			}

			var ticket = await _context.Tickets.FindAsync(ticketId);
			if (ticket == null)
			{
				return NotFound("Ticket not found.");
			}

			if (!DateTime.TryParse(date, out DateTime newDate))
			{
				return BadRequest("Invalid date format.");
			}

			var previousParentId = ticket.ParentId;
			var calendarId = await _context.Days
				.Where(d => d.Id == ticket.ParentId)
				.Select(d => d.CalendarId)
				.FirstOrDefaultAsync();

			if (calendarId == 0)
			{
				return BadRequest("Calendar not found for the ticket.");
			}

			var dayRecord = await _context.Days
				.FirstOrDefaultAsync(d => d.CalendarId == calendarId && d.Date.Date == newDate.Date);

			if (dayRecord == null)
			{
				dayRecord = new Day
				{
					CalendarId = calendarId,
					Date = newDate.Date
				};
				_context.Days.Add(dayRecord);
				await _context.SaveChangesAsync();
			}

			ticket.ParentId = dayRecord.Id;

			await _context.SaveChangesAsync();

			if (!HttpContext.Request.Headers.TryGetValue("X-Calendar-Id", out var calendarIdHeader) ||
				!int.TryParse(calendarIdHeader, out int calendarIdInt))
			{
				return BadRequest();
			}

			var previousDay = await _context.Days.FindAsync(previousParentId); // GPT generated - Lekérjük a korábbi napot

			if (previousDay != null)
			{
				await _calendarHub.Clients.Group(calendarIdInt.ToString()).SendAsync("TicketMovedBetweenDays",dayRecord.Date);
				await _calendarHub.Clients.Group(calendarIdInt.ToString()).SendAsync("TicketMovedBetweenDays", previousDay.Date);
			}

			return Ok(ticket);
		}

		#endregion



		#region Deletes

		// DELETE: api/Tickets/api/Tickets/
		[HttpDelete("{id}")]
		public async Task<IActionResult> DeleteTicket(int id)
		{
			var requiredPermission = "write";
			var hasPermission = await _jwtHelper.HasCalendarPermission(HttpContext, requiredPermission);

			if (!hasPermission)
			{
				return new ObjectResult(new { message = $"Required permission: {requiredPermission}" })
				{
					StatusCode = StatusCodes.Status403Forbidden
				};
			}

			var ticket = await _context.Tickets.FindAsync(id);
			if (ticket == null)
			{
				return NotFound();
			}

			_context.Tickets.Remove(ticket);
			await _context.SaveChangesAsync();

			if (!HttpContext.Request.Headers.TryGetValue("X-Calendar-Id", out var calendarIdHeader) ||
				!int.TryParse(calendarIdHeader, out int calendarId))
			{
				return BadRequest();
			}

			if (ticket.CurrentParentType == "CalendarList")
			{

				await _calendarHub.Clients.Group(calendarId.ToString()).SendAsync("TicketDeletedInCalendarLists");
			}
			else
			{
				var day = await _context.Days.FindAsync(ticket.ParentId);
				if (day == null)
				{
					return BadRequest("Associated Day not found.");
				}

				await _calendarHub.Clients.Group(calendarId.ToString()).SendAsync("TicketDeletedInDayView", day.Date);
			}

			return NoContent();
		}

		#endregion
	}
}
