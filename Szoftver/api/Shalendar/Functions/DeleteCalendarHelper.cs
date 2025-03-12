using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Shalendar.Contexts;
using Shalendar.Models;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Shalendar.Functions
{
	public class DeleteCalendarHelper
	{
		private readonly ShalendarDbContext _context;
		private readonly GroupManagerService _groupManager;
		private readonly IHubContext<CalendarHub> _calendarHub;

		public DeleteCalendarHelper(ShalendarDbContext context, GroupManagerService groupManager, IHubContext<CalendarHub> calendarHub)
		{
			_context = context;
			_groupManager = groupManager;
			_calendarHub = calendarHub;
		}

		/// <summary>
		/// Determines if the given user can delete the calendar by checking their permission level and ensuring they are the sole owner.
		/// </summary>
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

		/// <summary>
		/// Deletes the specified calendar and all related data, including permissions, days, calendar lists, and associated tickets, while ensuring users with this calendar as their default are updated.
		/// </summary>
		public async Task DeleteCalendar(int calendarId)
		{
			using var transaction = await _context.Database.BeginTransactionAsync();
			try
			{
				var usersWithDefaultCalendar = await _context.Users
					.Where(u => u.DefaultCalendarId == calendarId)
					.ToListAsync();

				foreach (var user in usersWithDefaultCalendar)
				{
					user.DefaultCalendarId = null;
				}
				await _context.SaveChangesAsync();

				var permissions = _context.CalendarPermissions.Where(cp => cp.CalendarId == calendarId);
				_context.CalendarPermissions.RemoveRange(permissions);

				var days = _context.Days.Where(d => d.CalendarId == calendarId);
				var dayIds = await days.Select(d => d.Id).ToListAsync();

				var calendarLists = _context.CalendarLists.Where(cl => cl.CalendarId == calendarId);
				var calendarListIds = await calendarLists.Select(cl => cl.Id).ToListAsync();

				var tickets = _context.Tickets
					.Where(t => dayIds.Contains(t.ParentId) || calendarListIds.Contains(t.CalendarListId));

				_context.Tickets.RemoveRange(tickets);
				await _context.SaveChangesAsync();

				_context.Days.RemoveRange(days);
				_context.CalendarLists.RemoveRange(calendarLists);
				await _context.SaveChangesAsync();

				var calendar = await _context.Calendars.FindAsync(calendarId);
				if (calendar != null)
				{
					_context.Calendars.Remove(calendar);
				}

				await _context.SaveChangesAsync();
				await transaction.CommitAsync();

				await _calendarHub.Clients.Group(calendarId.ToString()).SendAsync("CalendarDeleted");
			}
			catch (Exception)
			{
				await transaction.RollbackAsync();
				throw;
			}
		}
	}
}