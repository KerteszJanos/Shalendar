using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Shalendar.Contexts;
using Shalendar.Functions;
using Shalendar.Models;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using FluentAssertions;

namespace Shalendar.Tests.Functions
{
	public class CopyTicketHelperTests
	{
		private ShalendarDbContext CreateDbContext(string dbName)
		{
			var options = new DbContextOptionsBuilder<ShalendarDbContext>()
				.UseInMemoryDatabase(dbName)
				.ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
				.Options;

			return new ShalendarDbContext(options);
		}

		[Fact]
		public async Task CopyTicketAsync_ReturnsFalse_WhenTicketNotFound()
		{
			using var context = CreateDbContext(nameof(CopyTicketAsync_ReturnsFalse_WhenTicketNotFound));
			var helper = new CopyTicketHelper();

			var result = await helper.CopyTicketAsync(context, ticketId: 1, calendarId: 2, date: DateTime.Now);

			result.Should().BeFalse();
		}

		[Fact]
		public async Task CopyTicketAsync_ReturnsFalse_WhenOriginalCalendarListNotFound()
		{
			using var context = CreateDbContext(nameof(CopyTicketAsync_ReturnsFalse_WhenOriginalCalendarListNotFound));
			var helper = new CopyTicketHelper();

			context.Tickets.Add(new Ticket { Name = "Test Ticket", Id = 1, CalendarListId = 99, CurrentParentType = "CalendarList" });
			await context.SaveChangesAsync();

			var result = await helper.CopyTicketAsync(context, ticketId: 1, calendarId: 2, date: DateTime.Now);

			result.Should().BeFalse();
		}

		[Fact]
		public async Task CopyTicketAsync_CreatesTargetCalendarListAndTicket()
		{
			using var context = CreateDbContext(nameof(CopyTicketAsync_CreatesTargetCalendarListAndTicket));
			var helper = new CopyTicketHelper();

			var calendarList = new CalendarList { Id = 1, Name = "Work", Color = "Red", CalendarId = 1 };
			var ticket = new Ticket
			{
				Id = 1,
				Name = "Test Ticket",
				Description = "Test",
				CalendarListId = 1,
				CurrentPosition = 1,
				StartTime = TimeSpan.FromHours(9),
				EndTime = TimeSpan.FromHours(10),
				Priority = 1,
				CurrentParentType = "ScheduledPanel",
				ParentId = 0,
				IsCompleted = false
			};

			context.CalendarLists.Add(calendarList);
			context.Tickets.Add(ticket);
			await context.SaveChangesAsync();

			var result = await helper.CopyTicketAsync(context, ticketId: 1, calendarId: 2, date: null);

			result.Should().BeTrue();
			context.CalendarLists.Should().HaveCount(2);
			context.Tickets.Should().HaveCount(2);
		}

		[Fact]
		public async Task CopyTicketAsync_DoesNotDuplicateTicket_IfAlreadyExists()
		{
			using var context = CreateDbContext(nameof(CopyTicketAsync_DoesNotDuplicateTicket_IfAlreadyExists));
			var helper = new CopyTicketHelper();

			var originalList = new CalendarList { Id = 1, Name = "Work", Color = "Red", CalendarId = 1 };
			var targetList = new CalendarList { Id = 2, Name = "Work", Color = "Red", CalendarId = 2 };
			context.CalendarLists.AddRange(originalList, targetList);

			var ticket = new Ticket
			{
				Id = 1,
				Name = "Test",
				Description = "Test Desc",
				CalendarListId = 1,
				CurrentPosition = 1,
				StartTime = TimeSpan.FromHours(9),
				EndTime = TimeSpan.FromHours(10),
				Priority = 1,
				CurrentParentType = "TodoList",
				ParentId = 5,
				IsCompleted = false
			};
			context.Tickets.Add(ticket);

			var existingCopy = new Ticket
			{
				Id = 2,
				Name = "Test",
				Description = "Test Desc",
				CalendarListId = 2,
				ParentId = 2,
				CurrentPosition = 1,
				StartTime = ticket.StartTime,
				EndTime = ticket.EndTime,
				Priority = ticket.Priority,
				CurrentParentType = ticket.CurrentParentType,
				IsCompleted = ticket.IsCompleted
			};
			context.Tickets.Add(existingCopy);

			await context.SaveChangesAsync();

			var result = await helper.CopyTicketAsync(context, ticketId: 1, calendarId: 2, date: null);

			result.Should().BeTrue();
			context.Tickets.Should().HaveCount(2);
		}
	}
}
