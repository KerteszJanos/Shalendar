using Microsoft.EntityFrameworkCore;
using Shalendar.Contexts;
using Shalendar.Functions.Interfaces;
using Shalendar.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Shalendar.Functions
{
	public class CopyTicketHelper : ICopyTicketHelper
	{
		/// <summary>
		/// Copies a ticket to a specified calendar and date, ensuring the corresponding calendar list and day exist, while preventing duplicate tickets.
		/// </summary>
		public async Task<bool> CopyTicketAsync(ShalendarDbContext context, int ticketId, int calendarId, DateTime? date)
		{
			using (var transaction = await context.Database.BeginTransactionAsync())
			{
				try
				{
					var ticket = await context.Tickets.FindAsync(ticketId);
					if (ticket == null) return false;

					var originalCalendarList = await context.CalendarLists.FindAsync(ticket.CalendarListId);
					if (originalCalendarList == null) return false;

					var targetCalendarList = await context.CalendarLists
						.FirstOrDefaultAsync(cl => cl.CalendarId == calendarId && cl.Name == originalCalendarList.Name && cl.Color == originalCalendarList.Color);

					if (targetCalendarList == null)
					{
						targetCalendarList = new CalendarList
						{
							Name = originalCalendarList.Name,
							Color = originalCalendarList.Color,
							CalendarId = calendarId
						};
						context.CalendarLists.Add(targetCalendarList);
						await context.SaveChangesAsync();
					}

					int targetParentId = targetCalendarList.Id;

					if (date.HasValue)
					{
						var targetDay = await context.Days
							.FirstOrDefaultAsync(d => d.CalendarId == calendarId && d.Date == date.Value);

						if (targetDay == null)
						{
							targetDay = new Day
							{
								Date = date.Value,
								CalendarId = calendarId
							};
							context.Days.Add(targetDay);
							await context.SaveChangesAsync();
						}

						targetParentId = targetDay.Id;
					}

					var existingTicket = await context.Tickets
						.FirstOrDefaultAsync(t => t.Name == ticket.Name &&
												  t.CalendarListId == targetCalendarList.Id &&
												  t.ParentId == targetParentId &&
												  t.Priority == ticket.Priority &&
												  t.Description == ticket.Description &&
												  t.IsCompleted == ticket.IsCompleted);

					if (existingTicket == null)
					{
						var maxPosition = await context.Tickets
							.Where(t => t.ParentId == targetParentId)
							.MaxAsync(t => (int?)t.CurrentPosition) ?? 0;

						var newTicket = new Ticket
						{
							Name = ticket.Name,
							Description = ticket.Description,
							CurrentPosition = maxPosition + 1,
							StartTime = ticket.StartTime,
							EndTime = ticket.EndTime,
							Priority = ticket.Priority,
							CalendarListId = targetCalendarList.Id,
							CurrentParentType = ticket.CurrentParentType,
							ParentId = targetParentId,
							IsCompleted = ticket.IsCompleted
						};
						context.Tickets.Add(newTicket);
						await context.SaveChangesAsync();
					}

					await transaction.CommitAsync();
					return true;
				}
				catch
				{
					await transaction.RollbackAsync();
					throw;
				}
			}
		}
	}
}
