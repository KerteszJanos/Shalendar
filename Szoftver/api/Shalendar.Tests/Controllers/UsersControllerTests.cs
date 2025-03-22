using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.SignalR;
using Moq;
using Shalendar.Controllers;
using Xunit;
using Shalendar.Contexts;
using Shalendar.Functions.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Shalendar.Models.Dtos;
using Shalendar.Models;

namespace Shalendar.Tests.Controllers
{
	public class UsersControllerTests
	{
		private (UsersController controller,
				 Mock<IJwtHelper> jwtHelper,
				 Mock<IConfiguration> configuration,
				 Mock<IDeleteCalendarHelper> deleteCalendarHelper)
			CreateUsersController(ShalendarDbContext context)
		{
			var mockJwtHelper = new Mock<IJwtHelper>();
			var mockConfiguration = new Mock<IConfiguration>();
			var mockDeleteCalendarHelper = new Mock<IDeleteCalendarHelper>();

			var controller = new UsersController(
				context,
				mockJwtHelper.Object,
				mockConfiguration.Object,
				mockDeleteCalendarHelper.Object
			);

			controller.ControllerContext = new ControllerContext
			{
				HttpContext = new DefaultHttpContext()
			};

			return (controller, mockJwtHelper, mockConfiguration, mockDeleteCalendarHelper);
		}

		#region Gets

		[Fact]
		public async Task GetCurrentUser_ShouldReturn404_WhenUserNotFound()
		{
			var options = new DbContextOptionsBuilder<ShalendarDbContext>()
				.UseInMemoryDatabase(Guid.NewGuid().ToString())
				.Options;

			using var context = new ShalendarDbContext(options);

			var (controller, mockJwtHelper, mockConfig, mockDeleteHelper) = CreateUsersController(context);

			var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
			{
		new Claim(ClaimTypes.NameIdentifier, "1")
			}, "mock"));

			controller.ControllerContext.HttpContext.User = user;

			// Act
			var result = await controller.GetCurrentUser();

			// Assert
			Assert.IsType<NotFoundResult>(result.Result);
		}

		[Fact]
		public async Task GetCurrentUser_ShouldReturnUserData_WhenUserExists()
		{
			//Arrange
			var options = new DbContextOptionsBuilder<ShalendarDbContext>()
				.UseInMemoryDatabase(Guid.NewGuid().ToString())
				.Options;

			using var context = new ShalendarDbContext(options);

			context.Users.Add(new User
			{
				Id = 42,
				Username = "testuser",
				Email = "test@example.com",
				DefaultCalendarId = 7
			});
			context.SaveChanges();

			var (controller, mockJwtHelper, mockConfig, mockDeleteHelper) = CreateUsersController(context);

			var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
			{
		new Claim(ClaimTypes.NameIdentifier, "42")
			}, "mock"));

			controller.ControllerContext.HttpContext.User = user;

			// Act
			var result = await controller.GetCurrentUser();

			// Assert
			var okResult = Assert.IsType<OkObjectResult>(result.Result);

			var json = JsonSerializer.Serialize(okResult.Value);
			var parsed = JsonSerializer.Deserialize<JsonElement>(json);

			Assert.Equal(42, parsed.GetProperty("userId").GetInt32());
			Assert.Equal("testuser", parsed.GetProperty("username").GetString());
			Assert.Equal("test@example.com", parsed.GetProperty("email").GetString());
			Assert.Equal(7, parsed.GetProperty("defaultCalendarId").GetInt32());
		}

		#endregion


		#region Posts

		[Fact]
		public async Task PostUser_ShouldReturnBadRequest_WhenEmailAlreadyExists()
		{
			var options = new DbContextOptionsBuilder<ShalendarDbContext>()
				.UseInMemoryDatabase(Guid.NewGuid().ToString())
				.Options;

			using var context = new ShalendarDbContext(options);

			context.Users.Add(new User
			{
				Email = "existing@example.com",
				Username = "existinguser",
				PasswordHash = "hashedpassword"
			});
			context.SaveChanges();

			var (controller, _, _, _) = CreateUsersController(context);

			var newUser = new User
			{
				Email = "existing@example.com",
				Username = "newuser",
				PasswordHash = "newpassword"
			};

			var result = await controller.PostUser(newUser);

			var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
			Assert.Equal("An account with this email already exists.", badRequest.Value);
		}

		[Fact]
		public async Task LoginUser_ShouldReturnUnauthorized_WhenEmailNotRegistered()
		{
			var options = new DbContextOptionsBuilder<ShalendarDbContext>()
				.UseInMemoryDatabase(Guid.NewGuid().ToString())
				.Options;

			using var context = new ShalendarDbContext(options);
			var (controller, _, _, _) = CreateUsersController(context);

			var login = new LoginModelDto
			{
				Email = "notfound@example.com",
				Password = "any"
			};

			var result = await controller.LoginUser(login);

			var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result);
			Assert.Equal("This email address is not registered.", unauthorized.Value);
		}

		[Fact]
		public async Task LoginUser_ShouldReturnUnauthorized_WhenPasswordIsInvalid()
		{
			var options = new DbContextOptionsBuilder<ShalendarDbContext>()
				.UseInMemoryDatabase(Guid.NewGuid().ToString())
				.Options;

			using var context = new ShalendarDbContext(options);

			var passwordHasher = new PasswordHasher<User>();
			var user = new User
			{
				Email = "valid@example.com",
				Username = "validuser",
				PasswordHash = passwordHasher.HashPassword(null!, "correctpassword")
			};

			context.Users.Add(user);
			context.SaveChanges();

			var (controller, _, _, _) = CreateUsersController(context);

			var login = new LoginModelDto
			{
				Email = "valid@example.com",
				Password = "wrongpassword"
			};

			var result = await controller.LoginUser(login);

			var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result);
			Assert.Equal("Invalid password.", unauthorized.Value);
		}

		#endregion


		#region Puts

		[Fact]
		public async Task ChangePassword_ShouldReturnNotFound_WhenUserNotFound()
		{
			var options = new DbContextOptionsBuilder<ShalendarDbContext>()
				.UseInMemoryDatabase(Guid.NewGuid().ToString())
				.Options;

			using var context = new ShalendarDbContext(options);
			var (controller, _, _, _) = CreateUsersController(context);

			var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
			{
		new Claim(ClaimTypes.NameIdentifier, "1")
	}, "mock"));

			controller.ControllerContext.HttpContext.User = user;

			var model = new ChangePasswordDto
			{
				OldPassword = "old",
				NewPassword = "new"
			};

			var result = await controller.ChangePassword(model);

			Assert.IsType<NotFoundResult>(result);
		}

		[Fact]
		public async Task ChangePassword_ShouldReturnBadRequest_WhenOldPasswordIncorrect()
		{
			var options = new DbContextOptionsBuilder<ShalendarDbContext>()
				.UseInMemoryDatabase(Guid.NewGuid().ToString())
				.Options;

			using var context = new ShalendarDbContext(options);

			var passwordHasher = new PasswordHasher<User>();
			var user = new User
			{
				Id = 1,
				Email = "test@example.com",
				Username = "user",
				PasswordHash = passwordHasher.HashPassword(null!, "correctOldPassword")
			};

			context.Users.Add(user);
			context.SaveChanges();

			var (controller, _, _, _) = CreateUsersController(context);

			var claimsUser = new ClaimsPrincipal(new ClaimsIdentity(new[]
			{
		new Claim(ClaimTypes.NameIdentifier, "1")
	}, "mock"));

			controller.ControllerContext.HttpContext.User = claimsUser;

			var model = new ChangePasswordDto
			{
				OldPassword = "wrongPassword",
				NewPassword = "newSecurePassword123"
			};

			var result = await controller.ChangePassword(model);

			var badRequest = Assert.IsType<BadRequestObjectResult>(result);
			Assert.Equal("Incorrect old password.", badRequest.Value);
		}

		[Fact]
		public async Task SetDefaultCalendar_ShouldReturnNotFound_WhenUserNotFound()
		{
			var options = new DbContextOptionsBuilder<ShalendarDbContext>()
				.UseInMemoryDatabase(Guid.NewGuid().ToString())
				.Options;

			using var context = new ShalendarDbContext(options);
			var (controller, _, _, _) = CreateUsersController(context);

			var claimsUser = new ClaimsPrincipal(new ClaimsIdentity(new[]
			{
		new Claim(ClaimTypes.NameIdentifier, "1")
	}, "mock"));

			controller.ControllerContext.HttpContext.User = claimsUser;

			var result = await controller.SetDefaultCalendar(5);

			var notFound = Assert.IsType<NotFoundObjectResult>(result);

			var json = JsonSerializer.Serialize(notFound.Value);
			var parsed = JsonSerializer.Deserialize<JsonElement>(json);

			Assert.Equal("User not found.", parsed.GetProperty("message").GetString());
		}


		[Fact]
		public async Task SetDefaultCalendar_ShouldUpdateDefaultCalendar_WhenUserExists()
		{
			var options = new DbContextOptionsBuilder<ShalendarDbContext>()
				.UseInMemoryDatabase(Guid.NewGuid().ToString())
				.Options;

			using var context = new ShalendarDbContext(options);

			var user = new User
			{
				Id = 42,
				Username = "test",
				Email = "email@example.com",
				PasswordHash = "hashed",
				DefaultCalendarId = 1
			};

			context.Users.Add(user);
			context.SaveChanges();

			var (controller, _, _, _) = CreateUsersController(context);

			var claimsUser = new ClaimsPrincipal(new ClaimsIdentity(new[]
			{
		new Claim(ClaimTypes.NameIdentifier, "42")
	}, "mock"));

			controller.ControllerContext.HttpContext.User = claimsUser;

			var result = await controller.SetDefaultCalendar(99);

			var ok = Assert.IsType<OkObjectResult>(result);

			var json = JsonSerializer.Serialize(ok.Value);
			var parsed = JsonSerializer.Deserialize<JsonElement>(json);

			Assert.Equal("Default calendar updated successfully.", parsed.GetProperty("message").GetString());

			var updatedUser = await context.Users.FindAsync(42);
			Assert.Equal(99, updatedUser.DefaultCalendarId);
		}


		#endregion


		#region Deletes

		[Fact]
		public async Task DeleteCurrentUser_ShouldReturnUnauthorized_WhenUserNotAuthenticated()
		{
			var options = new DbContextOptionsBuilder<ShalendarDbContext>()
				.UseInMemoryDatabase(Guid.NewGuid().ToString())
				.Options;

			using var context = new ShalendarDbContext(options);
			var (controller, _, _, _) = CreateUsersController(context);

			controller.ControllerContext.HttpContext.User = new ClaimsPrincipal();

			var result = await controller.DeleteCurrentUser();

			var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result);

			var json = JsonSerializer.Serialize(unauthorized.Value);
			var parsed = JsonSerializer.Deserialize<JsonElement>(json);

			Assert.Equal("User not authenticated.", parsed.GetProperty("message").GetString());
		}

		[Fact]
		public async Task DeleteCurrentUser_ShouldReturnUnauthorized_WhenUserIdIsNotInteger()
		{
			var options = new DbContextOptionsBuilder<ShalendarDbContext>()
				.UseInMemoryDatabase(Guid.NewGuid().ToString())
				.Options;

			using var context = new ShalendarDbContext(options);
			var (controller, _, _, _) = CreateUsersController(context);

			var claimsUser = new ClaimsPrincipal(new ClaimsIdentity(new[]
			{
		new Claim(ClaimTypes.NameIdentifier, "not-an-integer")
	}, "mock"));

			controller.ControllerContext.HttpContext.User = claimsUser;

			var result = await controller.DeleteCurrentUser();

			var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result);

			var json = JsonSerializer.Serialize(unauthorized.Value);
			var parsed = JsonSerializer.Deserialize<JsonElement>(json);

			Assert.Equal("User not authenticated.", parsed.GetProperty("message").GetString());
		}

		[Fact]
		public async Task DeleteCurrentUser_ShouldDeleteUserAndCalendars_WhenValidUser()
		{
			var options = new DbContextOptionsBuilder<ShalendarDbContext>()
				.UseInMemoryDatabase(Guid.NewGuid().ToString())
				.Options;

			using var context = new ShalendarDbContext(options);

			var user = new User
			{
				Id = 10,
				Username = "test",
				Email = "test@example.com",
				PasswordHash = "pw"
			};

			var permission = new CalendarPermission
			{
				UserId = 10,
				CalendarId = 123,
				PermissionType = "owner"
			};

			context.Users.Add(user);
			context.CalendarPermissions.Add(permission);
			context.SaveChanges();

			var mockDeleteHelper = new Mock<IDeleteCalendarHelper>();
			mockDeleteHelper.Setup(h => h.ShouldDeleteCalendar(10, 123)).ReturnsAsync(true);
			mockDeleteHelper.Setup(h => h.DeleteCalendar(123)).Returns(Task.CompletedTask);

			var mockJwtHelper = new Mock<IJwtHelper>();
			var mockConfig = new Mock<IConfiguration>();

			var controller = new UsersController(context, mockJwtHelper.Object, mockConfig.Object, mockDeleteHelper.Object);

			controller.ControllerContext = new ControllerContext
			{
				HttpContext = new DefaultHttpContext
				{
					User = new ClaimsPrincipal(new ClaimsIdentity(new[]
					{
				new Claim(ClaimTypes.NameIdentifier, "10")
			}, "mock"))
				}
			};

			var result = await controller.DeleteCurrentUser();

			Assert.IsType<NoContentResult>(result);

			Assert.Null(await context.Users.FindAsync(10));

			mockDeleteHelper.Verify(h => h.ShouldDeleteCalendar(10, 123), Times.Once);
			mockDeleteHelper.Verify(h => h.DeleteCalendar(123), Times.Once);
		}

		#endregion

	}
}
