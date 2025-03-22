using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Moq;
using Shalendar.Contexts;
using Shalendar.Controllers;
using Shalendar.Functions.Interfaces;
using Shalendar.Models;
using Shalendar.Models.Dtos;
using Shalendar.Services.Interfaces;
using Xunit;

namespace Shalendar.Tests.Controllers
{
	public class TicketsControllerTests
	{
		private (TicketsController controller,
				 Mock<IJwtHelper> jwtHelper,
				 Mock<ICopyTicketHelper> copyTicketHelper,
				 Mock<IGetCalendarIdHelper> getCalendarIdHelper,
				 Mock<IGroupManagerService> groupManager,
				 Mock<IHubContext<CalendarHub>> hubContext)
			CreateTicketsController(ShalendarDbContext context)
		{
			var mockJwtHelper = new Mock<IJwtHelper>();
			var mockCopyTicketHelper = new Mock<ICopyTicketHelper>();
			var mockGetCalendarIdHelper = new Mock<IGetCalendarIdHelper>();
			var mockGroupManager = new Mock<IGroupManagerService>();
			var mockHubContext = new Mock<IHubContext<CalendarHub>>();

			var controller = new TicketsController(
				context,
				mockJwtHelper.Object,
				mockCopyTicketHelper.Object,
				mockHubContext.Object,
				mockGroupManager.Object,
				mockGetCalendarIdHelper.Object
			);

			controller.ControllerContext = new ControllerContext
			{
				HttpContext = new DefaultHttpContext()
			};

			return (controller, mockJwtHelper, mockCopyTicketHelper, mockGetCalendarIdHelper, mockGroupManager, mockHubContext);
		}

		#region Gets

		[Fact]
		public async Task GetTodoListTicketsByDateAndCalendar_ShouldReturnForbidden_WhenPermissionDenied()
		{
			var options = new DbContextOptionsBuilder<ShalendarDbContext>()
				.UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
			using var context = new ShalendarDbContext(options);

			var (controller, mockJwtHelper, _, _, _, _) = CreateTicketsController(context);
			mockJwtHelper.Setup(j => j.HasCalendarPermission(It.IsAny<HttpContext>(), "read")).ReturnsAsync(false);

			var result = await controller.GetTodoListTicketsByDateAndCalendar("2024-03-21", 1);

			var objectResult = Assert.IsType<ObjectResult>(result.Result);
			objectResult.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
		}

		[Fact]
		public async Task GetTodoListTicketsByDateAndCalendar_ShouldReturnBadRequest_WhenInvalidDate()
		{
			var options = new DbContextOptionsBuilder<ShalendarDbContext>()
				.UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
			using var context = new ShalendarDbContext(options);

			var (controller, mockJwtHelper, _, _, _, _) = CreateTicketsController(context);
			mockJwtHelper.Setup(j => j.HasCalendarPermission(It.IsAny<HttpContext>(), "read")).ReturnsAsync(true);

			var result = await controller.GetTodoListTicketsByDateAndCalendar("invalid-date", 1);

			Assert.IsType<BadRequestObjectResult>(result.Result);
		}

		[Fact]
		public async Task GetScheduledListTicketsByDateAndCalendar_ShouldReturnForbidden_WhenPermissionDenied()
		{
			var options = new DbContextOptionsBuilder<ShalendarDbContext>()
				.UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
			using var context = new ShalendarDbContext(options);

			var (controller, mockJwtHelper, _, _, _, _) = CreateTicketsController(context);
			mockJwtHelper.Setup(j => j.HasCalendarPermission(It.IsAny<HttpContext>(), "read")).ReturnsAsync(false);

			var result = await controller.GetScheduledListTicketsByDateAndCalendar("2024-03-21", 1);

			var objectResult = Assert.IsType<ObjectResult>(result.Result);
			objectResult.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
		}

		[Fact]
		public async Task GetScheduledListTicketsByDateAndCalendar_ShouldReturnBadRequest_WhenInvalidDate()
		{
			var options = new DbContextOptionsBuilder<ShalendarDbContext>()
				.UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
			using var context = new ShalendarDbContext(options);

			var (controller, mockJwtHelper, _, _, _, _) = CreateTicketsController(context);
			mockJwtHelper.Setup(j => j.HasCalendarPermission(It.IsAny<HttpContext>(), "read")).ReturnsAsync(true);

			var result = await controller.GetScheduledListTicketsByDateAndCalendar("invalid-date", 1);

			Assert.IsType<BadRequestObjectResult>(result.Result);
		}

		[Fact]
		public async Task GetAllDailyTicketsByDateAndCalendar_ShouldReturnForbidden_WhenPermissionDenied()
		{
			var options = new DbContextOptionsBuilder<ShalendarDbContext>()
				.UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
			using var context = new ShalendarDbContext(options);

			var (controller, mockJwtHelper, _, _, _, _) = CreateTicketsController(context);
			mockJwtHelper.Setup(j => j.HasCalendarPermission(It.IsAny<HttpContext>(), "read")).ReturnsAsync(false);

			var result = await controller.GetAllDailyTicketsByDateAndCalendar("2024-03-21", 1);

			var objectResult = Assert.IsType<ObjectResult>(result.Result);
			objectResult.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
		}

		[Fact]
		public async Task GetAllDailyTicketsByDateAndCalendar_ShouldReturnBadRequest_WhenInvalidDate()
		{
			var options = new DbContextOptionsBuilder<ShalendarDbContext>()
				.UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
			using var context = new ShalendarDbContext(options);

			var (controller, mockJwtHelper, _, _, _, _) = CreateTicketsController(context);
			mockJwtHelper.Setup(j => j.HasCalendarPermission(It.IsAny<HttpContext>(), "read")).ReturnsAsync(true);

			var result = await controller.GetAllDailyTicketsByDateAndCalendar("invalid-date", 1);

			Assert.IsType<BadRequestObjectResult>(result.Result);
		}

		[Fact]
		public async Task GetAllDailyTicketsByDateAndCalendar_ShouldReturnEmptyList_WhenDayDoesNotExist()
		{
			var options = new DbContextOptionsBuilder<ShalendarDbContext>()
				.UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
			using var context = new ShalendarDbContext(options);

			var (controller, mockJwtHelper, _, _, _, _) = CreateTicketsController(context);
			mockJwtHelper.Setup(j => j.HasCalendarPermission(It.IsAny<HttpContext>(), "read")).ReturnsAsync(true);

			var result = await controller.GetAllDailyTicketsByDateAndCalendar("2024-03-21", 1);

			var okResult = Assert.IsType<OkObjectResult>(result.Result);
			var tickets = Assert.IsAssignableFrom<IEnumerable<object>>(okResult.Value);
			tickets.Should().BeEmpty();
		}

		[Fact]
		public async Task GetAllDailyTicketsByDayId_ShouldReturnForbidden_WhenNoPermission()
		{
			var options = new DbContextOptionsBuilder<ShalendarDbContext>()
				.UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
			using var context = new ShalendarDbContext(options);

			var (controller, mockJwtHelper, _, _, _, _) = CreateTicketsController(context);

			mockJwtHelper.Setup(j => j.HasCalendarPermission(It.IsAny<HttpContext>(), "read"))
						 .ReturnsAsync(false);

			var result = await controller.GetAllDailyTicketsByDayId(1);

			var objectResult = Assert.IsType<ObjectResult>(result.Result);
			objectResult.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
			objectResult.Value.Should().BeEquivalentTo(new { message = "Required permission: read" });
		}

		[Fact]
		public async Task GetAllDailyTicketsByDayId_ShouldReturnEmptyList_WhenNoTicketsFound()
		{
			var options = new DbContextOptionsBuilder<ShalendarDbContext>()
				.UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
			using var context = new ShalendarDbContext(options);

			var (controller, mockJwtHelper, _, _, _, _) = CreateTicketsController(context);

			mockJwtHelper.Setup(j => j.HasCalendarPermission(It.IsAny<HttpContext>(), "read"))
						 .ReturnsAsync(true);

			var result = await controller.GetAllDailyTicketsByDayId(999);

			var okResult = Assert.IsType<OkObjectResult>(result.Result);
			var tickets = Assert.IsAssignableFrom<IEnumerable<object>>(okResult.Value);
			tickets.Should().BeEmpty();
		}

		[Fact]
		public async Task GetAllDailyTicketsByDayId_ShouldReturnTickets_WhenTheyExist()
		{
			var options = new DbContextOptionsBuilder<ShalendarDbContext>()
				.UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
			using var context = new ShalendarDbContext(options);

			context.CalendarLists.Add(new CalendarList
			{
				Id = 1,
				Name = "Default Calendar List",
				Color = "#FFFFFF"
			});

			context.Tickets.AddRange(
				new Ticket
				{
					Id = 1,
					ParentId = 10,
					Name = "Ticket 1",
					CurrentParentType = "ScheduledList",
					CalendarListId = 1,
					StartTime = new TimeSpan(14, 0, 0),
					CurrentPosition = 1,
					IsCompleted = false
				},
				new Ticket
				{
					Id = 2,
					ParentId = 10,
					Name = "Ticket 2",
					CurrentParentType = "TodoList",
					CalendarListId = 1,
					StartTime = null,
					CurrentPosition = 2,
					IsCompleted = true
				}
			);

			await context.SaveChangesAsync();

			var (controller, mockJwtHelper, _, _, _, _) = CreateTicketsController(context);

			mockJwtHelper.Setup(j => j.HasCalendarPermission(It.IsAny<HttpContext>(), "read"))
						 .ReturnsAsync(true);

			var result = await controller.GetAllDailyTicketsByDayId(10);

			var okResult = Assert.IsType<OkObjectResult>(result.Result);
			var tickets = Assert.IsAssignableFrom<IEnumerable<object>>(okResult.Value);
			tickets.Should().HaveCount(2);
		}


		#endregion



		#region Posts

		[Fact]
		public async Task CreateTicket_ShouldReturnBadRequest_WhenHeaderMissing()
		{
			var options = new DbContextOptionsBuilder<ShalendarDbContext>()
				.UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;

			using var context = new ShalendarDbContext(options);
			var (controller, _, _, mockGetCalendarIdHelper, _, _) = CreateTicketsController(context);

			mockGetCalendarIdHelper.Setup(h => h.TryGetCalendarId(It.IsAny<HttpContext>(), out It.Ref<int>.IsAny))
				.Returns(false);

			var ticket = new Ticket { Name = "New Ticket" };

			var result = await controller.CreateTicket(ticket);

			var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
			badRequest.Value.Should().Be("Invalid or missing X-Calendar-Id header.");
		}

		[Fact]
		public async Task CreateTicket_ShouldReturnForbidden_WhenNoWritePermission()
		{
			var options = new DbContextOptionsBuilder<ShalendarDbContext>()
				.UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;

			using var context = new ShalendarDbContext(options);
			var (controller, mockJwtHelper, _, mockGetCalendarIdHelper, _, _) = CreateTicketsController(context);

			int calendarId = 1;
			mockGetCalendarIdHelper.Setup(h => h.TryGetCalendarId(It.IsAny<HttpContext>(), out calendarId))
				.Returns(true);

			mockJwtHelper.Setup(j => j.HasCalendarPermission(It.IsAny<HttpContext>(), "write", calendarId))
				.ReturnsAsync(false);

			var ticket = new Ticket { Name = "New Ticket" };

			var result = await controller.CreateTicket(ticket);

			var forbidden = Assert.IsType<ObjectResult>(result.Result);
			forbidden.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
			forbidden.Value.Should().BeEquivalentTo(new { message = "Required permission: write" });
		}

		[Fact]
		public async Task CreateTicket_ShouldReturnBadRequest_WhenTicketNameMissing()
		{
			var options = new DbContextOptionsBuilder<ShalendarDbContext>()
				.UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;

			using var context = new ShalendarDbContext(options);
			var (controller, mockJwtHelper, _, mockGetCalendarIdHelper, _, _) = CreateTicketsController(context);

			int calendarId = 1;
			mockGetCalendarIdHelper.Setup(h => h.TryGetCalendarId(It.IsAny<HttpContext>(), out calendarId))
				.Returns(true);

			mockJwtHelper.Setup(j => j.HasCalendarPermission(It.IsAny<HttpContext>(), "write", calendarId))
				.ReturnsAsync(true);

			var ticket = new Ticket { Name = "" };

			var result = await controller.CreateTicket(ticket);

			var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
			badRequest.Value.Should().Be("Ticket name is required.");
		}

		[Fact]
		public async Task CreateTicket_ShouldCreateTicket_AndSendSignalR_WhenParentTypeCalendarList()
		{
			var options = new DbContextOptionsBuilder<ShalendarDbContext>()
				.UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;

			using var context = new ShalendarDbContext(options);
			var (controller, mockJwtHelper, _, mockGetCalendarIdHelper, mockGroupManager, mockHubContext) = CreateTicketsController(context);

			int calendarId = 1;
			mockGetCalendarIdHelper.Setup(h => h.TryGetCalendarId(It.IsAny<HttpContext>(), out calendarId))
				.Returns(true);

			mockJwtHelper.Setup(j => j.HasCalendarPermission(It.IsAny<HttpContext>(), "write", calendarId))
				.ReturnsAsync(true);

			mockGroupManager.Setup(g => g.IsUserAloneInGroup(calendarId.ToString()))
				.Returns(false);

			var mockClients = new Mock<IHubClients>();
			var mockClientProxy = new Mock<IClientProxy>();

			mockClients.Setup(c => c.Group(calendarId.ToString())).Returns(mockClientProxy.Object);
			mockHubContext.Setup(h => h.Clients).Returns(mockClients.Object);

			var ticket = new Ticket
			{
				Name = "Valid Ticket",
				CurrentParentType = "CalendarList"
			};

			var result = await controller.CreateTicket(ticket);

			var createdAtAction = Assert.IsType<CreatedAtActionResult>(result.Result);
			var createdTicket = Assert.IsType<Ticket>(createdAtAction.Value);

			createdTicket.Name.Should().Be("Valid Ticket");
			context.Tickets.Should().ContainSingle(t => t.Name == "Valid Ticket");

			mockClientProxy.Verify(
				m => m.SendCoreAsync("TicketCreatedInCalendarLists", It.IsAny<object[]>(), default),
				Times.Once);
		}

		[Fact]
		public async Task CreateTicket_ShouldReturnBadRequest_WhenDayNotFound_ForDayView()
		{
			var options = new DbContextOptionsBuilder<ShalendarDbContext>()
				.UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;

			using var context = new ShalendarDbContext(options);
			var (controller, mockJwtHelper, _, mockGetCalendarIdHelper, mockGroupManager, _) = CreateTicketsController(context);

			int calendarId = 1;
			mockGetCalendarIdHelper.Setup(h => h.TryGetCalendarId(It.IsAny<HttpContext>(), out calendarId))
				.Returns(true);

			mockJwtHelper.Setup(j => j.HasCalendarPermission(It.IsAny<HttpContext>(), "write", calendarId))
				.ReturnsAsync(true);

			mockGroupManager.Setup(g => g.IsUserAloneInGroup(calendarId.ToString()))
				.Returns(false);

			var ticket = new Ticket
			{
				Name = "Day View Ticket",
				CurrentParentType = "ScheduledList",
				ParentId = 99
			};

			var result = await controller.CreateTicket(ticket);

			var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
			badRequest.Value.Should().Be("Associated Day not found.");
		}

		[Fact]
		public async Task CreateTicket_ShouldCreateTicket_AndSendSignalR_WhenParentTypeDayViewAndDayExists()
		{
			var options = new DbContextOptionsBuilder<ShalendarDbContext>()
				.UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;

			using var context = new ShalendarDbContext(options);
			context.Days.Add(new Day { Id = 100, Date = new DateTime(2024, 04, 01), CalendarId = 1 });
			await context.SaveChangesAsync();

			var (controller, mockJwtHelper, _, mockGetCalendarIdHelper, mockGroupManager, mockHubContext) = CreateTicketsController(context);

			int calendarId = 1;
			mockGetCalendarIdHelper.Setup(h => h.TryGetCalendarId(It.IsAny<HttpContext>(), out calendarId))
				.Returns(true);

			mockJwtHelper.Setup(j => j.HasCalendarPermission(It.IsAny<HttpContext>(), "write", calendarId))
				.ReturnsAsync(true);

			mockGroupManager.Setup(g => g.IsUserAloneInGroup(calendarId.ToString()))
				.Returns(false);

			var mockClients = new Mock<IHubClients>();
			var mockClientProxy = new Mock<IClientProxy>();

			mockClients.Setup(c => c.Group(calendarId.ToString())).Returns(mockClientProxy.Object);
			mockHubContext.Setup(h => h.Clients).Returns(mockClients.Object);

			var ticket = new Ticket
			{
				Name = "Day View Ticket",
				CurrentParentType = "ScheduledList",
				ParentId = 100
			};

			var result = await controller.CreateTicket(ticket);

			var createdAtAction = Assert.IsType<CreatedAtActionResult>(result.Result);
			var createdTicket = Assert.IsType<Ticket>(createdAtAction.Value);

			createdTicket.Name.Should().Be("Day View Ticket");
			context.Tickets.Should().ContainSingle(t => t.Name == "Day View Ticket");

			mockClientProxy.Verify(
				m => m.SendCoreAsync("TicketCreatedInDayView",
					It.Is<object[]>(o => ((DateTime)o[0]) == new DateTime(2024, 04, 01)), default),
				Times.Once);
		}

		[Fact]
		public async Task ScheduleTicket_ShouldReturnBadRequest_WhenHeaderMissing()
		{
			var options = new DbContextOptionsBuilder<ShalendarDbContext>()
				.UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;

			using var context = new ShalendarDbContext(options);
			var (controller, _, _, mockGetCalendarIdHelper, _, _) = CreateTicketsController(context);

			mockGetCalendarIdHelper.Setup(h => h.TryGetCalendarId(It.IsAny<HttpContext>(), out It.Ref<int>.IsAny))
				.Returns(false);

			var dto = new ScheduleTicketDto { TicketId = 1, CalendarId = 1, Date = DateTime.Today };

			var result = await controller.ScheduleTicket(dto);

			var badRequest = Assert.IsType<BadRequestObjectResult>(result);
			badRequest.Value.Should().Be("Invalid or missing X-Calendar-Id header.");
		}

		[Fact]
		public async Task ScheduleTicket_ShouldReturnForbidden_WhenNoWritePermission()
		{
			var options = new DbContextOptionsBuilder<ShalendarDbContext>()
				.UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;

			using var context = new ShalendarDbContext(options);
			var (controller, mockJwtHelper, _, mockGetCalendarIdHelper, _, _) = CreateTicketsController(context);

			int calendarId = 1;
			mockGetCalendarIdHelper.Setup(h => h.TryGetCalendarId(It.IsAny<HttpContext>(), out calendarId))
				.Returns(true);

			mockJwtHelper.Setup(j => j.HasCalendarPermission(It.IsAny<HttpContext>(), "write", calendarId))
				.ReturnsAsync(false);

			var dto = new ScheduleTicketDto { TicketId = 1, CalendarId = calendarId, Date = DateTime.Today };

			var result = await controller.ScheduleTicket(dto);

			var forbidden = Assert.IsType<ObjectResult>(result);
			forbidden.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
			forbidden.Value.Should().BeEquivalentTo(new { message = "Required permission: write" });
		}

		[Fact]
		public async Task ScheduleTicket_ShouldReturnBadRequest_WhenDtoIsInvalid()
		{
			var options = new DbContextOptionsBuilder<ShalendarDbContext>()
				.UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;

			using var context = new ShalendarDbContext(options);
			var (controller, mockJwtHelper, _, mockGetCalendarIdHelper, _, _) = CreateTicketsController(context);

			int calendarId = 1;
			mockGetCalendarIdHelper.Setup(h => h.TryGetCalendarId(It.IsAny<HttpContext>(), out calendarId))
				.Returns(true);

			mockJwtHelper.Setup(j => j.HasCalendarPermission(It.IsAny<HttpContext>(), "write", calendarId))
				.ReturnsAsync(true);

			var dto = new ScheduleTicketDto { TicketId = 0, CalendarId = calendarId, Date = DateTime.Today };

			var result = await controller.ScheduleTicket(dto);

			var badRequest = Assert.IsType<BadRequestObjectResult>(result);
			badRequest.Value.Should().Be("Invalid data.");
		}

		[Fact]
		public async Task ScheduleTicket_ShouldReturnNotFound_WhenTicketDoesNotExist()
		{
			var options = new DbContextOptionsBuilder<ShalendarDbContext>()
				.UseInMemoryDatabase(Guid.NewGuid().ToString())
				.ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning))
			.Options;


			using var context = new ShalendarDbContext(options);
			var (controller, mockJwtHelper, _, mockGetCalendarIdHelper, _, _) = CreateTicketsController(context);

			int calendarId = 1;
			mockGetCalendarIdHelper.Setup(h => h.TryGetCalendarId(It.IsAny<HttpContext>(), out calendarId))
				.Returns(true);

			mockJwtHelper.Setup(j => j.HasCalendarPermission(It.IsAny<HttpContext>(), "write", calendarId))
				.ReturnsAsync(true);

			var dto = new ScheduleTicketDto { TicketId = 999, CalendarId = calendarId, Date = DateTime.Today };

			var result = await controller.ScheduleTicket(dto);

			var notFound = Assert.IsType<NotFoundObjectResult>(result);
			notFound.Value.Should().Be("Ticket not found.");
		}

		[Fact]
		public async Task ScheduleTicket_ShouldScheduleTicketAndCreateDay_WhenDayDoesNotExist()
		{
			var options = new DbContextOptionsBuilder<ShalendarDbContext>()
				.UseInMemoryDatabase(Guid.NewGuid().ToString())
				.ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning))
			.Options;

			using var context = new ShalendarDbContext(options);

			var ticket = new Ticket { Id = 1, Name = "Test Ticket", CurrentParentType = "TodoList" };
			context.Tickets.Add(ticket);
			await context.SaveChangesAsync();

			var (controller, mockJwtHelper, _, mockGetCalendarIdHelper, mockGroupManager, mockHubContext) = CreateTicketsController(context);

			int calendarId = 1;
			mockGetCalendarIdHelper.Setup(h => h.TryGetCalendarId(It.IsAny<HttpContext>(), out calendarId))
				.Returns(true);

			mockJwtHelper.Setup(j => j.HasCalendarPermission(It.IsAny<HttpContext>(), "write", calendarId))
				.ReturnsAsync(true);

			mockGroupManager.Setup(g => g.IsUserAloneInGroup(calendarId.ToString()))
				.Returns(false);

			var mockClients = new Mock<IHubClients>();
			var mockClientProxy = new Mock<IClientProxy>();
			mockClients.Setup(c => c.Group(calendarId.ToString())).Returns(mockClientProxy.Object);
			mockHubContext.Setup(h => h.Clients).Returns(mockClients.Object);

			var dto = new ScheduleTicketDto
			{
				TicketId = 1,
				CalendarId = calendarId,
				Date = new DateTime(2024, 5, 1),
				StartTime = new TimeSpan(10, 0, 0),
				EndTime = new TimeSpan(11, 0, 0)
			};

			var result = await controller.ScheduleTicket(dto);

			var okResult = Assert.IsType<OkObjectResult>(result);
			var updatedTicket = Assert.IsType<Ticket>(okResult.Value);

			updatedTicket.CurrentParentType.Should().Be("ScheduledList");
			updatedTicket.StartTime.Should().Be(dto.StartTime);
			updatedTicket.EndTime.Should().Be(dto.EndTime);

			context.Days.Should().ContainSingle(d => d.Date == dto.Date.Date);

			mockClientProxy.Verify(
				m => m.SendCoreAsync("TicketScheduled", It.Is<object[]>(o => ((DateTime)o[0]) == dto.Date.Date), default),
				Times.Once);
		}

		[Fact]
		public async Task ScheduleTicket_ShouldSetTicketAsTodo_WhenNoStartAndEndTimeProvided()
		{
			var options = new DbContextOptionsBuilder<ShalendarDbContext>()
				.UseInMemoryDatabase(Guid.NewGuid().ToString())
				.ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning))
			.Options;

			using var context = new ShalendarDbContext(options);

			var existingDay = new Day { Id = 10, CalendarId = 1, Date = new DateTime(2024, 5, 1) };
			context.Days.Add(existingDay);
			var ticket = new Ticket { Id = 2, Name = "Test Todo Ticket", CurrentParentType = "TodoList" };
			context.Tickets.Add(ticket);
			await context.SaveChangesAsync();

			var (controller, mockJwtHelper, _, mockGetCalendarIdHelper, mockGroupManager, _) = CreateTicketsController(context);

			int calendarId = 1;
			mockGetCalendarIdHelper.Setup(h => h.TryGetCalendarId(It.IsAny<HttpContext>(), out calendarId))
				.Returns(true);

			mockJwtHelper.Setup(j => j.HasCalendarPermission(It.IsAny<HttpContext>(), "write", calendarId))
				.ReturnsAsync(true);

			mockGroupManager.Setup(g => g.IsUserAloneInGroup(calendarId.ToString()))
				.Returns(true);

			var dto = new ScheduleTicketDto
			{
				TicketId = 2,
				CalendarId = calendarId,
				Date = existingDay.Date,
				StartTime = null,
				EndTime = null
			};

			var result = await controller.ScheduleTicket(dto);

			var okResult = Assert.IsType<OkObjectResult>(result);
			var updatedTicket = Assert.IsType<Ticket>(okResult.Value);

			updatedTicket.CurrentParentType.Should().Be("TodoList");
			updatedTicket.ParentId.Should().Be(existingDay.Id);
			updatedTicket.StartTime.Should().BeNull();
			updatedTicket.EndTime.Should().BeNull();
		}

		[Fact]
		public async Task CopyTicket_ShouldReturnForbidden_WhenNoReadPermissionAndHeaderMissing()
		{
			var options = new DbContextOptionsBuilder<ShalendarDbContext>()
				.UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
			using var context = new ShalendarDbContext(options);

			context.Calendars.Add(new Calendar { Id = 2, Name = "Test Calendar" });
			await context.SaveChangesAsync();

			var (controller, mockJwtHelper, mockCopyTicketHelper, _, _, _) = CreateTicketsController(context);
			mockJwtHelper.Setup(j => j.HasCalendarPermission(It.IsAny<HttpContext>(), "read"))
				.ReturnsAsync(false);

			var result = await controller.CopyTicket(ticketId: 1, calendarId: 2);

			var objectResult = Assert.IsType<ObjectResult>(result);
			objectResult.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
			objectResult.Value.Should().BeEquivalentTo(new { message = "Required permission: read for calendar: 'Test Calendar'" });
		}

		[Fact]
		public async Task CopyTicket_ShouldReturnForbidden_WhenNoWritePermission()
		{
			var options = new DbContextOptionsBuilder<ShalendarDbContext>()
				.UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
			using var context = new ShalendarDbContext(options);

			context.Calendars.Add(new Calendar { Id = 3, Name = "Target Calendar" });
			await context.SaveChangesAsync();

			var (controller, mockJwtHelper, mockCopyTicketHelper, _, _, _) = CreateTicketsController(context);
			mockJwtHelper.Setup(j => j.HasCalendarPermission(It.IsAny<HttpContext>(), "read"))
				.ReturnsAsync(true);
			mockJwtHelper.Setup(j => j.HasCalendarPermission(It.IsAny<HttpContext>(), "write", 3))
				.ReturnsAsync(false);

			var result = await controller.CopyTicket(ticketId: 1, calendarId: 3);

			var objectResult = Assert.IsType<ObjectResult>(result);
			objectResult.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
			objectResult.Value.Should().BeEquivalentTo(new { message = "Required permission: write for calendar: 'Target Calendar'" });
		}

		[Fact]
		public async Task CopyTicket_ShouldCopyTicket_AndSendSignalR_WhenDateIsNull()
		{
			var options = new DbContextOptionsBuilder<ShalendarDbContext>()
				.UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
			using var context = new ShalendarDbContext(options);

			var (controller, mockJwtHelper, mockCopyTicketHelper, _, mockGroupManager, mockHubContext) = CreateTicketsController(context);

			mockJwtHelper.Setup(j => j.HasCalendarPermission(It.IsAny<HttpContext>(), "read"))
				.ReturnsAsync(true);
			mockJwtHelper.Setup(j => j.HasCalendarPermission(It.IsAny<HttpContext>(), "write", It.IsAny<int>()))
				.ReturnsAsync(true);

			mockCopyTicketHelper.Setup(h => h.CopyTicketAsync(context, It.IsAny<int>(), It.IsAny<int>(), null))
				.ReturnsAsync(true);
			mockGroupManager.Setup(g => g.IsUserAloneInGroup(It.IsAny<string>())).Returns(false);

			var mockClients = new Mock<IHubClients>();
			var mockClientProxy = new Mock<IClientProxy>();
			mockClients.Setup(c => c.Group("1")).Returns(mockClientProxy.Object);
			mockHubContext.Setup(h => h.Clients).Returns(mockClients.Object);

			var result = await controller.CopyTicket(ticketId: 1, calendarId: 1);

			var objectResult = Assert.IsAssignableFrom<ObjectResult>(result);
			objectResult.StatusCode.Should().Be(StatusCodes.Status200OK);
			objectResult.Value.Should().Be("Ticket successfully copied or already existed.");

			mockClientProxy.Verify(
				m => m.SendCoreAsync("TicketCopiedInCalendarLists", It.IsAny<object[]>(), default),
				Times.Once);
		}

		[Fact]
		public async Task CopyTicket_ShouldCopyTicket_AndSendSignalR_WhenDateIsProvided()
		{
			var options = new DbContextOptionsBuilder<ShalendarDbContext>()
				.UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
			using var context = new ShalendarDbContext(options);

			var (controller, mockJwtHelper, mockCopyTicketHelper, _, mockGroupManager, mockHubContext) = CreateTicketsController(context);

			mockJwtHelper.Setup(j => j.HasCalendarPermission(It.IsAny<HttpContext>(), "read"))
				.ReturnsAsync(true);
			mockJwtHelper.Setup(j => j.HasCalendarPermission(It.IsAny<HttpContext>(), "write", It.IsAny<int>()))
				.ReturnsAsync(true);

			mockCopyTicketHelper.Setup(h => h.CopyTicketAsync(context, It.IsAny<int>(), It.IsAny<int>(), It.IsAny<DateTime?>()))
				.ReturnsAsync(true);
			mockGroupManager.Setup(g => g.IsUserAloneInGroup(It.IsAny<string>())).Returns(false);

			var mockClients = new Mock<IHubClients>();
			var mockClientProxy = new Mock<IClientProxy>();
			mockClients.Setup(c => c.Group("1")).Returns(mockClientProxy.Object);
			mockHubContext.Setup(h => h.Clients).Returns(mockClients.Object);

			DateTime date = new DateTime(2024, 05, 01);

			var result = await controller.CopyTicket(ticketId: 1, calendarId: 1, date: date);

			var objectResult = Assert.IsAssignableFrom<ObjectResult>(result);
			objectResult.StatusCode.Should().Be(StatusCodes.Status200OK);
			objectResult.Value.Should().Be("Ticket successfully copied or already existed.");

			mockClientProxy.Verify(
				m => m.SendCoreAsync("TicketCopiedInCalendar", It.Is<object[]>(o => ((DateTime)o[0]) == date), default),
				Times.Once);
		}

		#endregion



		#region Puts

		[Fact]
		public async Task ReorderTickets_ShouldReturnBadRequest_WhenHeaderMissing()
		{
			var options = new DbContextOptionsBuilder<ShalendarDbContext>()
				.UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
			using var context = new ShalendarDbContext(options);

			var (controller, _, _, mockGetCalendarIdHelper, _, _) = CreateTicketsController(context);
			mockGetCalendarIdHelper.Setup(h => h.TryGetCalendarId(It.IsAny<HttpContext>(), out It.Ref<int>.IsAny))
				.Returns(false);

			var result = await controller.ReorderTickets(new List<TicketOrderUpdateDto>());

			var badRequest = Assert.IsType<BadRequestObjectResult>(result);
			badRequest.Value.Should().Be("Invalid or missing X-Calendar-Id header.");
		}

		[Fact]
		public async Task ReorderTickets_ShouldReturnForbidden_WhenNoPermission()
		{
			var options = new DbContextOptionsBuilder<ShalendarDbContext>()
				.UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
			using var context = new ShalendarDbContext(options);

			var (controller, mockJwtHelper, _, mockGetCalendarIdHelper, _, _) = CreateTicketsController(context);
			int calendarId = 1;
			mockGetCalendarIdHelper.Setup(h => h.TryGetCalendarId(It.IsAny<HttpContext>(), out calendarId))
				.Returns(true);

			mockJwtHelper.Setup(j => j.HasCalendarPermission(It.IsAny<HttpContext>(), "write", calendarId))
				.ReturnsAsync(false);

			var result = await controller.ReorderTickets(new List<TicketOrderUpdateDto>());

			var forbidden = Assert.IsType<ObjectResult>(result);
			forbidden.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
			forbidden.Value.Should().BeEquivalentTo(new { message = "Required permission: write" });
		}

		[Fact]
		public async Task ReorderTickets_ShouldReturnBadRequest_WhenInputIsEmpty()
		{
			var options = new DbContextOptionsBuilder<ShalendarDbContext>()
				.UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
			using var context = new ShalendarDbContext(options);

			var (controller, mockJwtHelper, _, mockGetCalendarIdHelper, _, _) = CreateTicketsController(context);

			int calendarId = 1;
			mockGetCalendarIdHelper.Setup(h => h.TryGetCalendarId(It.IsAny<HttpContext>(), out calendarId)).Returns(true);
			mockJwtHelper.Setup(j => j.HasCalendarPermission(It.IsAny<HttpContext>(), "write", calendarId)).ReturnsAsync(true);

			var result = await controller.ReorderTickets(new List<TicketOrderUpdateDto>());

			var badRequest = Assert.IsType<BadRequestObjectResult>(result);
			badRequest.Value.Should().Be("Invalid order update data.");
		}

		[Fact]
		public async Task ReorderTickets_ShouldUpdatePositions_AndSendSignalR_ForCalendarList()
		{
			var options = new DbContextOptionsBuilder<ShalendarDbContext>()
				.UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
			using var context = new ShalendarDbContext(options);

			context.Tickets.Add(new Ticket { Id = 1, Name = "T1", CurrentPosition = 0, CurrentParentType = "CalendarList", ParentId = 100 });
			await context.SaveChangesAsync();

			var (controller, mockJwtHelper, _, mockGetCalendarIdHelper, mockGroupManager, mockHubContext) = CreateTicketsController(context);

			int calendarId = 1;
			mockGetCalendarIdHelper.Setup(h => h.TryGetCalendarId(It.IsAny<HttpContext>(), out calendarId)).Returns(true);
			mockJwtHelper.Setup(j => j.HasCalendarPermission(It.IsAny<HttpContext>(), "write", calendarId)).ReturnsAsync(true);
			mockGroupManager.Setup(g => g.IsUserAloneInGroup(calendarId.ToString())).Returns(false);

			var mockClients = new Mock<IHubClients>();
			var mockClientProxy = new Mock<IClientProxy>();
			mockClients.Setup(c => c.Group(calendarId.ToString())).Returns(mockClientProxy.Object);
			mockHubContext.Setup(h => h.Clients).Returns(mockClients.Object);

			var updates = new List<TicketOrderUpdateDto>
	{
		new TicketOrderUpdateDto { TicketId = 1, NewPosition = 5 }
	};

			var result = await controller.ReorderTickets(updates);

			Assert.IsType<NoContentResult>(result);

			var updated = context.Tickets.First(t => t.Id == 1);
			updated.CurrentPosition.Should().Be(5);

			mockClientProxy.Verify(
				m => m.SendCoreAsync("TicketReorderedInCalendarLists", It.IsAny<object[]>(), default),
				Times.Once);
		}

		[Fact]
		public async Task ReorderTickets_ShouldSendSignalR_ForDayView()
		{
			var options = new DbContextOptionsBuilder<ShalendarDbContext>()
				.UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
			using var context = new ShalendarDbContext(options);

			context.Days.Add(new Day { Id = 200, CalendarId = 1, Date = new DateTime(2024, 5, 1) });
			context.Tickets.Add(new Ticket { Id = 1, Name = "T1", CurrentPosition = 0, CurrentParentType = "ScheduledList", ParentId = 200 });
			await context.SaveChangesAsync();

			var (controller, mockJwtHelper, _, mockGetCalendarIdHelper, mockGroupManager, mockHubContext) = CreateTicketsController(context);

			int calendarId = 1;
			mockGetCalendarIdHelper.Setup(h => h.TryGetCalendarId(It.IsAny<HttpContext>(), out calendarId)).Returns(true);
			mockJwtHelper.Setup(j => j.HasCalendarPermission(It.IsAny<HttpContext>(), "write", calendarId)).ReturnsAsync(true);
			mockGroupManager.Setup(g => g.IsUserAloneInGroup(calendarId.ToString())).Returns(false);

			var mockClients = new Mock<IHubClients>();
			var mockClientProxy = new Mock<IClientProxy>();
			mockClients.Setup(c => c.Group(calendarId.ToString())).Returns(mockClientProxy.Object);
			mockHubContext.Setup(h => h.Clients).Returns(mockClients.Object);

			var updates = new List<TicketOrderUpdateDto>
	{
		new TicketOrderUpdateDto { TicketId = 1, NewPosition = 10 }
	};

			var result = await controller.ReorderTickets(updates);

			Assert.IsType<NoContentResult>(result);

			mockClientProxy.Verify(
				m => m.SendCoreAsync("TicketReorderedInDayView",
					It.Is<object[]>(o => ((DateTime)o[0]) == new DateTime(2024, 5, 1)), default),
				Times.Once);
		}

		[Fact]
		public async Task MoveTicketToCalendar_ShouldReturnBadRequest_WhenHeaderMissing()
		{
			var options = new DbContextOptionsBuilder<ShalendarDbContext>()
				.UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
			using var context = new ShalendarDbContext(options);

			var (controller, _, _, mockGetCalendarIdHelper, _, _) = CreateTicketsController(context);
			mockGetCalendarIdHelper.Setup(h => h.TryGetCalendarId(It.IsAny<HttpContext>(), out It.Ref<int>.IsAny)).Returns(false);

			var result = await controller.MoveTicketToCalendar(1);

			var badRequest = Assert.IsType<BadRequestObjectResult>(result);
			badRequest.Value.Should().Be("Invalid or missing X-Calendar-Id header.");
		}

		[Fact]
		public async Task MoveTicketToCalendar_ShouldReturnNotFound_WhenTicketDoesNotExist()
		{
			var options = new DbContextOptionsBuilder<ShalendarDbContext>()
				.UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
			using var context = new ShalendarDbContext(options);

			var (controller, mockJwtHelper, _, mockGetCalendarIdHelper, _, _) = CreateTicketsController(context);
			int calendarId = 1;
			mockGetCalendarIdHelper.Setup(h => h.TryGetCalendarId(It.IsAny<HttpContext>(), out calendarId)).Returns(true);
			mockJwtHelper.Setup(j => j.HasCalendarPermission(It.IsAny<HttpContext>(), "write", calendarId)).ReturnsAsync(true);

			var result = await controller.MoveTicketToCalendar(999);

			var notFound = Assert.IsType<NotFoundObjectResult>(result);
			notFound.Value.Should().Be("Ticket not found.");
		}

		[Fact]
		public async Task MoveTicketToCalendar_ShouldMoveTicketAndSendSignalR()
		{
			var options = new DbContextOptionsBuilder<ShalendarDbContext>()
				.UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
			using var context = new ShalendarDbContext(options);

			var day = new Day { Id = 10, CalendarId = 1, Date = new DateTime(2025, 3, 22) };
			var ticket = new Ticket
			{
				Id = 1,
				Name = "Test",
				CalendarListId = 5,
				CurrentParentType = "ScheduledList",
				ParentId = 10
			};
			context.Days.Add(day);
			context.Tickets.Add(ticket);
			await context.SaveChangesAsync();

			var (controller, mockJwtHelper, _, mockGetCalendarIdHelper, mockGroupManager, mockHubContext) = CreateTicketsController(context);
			int calendarId = 1;
			mockGetCalendarIdHelper.Setup(h => h.TryGetCalendarId(It.IsAny<HttpContext>(), out calendarId)).Returns(true);
			mockJwtHelper.Setup(j => j.HasCalendarPermission(It.IsAny<HttpContext>(), "write", calendarId)).ReturnsAsync(true);
			mockGroupManager.Setup(g => g.IsUserAloneInGroup(calendarId.ToString())).Returns(false);

			var mockClients = new Mock<IHubClients>();
			var mockClientProxy = new Mock<IClientProxy>();
			mockClients.Setup(c => c.Group(calendarId.ToString())).Returns(mockClientProxy.Object);
			mockHubContext.Setup(h => h.Clients).Returns(mockClients.Object);

			var result = await controller.MoveTicketToCalendar(1);

			var ok = Assert.IsType<OkObjectResult>(result);
			var updated = Assert.IsType<Ticket>(ok.Value);
			updated.CurrentParentType.Should().Be("CalendarList");

			mockClientProxy.Verify(
				m => m.SendCoreAsync("TicketMovedBackToCalendar", It.Is<object[]>(o => ((DateTime)o[0]) == day.Date), default),
				Times.Once);
		}

		[Fact]
		public async Task UpdateTicket_ShouldReturnBadRequest_WhenHeaderMissing()
		{
			var options = new DbContextOptionsBuilder<ShalendarDbContext>()
				.UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
			using var context = new ShalendarDbContext(options);

			var (controller, _, _, mockGetCalendarIdHelper, _, _) = CreateTicketsController(context);
			mockGetCalendarIdHelper.Setup(h => h.TryGetCalendarId(It.IsAny<HttpContext>(), out It.Ref<int>.IsAny)).Returns(false);

			var dto = new UpdateTicketDto { Id = 1, Name = "Test" };

			var result = await controller.UpdateTicket(dto);

			var badRequest = Assert.IsType<BadRequestObjectResult>(result);
			badRequest.Value.Should().Be("Invalid or missing X-Calendar-Id header.");
		}

		[Fact]
		public async Task UpdateTicket_ShouldReturnNotFound_WhenTicketNotFound()
		{
			var options = new DbContextOptionsBuilder<ShalendarDbContext>()
				.UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
			using var context = new ShalendarDbContext(options);

			var (controller, mockJwtHelper, _, mockGetCalendarIdHelper, _, _) = CreateTicketsController(context);
			int calendarId = 1;
			mockGetCalendarIdHelper.Setup(h => h.TryGetCalendarId(It.IsAny<HttpContext>(), out calendarId)).Returns(true);
			mockJwtHelper.Setup(j => j.HasCalendarPermission(It.IsAny<HttpContext>(), "write", calendarId)).ReturnsAsync(true);

			var dto = new UpdateTicketDto { Id = 123, Name = "Updated" };

			var result = await controller.UpdateTicket(dto);

			var notFound = Assert.IsType<NotFoundObjectResult>(result);
			notFound.Value.Should().Be("Ticket not found.");
		}

		[Fact]
		public async Task UpdateTicket_ShouldUpdateAndSendSignalR()
		{
			var options = new DbContextOptionsBuilder<ShalendarDbContext>()
				.UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
			using var context = new ShalendarDbContext(options);

			var day = new Day { Id = 200, CalendarId = 1, Date = new DateTime(2025, 3, 22) };
			var ticket = new Ticket
			{
				Id = 1,
				Name = "Old name",
				Description = "Old desc",
				CurrentParentType = "ScheduledList",
				ParentId = 200
			};
			context.Days.Add(day);
			context.Tickets.Add(ticket);
			await context.SaveChangesAsync();

			var (controller, mockJwtHelper, _, mockGetCalendarIdHelper, mockGroupManager, mockHubContext) = CreateTicketsController(context);
			int calendarId = 1;
			mockGetCalendarIdHelper.Setup(h => h.TryGetCalendarId(It.IsAny<HttpContext>(), out calendarId)).Returns(true);
			mockJwtHelper.Setup(j => j.HasCalendarPermission(It.IsAny<HttpContext>(), "write", calendarId)).ReturnsAsync(true);
			mockGroupManager.Setup(g => g.IsUserAloneInGroup(calendarId.ToString())).Returns(false);

			var mockClients = new Mock<IHubClients>();
			var mockClientProxy = new Mock<IClientProxy>();
			mockClients.Setup(c => c.Group(calendarId.ToString())).Returns(mockClientProxy.Object);
			mockHubContext.Setup(h => h.Clients).Returns(mockClients.Object);

			var dto = new UpdateTicketDto
			{
				Id = 1,
				Name = "New name",
				Description = "Updated desc",
				Priority = 2
			};

			var result = await controller.UpdateTicket(dto);

			var ok = Assert.IsType<OkObjectResult>(result);
			var updated = Assert.IsType<Ticket>(ok.Value);
			updated.Name.Should().Be("New name");
			updated.Description.Should().Be("Updated desc");

			mockClientProxy.Verify(
				m => m.SendCoreAsync("TicketUpdatedInDayView", It.Is<object[]>(o => ((DateTime)o[0]) == day.Date), default),
				Times.Once);
		}

		[Fact]
		public async Task UpdateTicketCompleted_ShouldReturnBadRequest_WhenHeaderMissing()
		{
			var options = new DbContextOptionsBuilder<ShalendarDbContext>()
				.UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
			using var context = new ShalendarDbContext(options);

			var (controller, _, _, mockGetCalendarIdHelper, _, _) = CreateTicketsController(context);
			mockGetCalendarIdHelper.Setup(h => h.TryGetCalendarId(It.IsAny<HttpContext>(), out It.Ref<int>.IsAny))
				.Returns(false);

			var result = await controller.UpdateTicketCompleted(1, true);

			var badRequest = Assert.IsType<BadRequestObjectResult>(result);
			badRequest.Value.Should().Be("Invalid or missing X-Calendar-Id header.");
		}

		[Fact]
		public async Task UpdateTicketCompleted_ShouldReturnNotFound_WhenTicketDoesNotExist()
		{
			var options = new DbContextOptionsBuilder<ShalendarDbContext>()
				.UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
			using var context = new ShalendarDbContext(options);

			var (controller, mockJwtHelper, _, mockGetCalendarIdHelper, _, _) = CreateTicketsController(context);
			int calendarId = 1;

			mockGetCalendarIdHelper.Setup(h => h.TryGetCalendarId(It.IsAny<HttpContext>(), out calendarId)).Returns(true);
			mockJwtHelper.Setup(j => j.HasCalendarPermission(It.IsAny<HttpContext>(), "write", calendarId)).ReturnsAsync(true);

			var result = await controller.UpdateTicketCompleted(42, true);

			var notFound = Assert.IsType<NotFoundObjectResult>(result);
			notFound.Value.Should().BeEquivalentTo(new { message = "Ticket not found" });
		}

		[Fact]
		public async Task UpdateTicketCompleted_ShouldUpdateAndSendSignalR()
		{
			var options = new DbContextOptionsBuilder<ShalendarDbContext>()
				.UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
			using var context = new ShalendarDbContext(options);

			var day = new Day { Id = 100, CalendarId = 1, Date = new DateTime(2025, 3, 22) };
			var ticket = new Ticket
			{
				Id = 1,
				Name = "Test ticket",
				CurrentParentType = "ScheduledList",
				ParentId = day.Id,
				IsCompleted = false
			};
			context.Days.Add(day);
			context.Tickets.Add(ticket);
			await context.SaveChangesAsync();

			var (controller, mockJwtHelper, _, mockGetCalendarIdHelper, mockGroupManager, mockHubContext) = CreateTicketsController(context);

			int calendarId = 1;
			mockGetCalendarIdHelper.Setup(h => h.TryGetCalendarId(It.IsAny<HttpContext>(), out calendarId)).Returns(true);
			mockJwtHelper.Setup(j => j.HasCalendarPermission(It.IsAny<HttpContext>(), "write", calendarId)).ReturnsAsync(true);
			mockGroupManager.Setup(g => g.IsUserAloneInGroup(calendarId.ToString())).Returns(false);

			var mockClients = new Mock<IHubClients>();
			var mockClientProxy = new Mock<IClientProxy>();
			mockClients.Setup(c => c.Group(calendarId.ToString())).Returns(mockClientProxy.Object);
			mockHubContext.Setup(h => h.Clients).Returns(mockClients.Object);

			var result = await controller.UpdateTicketCompleted(1, true);

			Assert.IsType<OkResult>(result);
			ticket.IsCompleted.Should().BeTrue();

			mockClientProxy.Verify(
				m => m.SendCoreAsync("TicketCompletedUpdatedInDayView", It.Is<object[]>(o => ((DateTime)o[0]) == day.Date), default),
				Times.Once);
		}

		[Fact]
		public async Task ChangeTicketDate_ShouldReturnBadRequest_WhenDateIsInvalid()
		{
			var options = new DbContextOptionsBuilder<ShalendarDbContext>()
				.UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
			using var context = new ShalendarDbContext(options);

			var ticket = new Ticket
			{
				Id = 1,
				Name = "Test ticket",
				CurrentParentType = "ScheduledList",
				ParentId = 1
			};
			context.Tickets.Add(ticket);
			await context.SaveChangesAsync();

			var (controller, mockJwtHelper, _, mockGetCalendarIdHelper, _, _) = CreateTicketsController(context);
			int calendarId = 1;

			mockGetCalendarIdHelper.Setup(h => h.TryGetCalendarId(It.IsAny<HttpContext>(), out calendarId)).Returns(true);
			mockJwtHelper.Setup(j => j.HasCalendarPermission(It.IsAny<HttpContext>(), "write", calendarId)).ReturnsAsync(true);

			var result = await controller.ChangeTicketDate(1, "invalid-date");

			var badRequest = Assert.IsType<BadRequestObjectResult>(result);
			badRequest.Value.Should().Be("Invalid date format.");
		}

		[Fact]
		public async Task ChangeTicketDate_ShouldReturnNotFound_WhenTicketMissing()
		{
			var options = new DbContextOptionsBuilder<ShalendarDbContext>()
				.UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
			using var context = new ShalendarDbContext(options);

			var (controller, mockJwtHelper, _, mockGetCalendarIdHelper, _, _) = CreateTicketsController(context);
			int calendarId = 1;

			mockGetCalendarIdHelper.Setup(h => h.TryGetCalendarId(It.IsAny<HttpContext>(), out calendarId)).Returns(true);
			mockJwtHelper.Setup(j => j.HasCalendarPermission(It.IsAny<HttpContext>(), "write", calendarId)).ReturnsAsync(true);

			var result = await controller.ChangeTicketDate(999, "2025-03-22");

			var notFound = Assert.IsType<NotFoundObjectResult>(result);
			notFound.Value.Should().Be("Ticket not found.");
		}

		[Fact]
		public async Task ChangeTicketDate_ShouldChangeDate_AndSendSignalR()
		{
			var options = new DbContextOptionsBuilder<ShalendarDbContext>()
				.UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
			using var context = new ShalendarDbContext(options);

			var oldDay = new Day { Id = 1, CalendarId = 1, Date = new DateTime(2025, 3, 20) };
			var ticket = new Ticket
			{
				Id = 1,
				Name = "Test ticket",
				CurrentParentType = "ScheduledList",
				ParentId = 1
			};
			context.Days.Add(oldDay);
			context.Tickets.Add(ticket);
			await context.SaveChangesAsync();

			var (controller, mockJwtHelper, _, mockGetCalendarIdHelper, mockGroupManager, mockHubContext) = CreateTicketsController(context);
			int calendarId = 1;

			mockGetCalendarIdHelper.Setup(h => h.TryGetCalendarId(It.IsAny<HttpContext>(), out calendarId)).Returns(true);
			mockJwtHelper.Setup(j => j.HasCalendarPermission(It.IsAny<HttpContext>(), "write", calendarId)).ReturnsAsync(true);
			mockGroupManager.Setup(g => g.IsUserAloneInGroup(calendarId.ToString())).Returns(false);

			var mockClients = new Mock<IHubClients>();
			var mockClientProxy = new Mock<IClientProxy>();
			mockClients.Setup(c => c.Group(calendarId.ToString())).Returns(mockClientProxy.Object);
			mockHubContext.Setup(h => h.Clients).Returns(mockClients.Object);

			var result = await controller.ChangeTicketDate(1, "2025-03-25");

			var ok = Assert.IsType<OkObjectResult>(result);
			var updatedTicket = Assert.IsType<Ticket>(ok.Value);

			var newDay = context.Days.FirstOrDefault(d => d.Date == new DateTime(2025, 3, 25));
			updatedTicket.ParentId.Should().Be(newDay?.Id);

			mockClientProxy.Verify(
				m => m.SendCoreAsync("TicketMovedBetweenDays", It.Is<object[]>(o => ((DateTime)o[0]) == new DateTime(2025, 3, 25)), default),
				Times.Once);

			mockClientProxy.Verify(
				m => m.SendCoreAsync("TicketMovedBetweenDays", It.Is<object[]>(o => ((DateTime)o[0]) == oldDay.Date), default),
				Times.Once);
		}


		#endregion



		#region Deletes

		[Fact]
		public async Task DeleteTicket_ShouldReturnBadRequest_WhenHeaderMissing()
		{
			var options = new DbContextOptionsBuilder<ShalendarDbContext>()
				.UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
			using var context = new ShalendarDbContext(options);

			var (controller, _, _, mockGetCalendarIdHelper, _, _) = CreateTicketsController(context);
			mockGetCalendarIdHelper.Setup(h => h.TryGetCalendarId(It.IsAny<HttpContext>(), out It.Ref<int>.IsAny))
				.Returns(false);

			var result = await controller.DeleteTicket(1);

			var badRequest = Assert.IsType<BadRequestObjectResult>(result);
			badRequest.Value.Should().Be("Invalid or missing X-Calendar-Id header.");
		}

		[Fact]
		public async Task DeleteTicket_ShouldReturnNotFound_WhenTicketDoesNotExist()
		{
			var options = new DbContextOptionsBuilder<ShalendarDbContext>()
				.UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
			using var context = new ShalendarDbContext(options);

			var (controller, mockJwtHelper, _, mockGetCalendarIdHelper, _, _) = CreateTicketsController(context);
			int calendarId = 1;

			mockGetCalendarIdHelper.Setup(h => h.TryGetCalendarId(It.IsAny<HttpContext>(), out calendarId)).Returns(true);
			mockJwtHelper.Setup(j => j.HasCalendarPermission(It.IsAny<HttpContext>(), "write", calendarId)).ReturnsAsync(true);

			var result = await controller.DeleteTicket(42);

			Assert.IsType<NotFoundResult>(result);
		}

		[Fact]
		public async Task DeleteTicket_ShouldDeleteTicket_AndSendSignalR_ForCalendarList()
		{
			var options = new DbContextOptionsBuilder<ShalendarDbContext>()
				.UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
			using var context = new ShalendarDbContext(options);

			var ticket = new Ticket
			{
				Id = 1,
				Name = "To delete",
				CurrentParentType = "CalendarList",
				CalendarListId = 5,
				ParentId = 100
			};
			context.Tickets.Add(ticket);
			await context.SaveChangesAsync();

			var (controller, mockJwtHelper, _, mockGetCalendarIdHelper, mockGroupManager, mockHubContext) = CreateTicketsController(context);

			int calendarId = 1;
			mockGetCalendarIdHelper.Setup(h => h.TryGetCalendarId(It.IsAny<HttpContext>(), out calendarId)).Returns(true);
			mockJwtHelper.Setup(j => j.HasCalendarPermission(It.IsAny<HttpContext>(), "write", calendarId)).ReturnsAsync(true);
			mockGroupManager.Setup(g => g.IsUserAloneInGroup(calendarId.ToString())).Returns(false);

			var mockClients = new Mock<IHubClients>();
			var mockClientProxy = new Mock<IClientProxy>();
			mockClients.Setup(c => c.Group(calendarId.ToString())).Returns(mockClientProxy.Object);
			mockHubContext.Setup(h => h.Clients).Returns(mockClients.Object);

			var result = await controller.DeleteTicket(1);

			Assert.IsType<NoContentResult>(result);
			context.Tickets.Should().BeEmpty();

			mockClientProxy.Verify(
				m => m.SendCoreAsync("TicketDeletedInCalendarLists", It.IsAny<object[]>(), default),
				Times.Once);
		}

		[Fact]
		public async Task DeleteTicket_ShouldDeleteTicket_AndSendSignalR_ForDayView()
		{
			var options = new DbContextOptionsBuilder<ShalendarDbContext>()
				.UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
			using var context = new ShalendarDbContext(options);

			var day = new Day { Id = 10, CalendarId = 1, Date = new DateTime(2025, 3, 22) };
			var ticket = new Ticket
			{
				Id = 1,
				Name = "To delete",
				CurrentParentType = "ScheduledList",
				ParentId = day.Id
			};
			context.Days.Add(day);
			context.Tickets.Add(ticket);
			await context.SaveChangesAsync();

			var (controller, mockJwtHelper, _, mockGetCalendarIdHelper, mockGroupManager, mockHubContext) = CreateTicketsController(context);

			int calendarId = 1;
			mockGetCalendarIdHelper.Setup(h => h.TryGetCalendarId(It.IsAny<HttpContext>(), out calendarId)).Returns(true);
			mockJwtHelper.Setup(j => j.HasCalendarPermission(It.IsAny<HttpContext>(), "write", calendarId)).ReturnsAsync(true);
			mockGroupManager.Setup(g => g.IsUserAloneInGroup(calendarId.ToString())).Returns(false);

			var mockClients = new Mock<IHubClients>();
			var mockClientProxy = new Mock<IClientProxy>();
			mockClients.Setup(c => c.Group(calendarId.ToString())).Returns(mockClientProxy.Object);
			mockHubContext.Setup(h => h.Clients).Returns(mockClients.Object);

			var result = await controller.DeleteTicket(1);

			Assert.IsType<NoContentResult>(result);
			context.Tickets.Should().BeEmpty();

			mockClientProxy.Verify(
				m => m.SendCoreAsync("TicketDeletedInDayView", It.Is<object[]>(o => ((DateTime)o[0]) == day.Date), default),
				Times.Once);
		}

		#endregion
	}
}