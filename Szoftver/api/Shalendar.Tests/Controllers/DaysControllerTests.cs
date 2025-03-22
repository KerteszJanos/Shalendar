using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Moq;
using Shalendar.Contexts;
using Shalendar.Controllers;
using Shalendar.Functions.Interfaces;
using Shalendar.Models;
using Shalendar.Models.Dtos;
using Xunit;

namespace Shalendar.Tests.Controllers
{
	public class DaysControllerTests
	{
		private (DaysController controller, Mock<IJwtHelper> jwtHelper) CreateDaysController(ShalendarDbContext context)
		{
			var mockJwtHelper = new Mock<IJwtHelper>();

			var controller = new DaysController(context, mockJwtHelper.Object)
			{
				ControllerContext = new ControllerContext
				{
					HttpContext = new DefaultHttpContext()
				}
			};

			return (controller, mockJwtHelper);
		}

		#region Gets

		[Fact]
		public async Task GetDayId_ShouldReturn403_WhenNoPermission()
		{
			var options = new DbContextOptionsBuilder<ShalendarDbContext>()
				.UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
			using var context = new ShalendarDbContext(options);

			var (controller, jwtHelper) = CreateDaysController(context);
			jwtHelper.Setup(j => j.HasCalendarPermission(It.IsAny<HttpContext>(), "read")).ReturnsAsync(false);

			var result = await controller.GetDayId("2024-01-01", 1);

			var objectResult = Assert.IsType<ObjectResult>(result);
			objectResult.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
			objectResult.Value.Should().BeEquivalentTo(new { message = "Required permission: read" });
		}

		[Fact]
		public async Task GetDayId_ShouldReturnBadRequest_WhenDateInvalid()
		{
			var options = new DbContextOptionsBuilder<ShalendarDbContext>()
				.UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
			using var context = new ShalendarDbContext(options);

			var (controller, jwtHelper) = CreateDaysController(context);
			jwtHelper.Setup(j => j.HasCalendarPermission(It.IsAny<HttpContext>(), "read")).ReturnsAsync(true);

			var result = await controller.GetDayId("invalid-date", 1);

			Assert.IsType<BadRequestObjectResult>(result);
		}

		[Fact]
		public async Task GetDayId_ShouldReturnNullId_WhenDayNotExists()
		{
			var options = new DbContextOptionsBuilder<ShalendarDbContext>()
				.UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
			using var context = new ShalendarDbContext(options);

			var (controller, jwtHelper) = CreateDaysController(context);
			jwtHelper.Setup(j => j.HasCalendarPermission(It.IsAny<HttpContext>(), "read")).ReturnsAsync(true);

			var result = await controller.GetDayId("2024-01-01", 1);

			var ok = Assert.IsType<OkObjectResult>(result);
			ok.Value.Should().BeEquivalentTo(new { id = (int?)null });
		}

		[Fact]
		public async Task GetDayId_ShouldReturnDayId_WhenExists()
		{
			var options = new DbContextOptionsBuilder<ShalendarDbContext>()
				.UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
			using var context = new ShalendarDbContext(options);

			context.Days.Add(new Day { Id = 99, CalendarId = 2, Date = new DateTime(2024, 01, 01) });
			await context.SaveChangesAsync();

			var (controller, jwtHelper) = CreateDaysController(context);
			jwtHelper.Setup(j => j.HasCalendarPermission(It.IsAny<HttpContext>(), "read")).ReturnsAsync(true);

			var result = await controller.GetDayId("2024-01-01", 2);

			var ok = Assert.IsType<OkObjectResult>(result);
			ok.Value.Should().BeEquivalentTo(new { id = 99 });
		}

		[Fact]
		public async Task GetExistingDaysInRange_ShouldReturn403_WhenNoPermission()
		{
			var options = new DbContextOptionsBuilder<ShalendarDbContext>()
				.UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
			using var context = new ShalendarDbContext(options);

			var (controller, jwtHelper) = CreateDaysController(context);
			jwtHelper.Setup(j => j.HasCalendarPermission(It.IsAny<HttpContext>(), "read")).ReturnsAsync(false);

			var result = await controller.GetExistingDaysInRange("2024-01-01", "2024-01-05", 1);

			var forbidden = Assert.IsType<ObjectResult>(result);
			forbidden.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
		}

		[Fact]
		public async Task GetExistingDaysInRange_ShouldReturnBadRequest_WhenDateInvalid()
		{
			var options = new DbContextOptionsBuilder<ShalendarDbContext>()
				.UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
			using var context = new ShalendarDbContext(options);

			var (controller, jwtHelper) = CreateDaysController(context);
			jwtHelper.Setup(j => j.HasCalendarPermission(It.IsAny<HttpContext>(), "read")).ReturnsAsync(true);

			var result = await controller.GetExistingDaysInRange("bad", "2024-01-05", 1);

			Assert.IsType<BadRequestObjectResult>(result);
		}

		[Fact]
		public async Task GetExistingDaysInRange_ShouldReturnMatchingDays_WhenFound()
		{
			var options = new DbContextOptionsBuilder<ShalendarDbContext>()
				.UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
			using var context = new ShalendarDbContext(options);

			context.Days.AddRange(
				new Day { Id = 1, CalendarId = 1, Date = new DateTime(2024, 01, 02) },
				new Day { Id = 2, CalendarId = 1, Date = new DateTime(2024, 01, 04) },
				new Day { Id = 3, CalendarId = 2, Date = new DateTime(2024, 01, 03) }
			);
			await context.SaveChangesAsync();

			var (controller, jwtHelper) = CreateDaysController(context);
			jwtHelper.Setup(j => j.HasCalendarPermission(It.IsAny<HttpContext>(), "read")).ReturnsAsync(true);

			var result = await controller.GetExistingDaysInRange("2024-01-01", "2024-01-05", 1);

			var ok = Assert.IsType<OkObjectResult>(result);
			ok.Value.Should().BeEquivalentTo(new
			{
				days = new[]
				{
			new { Id = 1, Date = "2024-01-02" },
			new { Id = 2, Date = "2024-01-04" }
		}
			});
		}

		#endregion



		#region Posts

		[Fact]
		public async Task CreateDay_ShouldReturnBadRequest_WhenDateIsInvalid()
		{
			var options = new DbContextOptionsBuilder<ShalendarDbContext>()
				.UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
			using var context = new ShalendarDbContext(options);

			var (controller, _) = CreateDaysController(context);

			var request = new CreateDayDto { CalendarId = 1, Date = "invalid-date" };

			var result = await controller.CreateDay(request);

			var badRequest = Assert.IsType<BadRequestObjectResult>(result);
			badRequest.Value.Should().Be("Invalid date format.");
		}

		[Fact]
		public async Task CreateDay_ShouldReturnExistingDayId_IfAlreadyExists()
		{
			var options = new DbContextOptionsBuilder<ShalendarDbContext>()
				.UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
			using var context = new ShalendarDbContext(options);

			var existingDay = new Day { Id = 7, CalendarId = 1, Date = new DateTime(2024, 01, 01) };
			context.Days.Add(existingDay);
			await context.SaveChangesAsync();

			var (controller, _) = CreateDaysController(context);

			var request = new CreateDayDto { CalendarId = 1, Date = "2024-01-01" };

			var result = await controller.CreateDay(request);

			var ok = Assert.IsType<OkObjectResult>(result);
			ok.Value.Should().BeEquivalentTo(new { id = 7 });
		}

		[Fact]
		public async Task CreateDay_ShouldCreateDay_IfNotExists()
		{
			var options = new DbContextOptionsBuilder<ShalendarDbContext>()
				.UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
			using var context = new ShalendarDbContext(options);

			var (controller, _) = CreateDaysController(context);

			var request = new CreateDayDto { CalendarId = 2, Date = "2024-05-01" };

			var result = await controller.CreateDay(request);

			var ok = Assert.IsType<OkObjectResult>(result);
			var value = Assert.IsType<Dictionary<string, object>>(ok.Value!.GetType()
				.GetProperties()
				.ToDictionary(p => p.Name, p => p.GetValue(ok.Value)));
			value.Should().ContainKey("id");

			var dayInDb = context.Days.FirstOrDefault();
			dayInDb.Should().NotBeNull();
			dayInDb!.CalendarId.Should().Be(2);
			dayInDb.Date.Date.Should().Be(new DateTime(2024, 05, 01));
		}

		#endregion



		#region Puts
		#endregion



		#region Deletes

		[Fact]
		public async Task DeleteDayIfNoTickets_ShouldReturnBadRequest_WhenDateInvalid()
		{
			var options = new DbContextOptionsBuilder<ShalendarDbContext>()
				.UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
			using var context = new ShalendarDbContext(options);

			var (controller, _) = CreateDaysController(context);

			var result = await controller.DeleteDayIfNoTickets(1, "not-a-date");

			var badRequest = Assert.IsType<BadRequestObjectResult>(result);
			badRequest.Value.Should().Be("Invalid date format.");
		}

		[Fact]
		public async Task DeleteDayIfNoTickets_ShouldReturnNotFound_WhenDayNotExists()
		{
			var options = new DbContextOptionsBuilder<ShalendarDbContext>()
				.UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
			using var context = new ShalendarDbContext(options);

			var (controller, _) = CreateDaysController(context);

			var result = await controller.DeleteDayIfNoTickets(1, "2024-01-01");

			var notFound = Assert.IsType<NotFoundObjectResult>(result);
			notFound.Value.Should().Be("Day not found.");
		}

		[Fact]
		public async Task DeleteDayIfNoTickets_ShouldNotDelete_WhenTicketsExist()
		{
			var options = new DbContextOptionsBuilder<ShalendarDbContext>()
				.UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
			using var context = new ShalendarDbContext(options);

			var day = new Day { Id = 5, CalendarId = 1, Date = new DateTime(2024, 1, 1) };
			context.Days.Add(day);
			context.Tickets.Add(new Ticket
			{
				Id = 10,
				ParentId = 5,
				CurrentParentType = "TodoList",
				CalendarListId = 1,
				Name = "test"
			});
			await context.SaveChangesAsync();

			var (controller, _) = CreateDaysController(context);

			var result = await controller.DeleteDayIfNoTickets(1, "2024-01-01");

			var ok = Assert.IsType<OkObjectResult>(result);
			ok.Value.Should().Be("Day was not deleted as it still has tickets.");

			context.Days.Should().ContainSingle(d => d.Id == 5);
		}

		[Fact]
		public async Task DeleteDayIfNoTickets_ShouldDelete_WhenNoTickets()
		{
			var options = new DbContextOptionsBuilder<ShalendarDbContext>()
				.UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
			using var context = new ShalendarDbContext(options);

			var day = new Day { Id = 8, CalendarId = 2, Date = new DateTime(2024, 2, 2) };
			context.Days.Add(day);
			await context.SaveChangesAsync();

			var (controller, _) = CreateDaysController(context);

			var result = await controller.DeleteDayIfNoTickets(2, "2024-02-02");

			var ok = Assert.IsType<OkObjectResult>(result);
			ok.Value.Should().Be("Day deleted successfully.");

			context.Days.Should().BeEmpty();
		}

		#endregion
	}
}
