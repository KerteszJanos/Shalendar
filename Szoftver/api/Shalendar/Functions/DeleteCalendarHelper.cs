using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Shalendar.Contexts;
using Shalendar.Models;

namespace Shalendar.Functions
{
	public class DeleteCalendarHelper
	{
		private readonly ShalendarDbContext _context;

		public DeleteCalendarHelper(ShalendarDbContext context)
		{
			_context = context;
		}
		public async Task<bool> ShouldDeleteCalendar(int userId, int calendarId)
		{
			var userPermission = await _context.CalendarPermissions
				.FirstOrDefaultAsync(cp => cp.CalendarId == calendarId && cp.UserId == userId);

			if (userPermission == null)
			{
				return false;
			}

			if (userPermission.PermissionType != "owner")
			{
				_context.CalendarPermissions.Remove(userPermission);
				await _context.SaveChangesAsync();
				return false;
			}

			var ownerCount = await _context.CalendarPermissions
				.CountAsync(cp => cp.CalendarId == calendarId && cp.PermissionType == "owner");

			if (ownerCount > 1)
			{
				_context.CalendarPermissions.Remove(userPermission);
				await _context.SaveChangesAsync();
				return false;
			}

			return true;
		}

		public async Task DeleteCalendar(int calendarId)
		{
			using var transaction = await _context.Database.BeginTransactionAsync();
			try
			{
				// Remove calendar as default calendar for users
				var usersWithDefaultCalendar = await _context.Users
					.Where(u => u.DefaultCalendarId == calendarId)
					.ToListAsync();

				foreach (var user in usersWithDefaultCalendar)
				{
					user.DefaultCalendarId = null;
				}
				await _context.SaveChangesAsync();

				// Remove calendar permissions
				var permissions = _context.CalendarPermissions.Where(cp => cp.CalendarId == calendarId);
				_context.CalendarPermissions.RemoveRange(permissions);

				// Get all related days
				var days = _context.Days.Where(d => d.CalendarId == calendarId);
				var dayIds = await days.Select(d => d.Id).ToListAsync();

				// Get all related calendar lists
				var calendarLists = _context.CalendarLists.Where(cl => cl.CalendarId == calendarId);
				var calendarListIds = await calendarLists.Select(cl => cl.Id).ToListAsync();

				// Remove all related tickets (linked to a day or a calendar list) - GPT generated
				var tickets = _context.Tickets
					.Where(t => dayIds.Contains(t.ParentId) || calendarListIds.Contains(t.CalendarListId));

				_context.Tickets.RemoveRange(tickets);
				await _context.SaveChangesAsync(); // Ensure tickets are deleted before their references

				// Remove all related data
				_context.Days.RemoveRange(days);
				_context.CalendarLists.RemoveRange(calendarLists);
				await _context.SaveChangesAsync();

				// Remove the calendar itself
				var calendar = await _context.Calendars.FindAsync(calendarId);
				if (calendar != null)
				{
					_context.Calendars.Remove(calendar);
				}

				// Commit the transaction
				await _context.SaveChangesAsync();
				await transaction.CommitAsync();
			}
			catch (Exception)
			{
				await transaction.RollbackAsync();
				throw;
			}
		}
	}
}