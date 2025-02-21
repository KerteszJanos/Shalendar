using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shalendar.Contexts;
using Shalendar.Models;

namespace Shalendar.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class TicketsController : ControllerBase
	{
		private readonly ShalendarDbContext _context;

		public TicketsController(ShalendarDbContext context)
		{
			_context = context;
		}

		#region Gets

		#endregion

		#region Posts

		// POST: api/Tickets
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

		#region Puts

		#endregion

		#region Deletes

		// DELETE: api/Tickets/{id}
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

		#region private methods

		#endregion
	}
}
