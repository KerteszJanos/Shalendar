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
    public class CalendarListsController : ControllerBase
    {
        private readonly ShalendarDbContext _context;

        public CalendarListsController(ShalendarDbContext context)
        {
            _context = context;
        }

		//TODO: kell auth!
		// GET: api/CalendarLists
		[HttpGet]
        public async Task<ActionResult<IEnumerable<CalendarList>>> GetCalendarLists()
        {
            return await _context.CalendarLists.ToListAsync();
        }

		//TODO: kell auth!
		// GET: api/CalendarLists/5
		[HttpGet("{id}")]
        public async Task<ActionResult<CalendarList>> GetCalendarList(int id)
        {
            var calendarList = await _context.CalendarLists.FindAsync(id);

            if (calendarList == null)
            {
                return NotFound();
            }

            return calendarList;
        }

		//TODO: kell auth!
		// PUT: api/CalendarLists/5
		// To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
		[HttpPut("{id}")]
        public async Task<IActionResult> PutCalendarList(int id, CalendarList calendarList)
        {
            if (id != calendarList.Id)
            {
                return BadRequest();
            }

            _context.Entry(calendarList).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!CalendarListExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

		//TODO: kell auth!
		// POST: api/CalendarLists
		// To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
		[HttpPost]
        public async Task<ActionResult<CalendarList>> PostCalendarList(CalendarList calendarList)
        {
            _context.CalendarLists.Add(calendarList);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetCalendarList", new { id = calendarList.Id }, calendarList);
        }

		//TODO: kell auth!
		// DELETE: api/CalendarLists/5
		[HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCalendarList(int id)
        {
            var calendarList = await _context.CalendarLists.FindAsync(id);
            if (calendarList == null)
            {
                return NotFound();
            }

            _context.CalendarLists.Remove(calendarList);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool CalendarListExists(int id)
        {
            return _context.CalendarLists.Any(e => e.Id == id);
        }
    }
}
