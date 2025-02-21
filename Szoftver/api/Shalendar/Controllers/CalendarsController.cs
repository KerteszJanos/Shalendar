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

		#endregion



		#region Posts



		#endregion



		#region Puts



		#endregion



		#region Deletes



		#endregion
	}
}
