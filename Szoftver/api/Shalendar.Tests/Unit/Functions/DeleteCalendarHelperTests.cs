using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Moq;
using Shalendar.Contexts;
using Shalendar.Functions;
using Shalendar.Models;
using Shalendar.Services.Interfaces;
using Xunit;
using FluentAssertions;

namespace Shalendar.Tests.Unit.Functions
{
    public class DeleteCalendarHelperTests
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
        public async Task ShouldDeleteCalendar_ReturnsFalse_WhenNoPermission()
        {
            using var context = CreateDbContext(nameof(ShouldDeleteCalendar_ReturnsFalse_WhenNoPermission));
            var mockGroupManager = new Mock<IGroupManagerService>();
            var mockHubContext = new Mock<IHubContext<CalendarHub>>();
            var helper = new DeleteCalendarHelper(context, mockGroupManager.Object, mockHubContext.Object);

            var result = await helper.ShouldDeleteCalendar(userId: 1, calendarId: 1);

            result.Should().BeFalse();
        }

        [Fact]
        public async Task ShouldDeleteCalendar_ReturnsFalse_WhenNotOwner()
        {
            using var context = CreateDbContext(nameof(ShouldDeleteCalendar_ReturnsFalse_WhenNotOwner));
            context.CalendarPermissions.Add(new CalendarPermission { UserId = 1, CalendarId = 1, PermissionType = "read" });
            await context.SaveChangesAsync();

            var mockGroupManager = new Mock<IGroupManagerService>();
            var mockHubContext = new Mock<IHubContext<CalendarHub>>();
            var helper = new DeleteCalendarHelper(context, mockGroupManager.Object, mockHubContext.Object);

            var result = await helper.ShouldDeleteCalendar(1, 1);

            result.Should().BeFalse();
            context.CalendarPermissions.Should().BeEmpty();
        }

        [Fact]
        public async Task ShouldDeleteCalendar_ReturnsFalse_WhenMultipleOwners()
        {
            using var context = CreateDbContext(nameof(ShouldDeleteCalendar_ReturnsFalse_WhenMultipleOwners));
            context.CalendarPermissions.AddRange(
                new CalendarPermission { UserId = 1, CalendarId = 1, PermissionType = "owner" },
                new CalendarPermission { UserId = 2, CalendarId = 1, PermissionType = "owner" });
            await context.SaveChangesAsync();

            var mockGroupManager = new Mock<IGroupManagerService>();
            var mockHubContext = new Mock<IHubContext<CalendarHub>>();
            var helper = new DeleteCalendarHelper(context, mockGroupManager.Object, mockHubContext.Object);

            var result = await helper.ShouldDeleteCalendar(1, 1);

            result.Should().BeFalse();
            context.CalendarPermissions.Should().ContainSingle();
            context.CalendarPermissions.First().UserId.Should().Be(2);
        }

        [Fact]
        public async Task ShouldDeleteCalendar_ReturnsTrue_WhenSoleOwner()
        {
            using var context = CreateDbContext(nameof(ShouldDeleteCalendar_ReturnsTrue_WhenSoleOwner));
            context.CalendarPermissions.Add(new CalendarPermission { UserId = 1, CalendarId = 1, PermissionType = "owner" });
            await context.SaveChangesAsync();

            var mockGroupManager = new Mock<IGroupManagerService>();
            var mockHubContext = new Mock<IHubContext<CalendarHub>>();
            var helper = new DeleteCalendarHelper(context, mockGroupManager.Object, mockHubContext.Object);

            var result = await helper.ShouldDeleteCalendar(1, 1);

            result.Should().BeTrue();
            context.CalendarPermissions.Should().ContainSingle();
        }

        [Fact]
        public async Task DeleteCalendar_ShouldClearDefaultCalendarId_ForAffectedUsers()
        {
            using var context = CreateDbContext(nameof(DeleteCalendar_ShouldClearDefaultCalendarId_ForAffectedUsers));
            var calendarId = 1;

            context.Users.AddRange(
                new User { Id = 1, DefaultCalendarId = calendarId },
                new User { Id = 2, DefaultCalendarId = calendarId },
                new User { Id = 3, DefaultCalendarId = 999 }
            );
            context.Calendars.Add(new Calendar { Id = calendarId });
            await context.SaveChangesAsync();

            var mockClientProxy = new Mock<IClientProxy>();
            mockClientProxy
                .Setup(x => x.SendCoreAsync("CalendarDeleted", It.IsAny<object[]>(), default))
                .Returns(Task.CompletedTask);


            var mockClients = new Mock<IHubClients>();
            mockClients.Setup(x => x.Group(calendarId.ToString()))
                .Returns(mockClientProxy.Object);

            var mockHubContext = new Mock<IHubContext<CalendarHub>>();
            mockHubContext.Setup(x => x.Clients).Returns(mockClients.Object);

            var mockGroupManager = new Mock<IGroupManagerService>();
            var helper = new DeleteCalendarHelper(context, mockGroupManager.Object, mockHubContext.Object);

            await helper.DeleteCalendar(calendarId);

            context.Users.Where(u => u.Id == 1 || u.Id == 2).Should().OnlyContain(u => u.DefaultCalendarId == null);
        }

        [Fact]
        public async Task DeleteCalendar_ShouldDeletePermissionsDaysListsTicketsAndCalendar()
        {
            using var context = CreateDbContext(nameof(DeleteCalendar_ShouldDeletePermissionsDaysListsTicketsAndCalendar));
            var calendarId = 5;

            var day = new Day { Id = 1, CalendarId = calendarId, Date = DateTime.Today };
            var list = new CalendarList { Id = 2, CalendarId = calendarId, Name = "Work", Color = "Blue" };
            var ticket1 = new Ticket { Id = 1, ParentId = day.Id, CalendarListId = 999, Name = "ByDay", CurrentParentType = "TodoList" };
            var ticket2 = new Ticket { Id = 2, ParentId = 999, CalendarListId = list.Id, Name = "ByList", CurrentParentType = "CalendarList" };

            context.Days.Add(day);
            context.CalendarLists.Add(list);
            context.Tickets.AddRange(ticket1, ticket2);
            context.CalendarPermissions.Add(new CalendarPermission { CalendarId = calendarId, UserId = 1, PermissionType = "owner" });
            context.Calendars.Add(new Calendar { Id = calendarId });
            await context.SaveChangesAsync();

            var mockClientProxy = new Mock<IClientProxy>();
            mockClientProxy
                .Setup(x => x.SendCoreAsync("CalendarDeleted", It.IsAny<object[]>(), default))
                .Returns(Task.CompletedTask);

            var mockClients = new Mock<IHubClients>();
            mockClients.Setup(x => x.Group(calendarId.ToString()))
                .Returns(mockClientProxy.Object);

            var mockHubContext = new Mock<IHubContext<CalendarHub>>();
            mockHubContext.Setup(x => x.Clients).Returns(mockClients.Object);

            var mockGroupManager = new Mock<IGroupManagerService>();
            var helper = new DeleteCalendarHelper(context, mockGroupManager.Object, mockHubContext.Object);

            await helper.DeleteCalendar(calendarId);

            context.Days.Should().BeEmpty();
            context.CalendarLists.Should().BeEmpty();
            context.Tickets.Should().BeEmpty();
            context.CalendarPermissions.Should().BeEmpty();
            context.Calendars.Should().BeEmpty();
        }

        [Fact]
        public async Task DeleteCalendar_ShouldNotFail_IfCalendarNotFound()
        {
            using var context = CreateDbContext(nameof(DeleteCalendar_ShouldNotFail_IfCalendarNotFound));
            var calendarId = 777;

            context.Users.Add(new User { Id = 1, DefaultCalendarId = calendarId });
            await context.SaveChangesAsync();

            var mockClientProxy = new Mock<IClientProxy>();
            mockClientProxy
                .Setup(x => x.SendCoreAsync("CalendarDeleted", It.IsAny<object[]>(), default))
                .Returns(Task.CompletedTask);

            var mockClients = new Mock<IHubClients>();
            mockClients.Setup(x => x.Group(calendarId.ToString()))
                .Returns(mockClientProxy.Object);

            var mockHubContext = new Mock<IHubContext<CalendarHub>>();
            mockHubContext.Setup(x => x.Clients).Returns(mockClients.Object);

            var mockGroupManager = new Mock<IGroupManagerService>();
            var helper = new DeleteCalendarHelper(context, mockGroupManager.Object, mockHubContext.Object);

            var act = async () => await helper.DeleteCalendar(calendarId);

            await act.Should().NotThrowAsync();
            context.Users.First().DefaultCalendarId.Should().BeNull();
        }
    }
}
