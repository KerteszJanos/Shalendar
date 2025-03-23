using Xunit;
using Moq;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Shalendar.Controllers;
using Shalendar.Models;
using Shalendar.Contexts;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using System.Linq;
using Shalendar.Functions.Interfaces;
using Shalendar.Services.Interfaces;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Shalendar.Tests.Unit.Controllers
{
    public class CalendarListsControllerTests
    {
        private (CalendarListsController controller,
                 Mock<IJwtHelper> jwtHelper,
                 Mock<IGetCalendarIdHelper> calendarIdHelper,
                 Mock<IGroupManagerService> groupManager,
                 Mock<IHubContext<CalendarHub>> hubContext)
            CreateCalendarListsController(ShalendarDbContext context)
        {
            var mockJwtHelper = new Mock<IJwtHelper>();
            var mockCalendarIdHelper = new Mock<IGetCalendarIdHelper>();
            var mockGroupManager = new Mock<IGroupManagerService>();
            var mockHubContext = new Mock<IHubContext<CalendarHub>>();

            var controller = new CalendarListsController(
                context,
                mockJwtHelper.Object,
                mockCalendarIdHelper.Object,
                mockGroupManager.Object,
                mockHubContext.Object
            );

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };

            return (controller, mockJwtHelper, mockCalendarIdHelper, mockGroupManager, mockHubContext);
        }

        #region Gets

        [Fact]
        public async Task GetCalendarListsByCalendarId_ShouldReturn403_WhenNoPermission()
        {
            var calendarId = 1;

            var options = new DbContextOptionsBuilder<ShalendarDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            using var context = new ShalendarDbContext(options);

            var (controller, MockJwtHelper, MockGetCalendarIdHelper, MockGroupManager, MockHubContext) = CreateCalendarListsController(context);

            MockJwtHelper
                .Setup(j => j.HasCalendarPermission(It.IsAny<HttpContext>(), "read"))
                .ReturnsAsync(false);

            // Act
            var result = await controller.GetCalendarListsByCalendarId(calendarId);

            // Assert
            var objectResult = Assert.IsType<ObjectResult>(result.Result);
            objectResult.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
        }


        [Fact]
        public async Task GetCalendarListsByCalendarId_ShouldReturnLists_WhenPermissionGranted()
        {
            var calendarId = 42;

            var options = new DbContextOptionsBuilder<ShalendarDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            using var context = new ShalendarDbContext(options);

            context.CalendarLists.Add(new CalendarList
            {
                Id = 1,
                Name = "Test List",
                Color = "blue",
                CalendarId = calendarId
            });

            context.SaveChanges();

            var (controller, MockJwtHelper, MockGetCalendarIdHelper, MockGroupManager, MockHubContext) = CreateCalendarListsController(context);

            MockJwtHelper.Setup(j => j.HasCalendarPermission(It.IsAny<HttpContext>(), "read"))
                          .ReturnsAsync(true);

            // Act
            var result = await controller.GetCalendarListsByCalendarId(calendarId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var list = Assert.IsAssignableFrom<IEnumerable<object>>(okResult.Value);
            list.Should().HaveCount(1);
        }

        #endregion



        #region Posts

        [Fact]
        public async Task PostCalendarList_ShouldCreateList_WhenPermissionGrantedAndCalendarExists()
        {
            var calendarId = 42;

            var options = new DbContextOptionsBuilder<ShalendarDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            using var context = new ShalendarDbContext(options);

            context.Calendars.Add(new Calendar { Id = calendarId, Name = "Test Calendar" });
            await context.SaveChangesAsync();

            var calendarList = new CalendarList
            {
                Id = 1,
                Name = "New List",
                Color = "green",
                CalendarId = calendarId
            };

            var (controller, MockJwtHelper, MockGetCalendarIdHelper, MockGroupManager, MockHubContext) = CreateCalendarListsController(context);

            MockJwtHelper.Setup(j => j.HasCalendarPermission(It.IsAny<HttpContext>(), "owner", calendarId))
                          .ReturnsAsync(true);

            MockGetCalendarIdHelper.Setup(h => h.TryGetCalendarId(It.IsAny<HttpContext>(), out calendarId))
                                  .Returns(true);

            MockGroupManager.Setup(m => m.IsUserAloneInGroup(calendarId.ToString()))
                             .Returns(true);

            // Act
            var result = await controller.PostCalendarList(calendarList);

            // Assert
            var createdAt = Assert.IsType<CreatedAtActionResult>(result.Result);
            var createdList = Assert.IsType<CalendarList>(createdAt.Value);

            createdList.Id.Should().Be(1);
            createdList.Name.Should().Be("New List");

            var fromDb = context.CalendarLists.FirstOrDefault(cl => cl.Id == 1);
            fromDb.Should().NotBeNull();
            fromDb.Name.Should().Be("New List");
        }

        [Fact]
        public async Task PostCalendarList_ShouldReturnBadRequest_WhenHeaderMissing()
        {
            var calendarId = 0;

            var options = new DbContextOptionsBuilder<ShalendarDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            using var context = new ShalendarDbContext(options);

            var calendarList = new CalendarList
            {
                Id = 1,
                Name = "New List",
                Color = "green",
                CalendarId = 123
            };

            var (controller, MockJwtHelper, MockGetCalendarIdHelper, MockGroupManager, MockHubContext) = CreateCalendarListsController(context);

            MockGetCalendarIdHelper.Setup(h => h.TryGetCalendarId(It.IsAny<HttpContext>(), out calendarId))
                                  .Returns(false);

            // Act
            var result = await controller.PostCalendarList(calendarList);

            // Assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
            badRequest.Value.Should().Be("Invalid or missing X-Calendar-Id header.");
        }

        [Fact]
        public async Task PostCalendarList_ShouldReturnForbidden_WhenPermissionMissing()
        {
            var calendarId = 101;

            var options = new DbContextOptionsBuilder<ShalendarDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            using var context = new ShalendarDbContext(options);

            var calendarList = new CalendarList
            {
                Id = 1,
                Name = "New List",
                Color = "green",
                CalendarId = calendarId
            };

            var (controller, MockJwtHelper, MockGetCalendarIdHelper, MockGroupManager, MockHubContext) = CreateCalendarListsController(context);

            MockJwtHelper.Setup(j => j.HasCalendarPermission(It.IsAny<HttpContext>(), "owner", calendarId))
                          .ReturnsAsync(false);

            MockGetCalendarIdHelper.Setup(h => h.TryGetCalendarId(It.IsAny<HttpContext>(), out calendarId))
                                  .Returns(true);

            // Act
            var result = await controller.PostCalendarList(calendarList);

            // Assert
            var objectResult = Assert.IsType<ObjectResult>(result.Result);
            objectResult.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
            objectResult.Value.Should().BeEquivalentTo(new { message = "Required permission: owner" });
        }

        [Fact]
        public async Task PostCalendarList_ShouldReturnBadRequest_WhenCalendarDoesNotExist()
        {
            var calendarId = 999;

            var options = new DbContextOptionsBuilder<ShalendarDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            using var context = new ShalendarDbContext(options);

            var calendarList = new CalendarList
            {
                Id = 1,
                Name = "New List",
                Color = "green",
                CalendarId = calendarId
            };

            var (controller, MockJwtHelper, MockGetCalendarIdHelper, MockGroupManager, MockHubContext) = CreateCalendarListsController(context);

            MockJwtHelper.Setup(j => j.HasCalendarPermission(It.IsAny<HttpContext>(), "owner", calendarId))
                          .ReturnsAsync(true);

            MockGetCalendarIdHelper.Setup(h => h.TryGetCalendarId(It.IsAny<HttpContext>(), out calendarId))
                                  .Returns(true);

            // Act
            var result = await controller.PostCalendarList(calendarList);

            // Assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
            badRequest.Value.Should().Be("The specified CalendarId does not exist.");
        }

        #endregion



        #region Puts

        [Fact]
        public async Task UpdateCalendarList_ShouldReturnBadRequest_WhenHeaderMissing()
        {
            var calendarId = 0;

            var options = new DbContextOptionsBuilder<ShalendarDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            using var context = new ShalendarDbContext(options);

            var updatedList = new CalendarList { Id = 1, Name = "Updated", Color = "red", CalendarId = 1 };

            var (controller, MockJwtHelper, MockGetCalendarIdHelper, MockGroupManager, MockHubContext) = CreateCalendarListsController(context);

            MockGetCalendarIdHelper.Setup(h => h.TryGetCalendarId(It.IsAny<HttpContext>(), out calendarId)).Returns(false);

            var result = await controller.UpdateCalendarList(1, updatedList);

            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            badRequest.Value.Should().Be("Invalid or missing X-Calendar-Id header.");
        }

        [Fact]
        public async Task UpdateCalendarList_ShouldReturnForbidden_WhenPermissionMissing()
        {
            var calendarId = 10;

            var options = new DbContextOptionsBuilder<ShalendarDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            using var context = new ShalendarDbContext(options);

            var updatedList = new CalendarList { Id = 1, Name = "Updated", Color = "red", CalendarId = calendarId };

            var (controller, MockJwtHelper, MockGetCalendarIdHelper, MockGroupManager, MockHubContext) = CreateCalendarListsController(context);

            MockGetCalendarIdHelper.Setup(h => h.TryGetCalendarId(It.IsAny<HttpContext>(), out calendarId)).Returns(true);
            MockJwtHelper.Setup(j => j.HasCalendarPermission(It.IsAny<HttpContext>(), "owner", calendarId)).ReturnsAsync(false);

            var result = await controller.UpdateCalendarList(1, updatedList);

            var forbidden = Assert.IsType<ObjectResult>(result);
            forbidden.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
            forbidden.Value.Should().BeEquivalentTo(new { message = "Required permission: owner" });
        }

        [Fact]
        public async Task UpdateCalendarList_ShouldReturnBadRequest_WhenIdMismatch()
        {
            var calendarId = 100;

            var options = new DbContextOptionsBuilder<ShalendarDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            using var context = new ShalendarDbContext(options);

            var updatedList = new CalendarList { Id = 99, Name = "Updated", Color = "red", CalendarId = calendarId };

            var (controller, MockJwtHelper, MockGetCalendarIdHelper, MockGroupManager, MockHubContext) = CreateCalendarListsController(context);

            MockGetCalendarIdHelper.Setup(h => h.TryGetCalendarId(It.IsAny<HttpContext>(), out calendarId)).Returns(true);
            MockJwtHelper.Setup(j => j.HasCalendarPermission(It.IsAny<HttpContext>(), "owner", calendarId)).ReturnsAsync(true);

            var result = await controller.UpdateCalendarList(1, updatedList);

            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            badRequest.Value.Should().Be("ID mismatch.");
        }

        [Fact]
        public async Task UpdateCalendarList_ShouldReturnNotFound_WhenListDoesNotExist()
        {
            var calendarId = 1000;

            var options = new DbContextOptionsBuilder<ShalendarDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            using var context = new ShalendarDbContext(options);

            var updatedList = new CalendarList { Id = 1, Name = "Updated", Color = "red", CalendarId = calendarId };

            var (controller, MockJwtHelper, MockGetCalendarIdHelper, MockGroupManager, MockHubContext) = CreateCalendarListsController(context);

            MockGetCalendarIdHelper.Setup(h => h.TryGetCalendarId(It.IsAny<HttpContext>(), out calendarId)).Returns(true);
            MockJwtHelper.Setup(j => j.HasCalendarPermission(It.IsAny<HttpContext>(), "owner", calendarId)).ReturnsAsync(true);

            var result = await controller.UpdateCalendarList(1, updatedList);

            var notFound = Assert.IsType<NotFoundObjectResult>(result);
            notFound.Value.Should().Be("Calendar list not found.");
        }

        [Fact]
        public async Task UpdateCalendarList_ShouldReturnNoContent_WhenUpdateSuccessful()
        {
            var calendarId = 200;

            var options = new DbContextOptionsBuilder<ShalendarDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            using var context = new ShalendarDbContext(options);

            context.CalendarLists.Add(new CalendarList { Id = 1, Name = "Old", Color = "blue", CalendarId = calendarId });
            await context.SaveChangesAsync();

            var updatedList = new CalendarList { Id = 1, Name = "Updated", Color = "red", CalendarId = calendarId };

            var (controller, MockJwtHelper, MockGetCalendarIdHelper, MockGroupManager, MockHubContext) = CreateCalendarListsController(context);

            MockGetCalendarIdHelper.Setup(h => h.TryGetCalendarId(It.IsAny<HttpContext>(), out calendarId)).Returns(true);
            MockJwtHelper.Setup(j => j.HasCalendarPermission(It.IsAny<HttpContext>(), "owner", calendarId)).ReturnsAsync(true);
            MockGroupManager.Setup(m => m.IsUserAloneInGroup(calendarId.ToString())).Returns(true);

            var result = await controller.UpdateCalendarList(1, updatedList);

            Assert.IsType<NoContentResult>(result);

            var fromDb = context.CalendarLists.FirstOrDefault(cl => cl.Id == 1);
            fromDb.Should().NotBeNull();
            fromDb!.Name.Should().Be("Updated");
            fromDb.Color.Should().Be("red");
        }

        #endregion



        #region Deletes

        [Fact]
        public async Task DeleteCalendarList_ShouldReturnBadRequest_WhenHeaderMissing()
        {
            var calendarId = 0;

            var options = new DbContextOptionsBuilder<ShalendarDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
            using var context = new ShalendarDbContext(options);

            var (controller, MockJwtHelper, MockGetCalendarIdHelper, MockGroupManager, MockHubContext) = CreateCalendarListsController(context);

            MockGetCalendarIdHelper.Setup(h => h.TryGetCalendarId(It.IsAny<HttpContext>(), out calendarId)).Returns(false);

            var result = await controller.DeleteCalendarList(1);

            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            badRequest.Value.Should().Be("Invalid or missing X-Calendar-Id header.");
        }

        [Fact]
        public async Task DeleteCalendarList_ShouldReturnForbidden_WhenPermissionMissing()
        {
            var calendarId = 10;

            var options = new DbContextOptionsBuilder<ShalendarDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
            using var context = new ShalendarDbContext(options);

            var (controller, MockJwtHelper, MockGetCalendarIdHelper, MockGroupManager, MockHubContext) = CreateCalendarListsController(context);

            MockGetCalendarIdHelper.Setup(h => h.TryGetCalendarId(It.IsAny<HttpContext>(), out calendarId)).Returns(true);
            MockJwtHelper.Setup(j => j.HasCalendarPermission(It.IsAny<HttpContext>(), "owner", calendarId)).ReturnsAsync(false);

            var result = await controller.DeleteCalendarList(1);

            var forbidden = Assert.IsType<ObjectResult>(result);
            forbidden.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
            forbidden.Value.Should().BeEquivalentTo(new { message = "Required permission: owner" });
        }

        [Fact]
        public async Task DeleteCalendarList_ShouldReturnNotFound_WhenListDoesNotExist()
        {
            var calendarId = 123;

            var options = new DbContextOptionsBuilder<ShalendarDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString()).ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning)).Options;
            using var context = new ShalendarDbContext(options);

            var (controller, MockJwtHelper, MockGetCalendarIdHelper, MockGroupManager, MockHubContext) = CreateCalendarListsController(context); ;

            MockGetCalendarIdHelper.Setup(h => h.TryGetCalendarId(It.IsAny<HttpContext>(), out calendarId)).Returns(true);
            MockJwtHelper.Setup(j => j.HasCalendarPermission(It.IsAny<HttpContext>(), "owner", calendarId)).ReturnsAsync(true);

            var result = await controller.DeleteCalendarList(1);

            var notFound = Assert.IsType<NotFoundObjectResult>(result);
            notFound.Value.Should().Be("Calendar list not found.");
        }

        [Fact]
        public async Task DeleteCalendarList_ShouldDeleteListAndTickets_WhenTheyExist()
        {
            var calendarId = 99;
            var listId = 1;

            var options = new DbContextOptionsBuilder<ShalendarDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString()).ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning)).Options;
            using var context = new ShalendarDbContext(options);

            context.Calendars.Add(new Calendar { Id = calendarId, Name = "Test" });
            context.CalendarLists.Add(new CalendarList { Id = listId, Name = "List", Color = "gray", CalendarId = calendarId });
            context.Tickets.Add(new Ticket { Id = 1, CalendarListId = listId, Name = "Ticket", CurrentParentType = "CalendarList", ParentId = listId });
            await context.SaveChangesAsync();

            var (controller, MockJwtHelper, MockGetCalendarIdHelper, MockGroupManager, MockHubContext) = CreateCalendarListsController(context);

            MockGetCalendarIdHelper.Setup(h => h.TryGetCalendarId(It.IsAny<HttpContext>(), out calendarId)).Returns(true);
            MockJwtHelper.Setup(j => j.HasCalendarPermission(It.IsAny<HttpContext>(), "owner", calendarId)).ReturnsAsync(true);
            MockGroupManager.Setup(m => m.IsUserAloneInGroup(calendarId.ToString())).Returns(true);

            var result = await controller.DeleteCalendarList(listId);

            Assert.IsType<NoContentResult>(result);
            context.CalendarLists.Any(cl => cl.Id == listId).Should().BeFalse();
            context.Tickets.Any().Should().BeFalse();
        }

        #endregion
    }
}