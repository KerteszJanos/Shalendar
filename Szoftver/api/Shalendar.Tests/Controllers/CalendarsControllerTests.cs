// Gpt generated
using Xunit;
using Moq;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Shalendar.Controllers;
using Shalendar.Models;
using Shalendar.Contexts;
using Shalendar.Functions;
using Shalendar.Functions.Interfaces;
using Shalendar.Services.Interfaces;
using System.Threading.Tasks;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Shalendar.Tests.Controllers
{
	public class CalendarsControllerTests
	{
		private (CalendarsController controller,
				 Mock<IJwtHelper> jwtHelper,
				 Mock<IDeleteCalendarHelper> deleteCalendarHelper,
				 Mock<ICopyTicketHelper> copyTicketHelper,
				 Mock<IGroupManagerService> groupManager,
				 Mock<IHubContext<CalendarHub>> hubContext)
			CreateCalendarsController(ShalendarDbContext context)
		{
			var mockJwtHelper = new Mock<IJwtHelper>();
			var mockDeleteCalendarHelper = new Mock<IDeleteCalendarHelper>();
			var mockCopyTicketHelper = new Mock<ICopyTicketHelper>();
			var mockGroupManager = new Mock<IGroupManagerService>();
			var mockHubContext = new Mock<IHubContext<CalendarHub>>();

			var controller = new CalendarsController(
				context,
				mockJwtHelper.Object,
				mockDeleteCalendarHelper.Object,
				mockCopyTicketHelper.Object,
				mockGroupManager.Object,
				mockHubContext.Object
			);

			controller.ControllerContext = new ControllerContext
			{
				HttpContext = new DefaultHttpContext()
			};

			return (controller, mockJwtHelper, mockDeleteCalendarHelper, mockCopyTicketHelper, mockGroupManager, mockHubContext);
		}

		#region Gets

		[Fact]
		public async Task GetCalendar_ShouldReturnNotFound_WhenCalendarDoesNotExist()
		{
			var options = new DbContextOptionsBuilder<ShalendarDbContext>()
				.UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
			using var context = new ShalendarDbContext(options);

			var (controller, mockJwtHelper, _, _, _, _) = CreateCalendarsController(context);

			var result = await controller.GetCalendar(1);

			Assert.IsType<NotFoundResult>(result.Result);
		}

		[Fact]
		public async Task GetCalendar_ShouldReturn403_WhenNoPermission()
		{
			var options = new DbContextOptionsBuilder<ShalendarDbContext>()
				.UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
			using var context = new ShalendarDbContext(options);

			context.Calendars.Add(new Calendar { Id = 1, Name = "Test Calendar" });
			await context.SaveChangesAsync();

			var (controller, mockJwtHelper, _, _, _, _) = CreateCalendarsController(context);

			mockJwtHelper.Setup(j => j.HasCalendarPermission(It.IsAny<HttpContext>(), "read")).ReturnsAsync(false);

			var result = await controller.GetCalendar(1);

			var objectResult = Assert.IsType<ObjectResult>(result.Result);
			objectResult.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
			objectResult.Value.Should().BeEquivalentTo(new { message = "Required permission: read for the calendar named: Test Calendar" });
		}

		[Fact]
		public async Task GetCalendar_ShouldReturnCalendar_WhenPermissionGranted()
		{
			var options = new DbContextOptionsBuilder<ShalendarDbContext>()
				.UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
			using var context = new ShalendarDbContext(options);

			context.Calendars.Add(new Calendar { Id = 1, Name = "Test Calendar" });
			await context.SaveChangesAsync();

			var (controller, mockJwtHelper, _, _, _, _) = CreateCalendarsController(context);

			mockJwtHelper.Setup(j => j.HasCalendarPermission(It.IsAny<HttpContext>(), "read")).ReturnsAsync(true);

			var result = await controller.GetCalendar(1);

			var calendar = Assert.IsType<Calendar>(result.Value);
			calendar.Name.Should().Be("Test Calendar");
		}

		[Fact]
		public async Task GetCalendarNoPermissionNeeded_ShouldReturnNotFound_WhenNotExist()
		{
			var options = new DbContextOptionsBuilder<ShalendarDbContext>()
				.UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
			using var context = new ShalendarDbContext(options);

			var (controller, _, _, _, _, _) = CreateCalendarsController(context);

			var result = await controller.GetCalendarNoPermissionNeeded(123);

			Assert.IsType<NotFoundResult>(result.Result);
		}

		[Fact]
		public async Task GetCalendarNoPermissionNeeded_ShouldReturnCalendar_WhenExists()
		{
			var options = new DbContextOptionsBuilder<ShalendarDbContext>()
				.UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
			using var context = new ShalendarDbContext(options);

			context.Calendars.Add(new Calendar { Id = 42, Name = "Public Calendar" });
			await context.SaveChangesAsync();

			var (controller, _, _, _, _, _) = CreateCalendarsController(context);

			var result = await controller.GetCalendarNoPermissionNeeded(42);

			var calendar = Assert.IsType<Calendar>(result.Value);
			calendar.Name.Should().Be("Public Calendar");
		}

		[Fact]
		public async Task GetUserCalendarPermissions_ShouldReturnPermissions_ForUser()
		{
			var options = new DbContextOptionsBuilder<ShalendarDbContext>()
				.UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
			using var context = new ShalendarDbContext(options);

			context.CalendarPermissions.Add(new CalendarPermission
			{
				CalendarId = 1,
				UserId = 5,
				PermissionType = "write"
			});
			await context.SaveChangesAsync();

			var (controller, _, _, _, _, _) = CreateCalendarsController(context);

			var result = await controller.GetUserCalendarPermissions(5);

			var okResult = Assert.IsType<OkObjectResult>(result.Result);
			var permissions = Assert.IsAssignableFrom<IEnumerable<CalendarPermission>>(okResult.Value);
			permissions.Should().HaveCount(1);
		}

		[Fact]
		public async Task GetCalendarPermissions_ShouldReturn403_WhenNoPermission()
		{
			var options = new DbContextOptionsBuilder<ShalendarDbContext>()
				.UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
			using var context = new ShalendarDbContext(options);

			var (controller, mockJwtHelper, _, _, _, _) = CreateCalendarsController(context);

			mockJwtHelper.Setup(j => j.HasCalendarPermission(It.IsAny<HttpContext>(), "owner", 10)).ReturnsAsync(false);

			var result = await controller.GetCalendarPermissions(10);

			var objectResult = Assert.IsType<ObjectResult>(result.Result);
			objectResult.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
			objectResult.Value.Should().BeEquivalentTo(new { message = "Required permission: owner" });
		}

		[Fact]
		public async Task GetCalendarPermissions_ShouldReturnNotFound_WhenNoneExist()
		{
			var options = new DbContextOptionsBuilder<ShalendarDbContext>()
				.UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
			using var context = new ShalendarDbContext(options);

			context.Calendars.Add(new Calendar { Id = 1, Name = "Empty Calendar" });
			await context.SaveChangesAsync();

			context.Users.Add(new User { Id = 1, Email = "test@example.com" });
			await context.SaveChangesAsync();

			var (controller, mockJwtHelper, _, _, _, _) = CreateCalendarsController(context);

			mockJwtHelper.Setup(j => j.HasCalendarPermission(It.IsAny<HttpContext>(), "owner", 1)).ReturnsAsync(true);

			var result = await controller.GetCalendarPermissions(1);

			var notFound = Assert.IsType<NotFoundObjectResult>(result.Result);
			notFound.Value.Should().Be("No permissions found for this calendar.");
		}

		[Fact]
		public async Task GetCalendarPermissions_ShouldReturnPermissions_WhenTheyExist()
		{
			var options = new DbContextOptionsBuilder<ShalendarDbContext>()
				.UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
			using var context = new ShalendarDbContext(options);

			context.Users.Add(new User { Id = 1, Email = "user@example.com" });
			context.CalendarPermissions.Add(new CalendarPermission
			{
				CalendarId = 7,
				UserId = 1,
				PermissionType = "owner"
			});
			await context.SaveChangesAsync();

			var (controller, mockJwtHelper, _, _, _, _) = CreateCalendarsController(context);

			mockJwtHelper.Setup(j => j.HasCalendarPermission(It.IsAny<HttpContext>(), "owner", 7)).ReturnsAsync(true);

			var result = await controller.GetCalendarPermissions(7);

			var ok = Assert.IsType<OkObjectResult>(result.Result);
			var permissions = Assert.IsAssignableFrom<IEnumerable<object>>(ok.Value);
			permissions.Should().ContainSingle();
		}

		[Fact]
		public async Task GetUserAccessibleCalendars_ShouldReturnBadRequest_WhenUserClaimMissing()
		{
			var options = new DbContextOptionsBuilder<ShalendarDbContext>()
				.UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
			using var context = new ShalendarDbContext(options);

			var (controller, _, _, _, _, _) = CreateCalendarsController(context);

			// user claim not set
			var result = await controller.GetUserAccessibleCalendars();

			Assert.IsType<BadRequestResult>(result.Result);
		}

		[Fact]
		public async Task GetUserAccessibleCalendars_ShouldReturnNotFound_WhenUserHasNoAccess()
		{
			var options = new DbContextOptionsBuilder<ShalendarDbContext>()
				.UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
			using var context = new ShalendarDbContext(options);

			context.Users.Add(new User { Id = 123, Email = "abc@a.com" });
			await context.SaveChangesAsync();

			var (controller, _, _, _, _, _) = CreateCalendarsController(context);

			controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new[]
			{
		new Claim(ClaimTypes.NameIdentifier, "123")
	}, "mock"));

			var result = await controller.GetUserAccessibleCalendars();

			var notFound = Assert.IsType<NotFoundObjectResult>(result.Result);
			notFound.Value.Should().BeEquivalentTo(new { message = "No calendars found with owner or write permissions." });
		}

		[Fact]
		public async Task GetUserAccessibleCalendars_ShouldReturnCalendars_WhenPermissionsExist()
		{
			var options = new DbContextOptionsBuilder<ShalendarDbContext>()
				.UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
			using var context = new ShalendarDbContext(options);

			context.Calendars.Add(new Calendar { Id = 1, Name = "My Cal" });
			context.CalendarPermissions.Add(new CalendarPermission { CalendarId = 1, UserId = 99, PermissionType = "write" });
			await context.SaveChangesAsync();

			var (controller, _, _, _, _, _) = CreateCalendarsController(context);

			controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new[]
			{
		new Claim(ClaimTypes.NameIdentifier, "99")
	}, "mock"));

			var result = await controller.GetUserAccessibleCalendars();

			var ok = Assert.IsType<OkObjectResult>(result.Result);
			var calendars = Assert.IsAssignableFrom<IEnumerable<object>>(ok.Value);
			calendars.Should().ContainSingle();
		}

		#endregion



		#region Posts

		[Fact]
		public async Task CreateCalendar_ShouldCreateCalendarWithDefaults_WhenValid()
		{
			var options = new DbContextOptionsBuilder<ShalendarDbContext>()
				.UseInMemoryDatabase(Guid.NewGuid().ToString()).ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning)).Options;
			using var context = new ShalendarDbContext(options);

			var (controller, _, _, _, _, _) = CreateCalendarsController(context);

			var newCalendar = new Calendar { Name = "New Calendar" };

			var result = await controller.CreateCalendar(1, newCalendar);

			var created = Assert.IsType<CreatedAtActionResult>(result.Result);
			var calendar = Assert.IsType<Calendar>(created.Value);

			calendar.Name.Should().Be("New Calendar");

			context.Calendars.Should().ContainSingle(c => c.Name == "New Calendar");
			context.CalendarPermissions.Should().ContainSingle(cp => cp.UserId == 1 && cp.PermissionType == "owner");
			context.CalendarLists.Should().ContainSingle(cl => cl.Name == "Default List");
		}


		[Fact]
		public async Task AddCalendarPermission_ShouldReturn403_WhenUserNotOwner()
		{
			var options = new DbContextOptionsBuilder<ShalendarDbContext>()
				.UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
			using var context = new ShalendarDbContext(options);

			var (controller, jwtHelper, _, _, _, _) = CreateCalendarsController(context);

			jwtHelper.Setup(j => j.HasCalendarPermission(It.IsAny<HttpContext>(), "owner", 1)).ReturnsAsync(false);

			var result = await controller.AddCalendarPermission(1, "test@example.com", "write");

			var forbidden = Assert.IsType<ObjectResult>(result);
			forbidden.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
		}

		[Fact]
		public async Task AddCalendarPermission_ShouldReturnNotFound_WhenUserDoesNotExist()
		{
			var options = new DbContextOptionsBuilder<ShalendarDbContext>()
				.UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
			using var context = new ShalendarDbContext(options);

			var (controller, jwtHelper, _, _, _, _) = CreateCalendarsController(context);

			jwtHelper.Setup(j => j.HasCalendarPermission(It.IsAny<HttpContext>(), "owner", 1)).ReturnsAsync(true);

			var result = await controller.AddCalendarPermission(1, "missing@example.com", "write");

			var notFound = Assert.IsType<NotFoundObjectResult>(result);
			notFound.Value.Should().BeEquivalentTo(new { message = "User not found." });
		}

		[Fact]
		public async Task AddCalendarPermission_ShouldUpdate_WhenPermissionExists()
		{
			var options = new DbContextOptionsBuilder<ShalendarDbContext>()
				.UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
			using var context = new ShalendarDbContext(options);

			var user = new User { Id = 1, Email = "user@example.com" };
			context.Users.Add(user);
			context.CalendarPermissions.Add(new CalendarPermission
			{
				CalendarId = 5,
				UserId = user.Id,
				PermissionType = "read"
			});
			await context.SaveChangesAsync();

			var (controller, jwtHelper, _, _, _, _) = CreateCalendarsController(context);

			jwtHelper.Setup(j => j.HasCalendarPermission(It.IsAny<HttpContext>(), "owner", 5)).ReturnsAsync(true);

			var result = await controller.AddCalendarPermission(5, user.Email, "write");

			Assert.IsType<OkResult>(result);

			var updated = context.CalendarPermissions.First();
			updated.PermissionType.Should().Be("write");
		}

		[Fact]
		public async Task AddCalendarPermission_ShouldAdd_WhenPermissionDoesNotExist()
		{
			var options = new DbContextOptionsBuilder<ShalendarDbContext>()
				.UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
			using var context = new ShalendarDbContext(options);

			var user = new User { Id = 2, Email = "newuser@example.com" };
			context.Users.Add(user);
			await context.SaveChangesAsync();

			var (controller, jwtHelper, _, _, _, _) = CreateCalendarsController(context);

			jwtHelper.Setup(j => j.HasCalendarPermission(It.IsAny<HttpContext>(), "owner", 3)).ReturnsAsync(true);

			var result = await controller.AddCalendarPermission(3, user.Email, "write");

			Assert.IsType<OkResult>(result);
			context.CalendarPermissions.Should().ContainSingle(cp => cp.UserId == 2 && cp.CalendarId == 3 && cp.PermissionType == "write");
		}

		[Fact]
		public async Task CopyAllTickets_ShouldReturnBadRequest_WhenHeaderMissing()
		{
			var options = new DbContextOptionsBuilder<ShalendarDbContext>()
				.UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
			using var context = new ShalendarDbContext(options);

			var (controller, _, _, _, _, _) = CreateCalendarsController(context);

			var result = await controller.CopyAllTickets(10);

			Assert.IsType<BadRequestResult>(result);
		}

		[Fact]
		public async Task CopyAllTickets_ShouldReturn403_WhenNoReadPermission()
		{
			var options = new DbContextOptionsBuilder<ShalendarDbContext>()
				.UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
			using var context = new ShalendarDbContext(options);

			context.Calendars.Add(new Calendar { Id = 1, Name = "Original" });
			await context.SaveChangesAsync();

			var (controller, jwtHelper, _, _, _, _) = CreateCalendarsController(context);

			controller.ControllerContext.HttpContext.Request.Headers["X-Calendar-Id"] = "1";

			jwtHelper.Setup(j => j.HasCalendarPermission(It.IsAny<HttpContext>(), "read")).ReturnsAsync(false);

			var result = await controller.CopyAllTickets(99);

			var forbidden = Assert.IsType<ObjectResult>(result);
			forbidden.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
			forbidden.Value.Should().BeEquivalentTo(new { message = "Required permission: read for calendar: 'Original'" });
		}

		[Fact]
		public async Task CopyAllTickets_ShouldReturn403_WhenNoWritePermission()
		{
			var options = new DbContextOptionsBuilder<ShalendarDbContext>()
				.UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
			using var context = new ShalendarDbContext(options);

			context.Calendars.AddRange(
				new Calendar { Id = 1, Name = "Original" },
				new Calendar { Id = 2, Name = "Target" }
			);
			await context.SaveChangesAsync();

			var (controller, jwtHelper, _, _, _, _) = CreateCalendarsController(context);

			controller.ControllerContext.HttpContext.Request.Headers["X-Calendar-Id"] = "1";

			jwtHelper.Setup(j => j.HasCalendarPermission(It.IsAny<HttpContext>(), "read")).ReturnsAsync(true);
			jwtHelper.Setup(j => j.HasCalendarPermission(It.IsAny<HttpContext>(), "write", 2)).ReturnsAsync(false);

			var result = await controller.CopyAllTickets(2);

			var forbidden = Assert.IsType<ObjectResult>(result);
			forbidden.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
			forbidden.Value.Should().BeEquivalentTo(new { message = "Required permission: write for calendar: 'Target'" });
		}

		#endregion



		#region Puts

		#endregion



		#region Deletes

		[Fact]
		public async Task DeleteCalendarPermission_ShouldReturnNotFound_WhenUserNotFound()
		{
			var options = new DbContextOptionsBuilder<ShalendarDbContext>()
				.UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
			using var context = new ShalendarDbContext(options);

			var (controller, _, _, _, _, _) = CreateCalendarsController(context);

			var result = await controller.DeleteCalendarPermission(1, "nonexistent@example.com");

			var notFound = Assert.IsType<NotFoundObjectResult>(result);
			notFound.Value.Should().BeEquivalentTo(new { message = "User not found." });
		}

		[Fact]
		public async Task DeleteCalendarPermission_ShouldReturnNotFound_WhenPermissionNotFound()
		{
			var options = new DbContextOptionsBuilder<ShalendarDbContext>()
				.UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
			using var context = new ShalendarDbContext(options);

			var user = new User { Id = 1, Email = "user@example.com" };
			context.Users.Add(user);
			await context.SaveChangesAsync();

			var (controller, _, _, _, _, _) = CreateCalendarsController(context);

			var result = await controller.DeleteCalendarPermission(10, user.Email);

			var notFound = Assert.IsType<NotFoundObjectResult>(result);
			notFound.Value.Should().BeEquivalentTo(new { message = "Permission not found." });
		}

		[Fact]
		public async Task DeleteCalendarPermission_ShouldRemovePermission_WhenExists()
		{
			var options = new DbContextOptionsBuilder<ShalendarDbContext>()
				.UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
			using var context = new ShalendarDbContext(options);

			var user = new User { Id = 1, Email = "user@example.com" };
			var permission = new CalendarPermission { CalendarId = 5, UserId = 1, PermissionType = "write" };
			context.Users.Add(user);
			context.CalendarPermissions.Add(permission);
			await context.SaveChangesAsync();

			var (controller, _, _, _, _, _) = CreateCalendarsController(context);

			var result = await controller.DeleteCalendarPermission(5, user.Email);

			Assert.IsType<OkResult>(result);
			context.CalendarPermissions.Should().BeEmpty();
		}

		[Fact]
		public async Task DeleteCalendar_ShouldReturnUnauthorized_WhenUserNotAuthenticated()
		{
			var options = new DbContextOptionsBuilder<ShalendarDbContext>()
				.UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
			using var context = new ShalendarDbContext(options);

			var (controller, _, deleteHelper, _, _, _) = CreateCalendarsController(context);

			// No user claim set
			var result = await controller.DeleteCalendar(99);

			var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result);
			unauthorized.Value.Should().BeEquivalentTo(new { message = "User not authenticated." });
		}

		[Fact]
		public async Task DeleteCalendar_ShouldReturnOk_WhenCalendarShouldNotBeDeleted()
		{
			var options = new DbContextOptionsBuilder<ShalendarDbContext>()
				.UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
			using var context = new ShalendarDbContext(options);

			var (controller, _, deleteHelper, _, _, _) = CreateCalendarsController(context);

			controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new[]
			{
		new Claim(ClaimTypes.NameIdentifier, "5")
	}, "mock"));

			deleteHelper.Setup(d => d.ShouldDeleteCalendar(5, 10)).ReturnsAsync(false);

			var result = await controller.DeleteCalendar(10);

			var ok = Assert.IsType<OkObjectResult>(result);
			ok.Value.Should().BeEquivalentTo(new { message = "User permission removed." });
		}

		[Fact]
		public async Task DeleteCalendar_ShouldDelete_WhenAllowed()
		{
			var options = new DbContextOptionsBuilder<ShalendarDbContext>()
				.UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
			using var context = new ShalendarDbContext(options);

			var (controller, _, deleteHelper, _, _, _) = CreateCalendarsController(context);

			controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new[]
			{
		new Claim(ClaimTypes.NameIdentifier, "8")
	}, "mock"));

			deleteHelper.Setup(d => d.ShouldDeleteCalendar(8, 20)).ReturnsAsync(true);
			deleteHelper.Setup(d => d.DeleteCalendar(20)).Returns(Task.CompletedTask);

			var result = await controller.DeleteCalendar(20);

			var ok = Assert.IsType<OkObjectResult>(result);
			ok.Value.Should().BeEquivalentTo(new { message = "Calendar deleted." });
		}

		#endregion
	}
}
