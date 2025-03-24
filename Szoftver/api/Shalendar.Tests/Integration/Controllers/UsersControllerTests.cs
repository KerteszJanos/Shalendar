using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shalendar.Models;
using Shalendar.Tests.Integration.Helpers;
using Xunit;

namespace Shalendar.Tests.Integration.Controllers
{
	public class UsersControllerTests : IClassFixture<CustomWebApplicationFactory>
	{
		private readonly HttpClient _client;
		private readonly CustomWebApplicationFactory _factory;

		public UsersControllerTests(CustomWebApplicationFactory factory)
		{
			_factory = factory;
			_client = factory.CreateClient();
		}

		public static IEnumerable<object[]> usersControllerEndpoints => new List<object[]>
		{
			new object[] { HttpMethod.Get, "/api/Users/me" },
			new object[] { HttpMethod.Put, "/api/Users/change-password" },
			new object[] { HttpMethod.Put, "/api/Users/set-default-calendar/1" },
			new object[] { HttpMethod.Delete, "/api/Users/delete" }
		};

		[Theory]
		[MemberData(nameof(usersControllerEndpoints))]
		public async Task Endpoint_ShouldReturn401_WhenTokenIsInvalid(HttpMethod method, string url)
		{
			var request = new HttpRequestMessage(method, url);
			request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "invalid.token.here");

			if (method == HttpMethod.Put || method == HttpMethod.Post)
			{
				request.Content = new StringContent("{}", Encoding.UTF8, "application/json");
			}

			var response = await _client.SendAsync(request);

			Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
		}

		#region Gets

		[Fact]
		public async Task GetCurrentUser_ShouldReturnUserData_WhenAuthorized()
		{
			// Arrange
			using var scope = _factory.Services.CreateScope();
			var context = scope.ServiceProvider.GetRequiredService<Shalendar.Contexts.ShalendarDbContext>();

			var user = new User
			{
				Email = "me@example.com",
				Username = "meuser",
				PasswordHash = "pw",
				DefaultCalendarId = null
			};

			context.Users.Add(user);
			await context.SaveChangesAsync();

			var token = JwtTokenGenerator.GenerateToken(user, Array.Empty<CalendarPermission>());

			var request = new HttpRequestMessage(HttpMethod.Get, "/api/Users/me");
			request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

			// Act
			var response = await _client.SendAsync(request);

			// Assert
			Assert.Equal(HttpStatusCode.OK, response.StatusCode);
			var result = await response.Content.ReadFromJsonAsync<JsonElement>();
			Assert.Equal(user.Id, result.GetProperty("userId").GetInt32());
			Assert.Equal(user.Username, result.GetProperty("username").GetString());
			Assert.Equal(user.Email, result.GetProperty("email").GetString());
		}

		#endregion



		#region Posts

		[Fact]
		public async Task PostUser_ShouldCreateUserSuccessfully()
		{
			// Arrange
			using var scope = _factory.Services.CreateScope();
			var context = scope.ServiceProvider.GetRequiredService<Shalendar.Contexts.ShalendarDbContext>();

			var newUser = new User
			{
				Email = "newuser@example.com",
				Username = "newuser",
				PasswordHash = "SecurePass1"
			};

			var request = new HttpRequestMessage(HttpMethod.Post, "/api/Users");
			request.Content = JsonContent.Create(newUser);

			// Act
			var response = await _client.SendAsync(request);

			// Assert
			Assert.Equal(HttpStatusCode.OK, response.StatusCode);

			using var verifyScope = _factory.Services.CreateScope();
			var verifyContext = verifyScope.ServiceProvider.GetRequiredService<Shalendar.Contexts.ShalendarDbContext>();
			var user = await verifyContext.Users.FirstOrDefaultAsync(u => u.Email == newUser.Email);
			Assert.NotNull(user);
			Assert.Equal("newuser", user!.Username);
			Assert.NotNull(await verifyContext.Calendars.FindAsync(user.DefaultCalendarId));
		}

		[Fact]
		public async Task LoginUser_ShouldReturnTokenAndUserData_WhenCredentialsAreValid()
		{
			// Arrange
			using var scope = _factory.Services.CreateScope();
			var context = scope.ServiceProvider.GetRequiredService<Shalendar.Contexts.ShalendarDbContext>();

			var user = new User
			{
				Email = "login@example.com",
				Username = "loginuser",
				PasswordHash = "OriginalPass1"
			};

			var hasher = new PasswordHasher<User>();
			user.PasswordHash = hasher.HashPassword(user, user.PasswordHash);

			context.Users.Add(user);
			await context.SaveChangesAsync();

			var loginDto = new
			{
				Email = "login@example.com",
				Password = "OriginalPass1"
			};

			var request = new HttpRequestMessage(HttpMethod.Post, "/api/Users/login");
			request.Content = JsonContent.Create(loginDto);

			// Act
			var response = await _client.SendAsync(request);

			// Assert
			Assert.Equal(HttpStatusCode.OK, response.StatusCode);

			var result = await response.Content.ReadFromJsonAsync<JsonElement>();
			Assert.True(result.TryGetProperty("token", out var token));
			Assert.False(string.IsNullOrWhiteSpace(token.GetString()));

			var userObj = result.GetProperty("user");
			Assert.Equal(user.Id, userObj.GetProperty("userId").GetInt32());
			Assert.Equal(user.Username, userObj.GetProperty("username").GetString());
			Assert.Equal(user.Email, userObj.GetProperty("email").GetString());
		}

		#endregion



		#region Puts

		[Fact]
		public async Task ChangePassword_ShouldUpdatePassword_WhenCurrentPasswordIsCorrect()
		{
			// Arrange
			using var scope = _factory.Services.CreateScope();
			var context = scope.ServiceProvider.GetRequiredService<Shalendar.Contexts.ShalendarDbContext>();

			var user = new User
			{
				Email = "changepass@example.com",
				Username = "changepass",
				PasswordHash = "OldPass123"
			};

			var hasher = new PasswordHasher<User>();
			user.PasswordHash = hasher.HashPassword(user, user.PasswordHash);

			context.Users.Add(user);
			await context.SaveChangesAsync();

			var token = JwtTokenGenerator.GenerateToken(user, Array.Empty<CalendarPermission>());

			var payload = new
			{
				OldPassword = "OldPass123",
				NewPassword = "NewPass123"
			};

			var request = new HttpRequestMessage(HttpMethod.Put, "/api/Users/change-password");
			request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
			request.Content = JsonContent.Create(payload);

			// Act
			var response = await _client.SendAsync(request);

			// Assert
			Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

			using var verifyScope = _factory.Services.CreateScope();
			var verifyContext = verifyScope.ServiceProvider.GetRequiredService<Shalendar.Contexts.ShalendarDbContext>();
			var updatedUser = await verifyContext.Users.FindAsync(user.Id);

			var result = hasher.VerifyHashedPassword(updatedUser!, updatedUser!.PasswordHash, "NewPass123");
			Assert.Equal(PasswordVerificationResult.Success, result);
		}

		[Fact]
		public async Task SetDefaultCalendar_ShouldUpdateDefaultCalendar_WhenUserIsAuthorized()
		{
			// Arrange
			using var scope = _factory.Services.CreateScope();
			var context = scope.ServiceProvider.GetRequiredService<Shalendar.Contexts.ShalendarDbContext>();

			var calendar = new Calendar { Name = "Selectable" };
			context.Calendars.Add(calendar);
			await context.SaveChangesAsync();

			var user = new User
			{
				Email = "defaultcal@example.com",
				Username = "defaultuser",
				PasswordHash = "pw",
				DefaultCalendarId = null
			};

			context.Users.Add(user);
			await context.SaveChangesAsync();

			var token = JwtTokenGenerator.GenerateToken(user, Array.Empty<CalendarPermission>());

			var request = new HttpRequestMessage(HttpMethod.Put, $"/api/Users/set-default-calendar/{calendar.Id}");
			request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

			// Act
			var response = await _client.SendAsync(request);

			// Assert
			Assert.Equal(HttpStatusCode.OK, response.StatusCode);

			using var verifyScope = _factory.Services.CreateScope();
			var verifyContext = verifyScope.ServiceProvider.GetRequiredService<Shalendar.Contexts.ShalendarDbContext>();
			var updatedUser = await verifyContext.Users.FindAsync(user.Id);
			Assert.Equal(calendar.Id, updatedUser!.DefaultCalendarId);
		}

		#endregion



		#region Deletes

		[Fact]
		public async Task DeleteCurrentUser_ShouldRemoveUser_WhenAuthorized()
		{
			// Arrange
			using var scope = _factory.Services.CreateScope();
			var context = scope.ServiceProvider.GetRequiredService<Shalendar.Contexts.ShalendarDbContext>();

			var user = new User
			{
				Email = "delete@example.com",
				Username = "tobedeleted",
				PasswordHash = "pw"
			};

			context.Users.Add(user);
			await context.SaveChangesAsync();

			var calendar = new Calendar { Name = "Unused" };
			context.Calendars.Add(calendar);
			await context.SaveChangesAsync();

			var permission = new CalendarPermission
			{
				UserId = user.Id,
				CalendarId = calendar.Id,
				PermissionType = "owner"
			};

			context.CalendarPermissions.Add(permission);
			await context.SaveChangesAsync();

			var token = JwtTokenGenerator.GenerateToken(user, new[] { permission });

			var request = new HttpRequestMessage(HttpMethod.Delete, "/api/Users/delete");
			request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

			// Act
			var response = await _client.SendAsync(request);

			// Assert
			Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

			using var verifyScope = _factory.Services.CreateScope();
			var verifyContext = verifyScope.ServiceProvider.GetRequiredService<Shalendar.Contexts.ShalendarDbContext>();
			var deleted = await verifyContext.Users.FindAsync(user.Id);
			Assert.Null(deleted);
		}

		#endregion
	}
}