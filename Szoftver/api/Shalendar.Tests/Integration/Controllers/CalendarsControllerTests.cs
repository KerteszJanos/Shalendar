using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shalendar.Models;
using Shalendar.Tests.Integration.Helpers;
using Xunit;

namespace Shalendar.Tests.Integration.Controllers
{
	public class CalendarsControllerTests : IClassFixture<CustomWebApplicationFactory>
	{
		private readonly HttpClient _client;
		private readonly CustomWebApplicationFactory _factory;

		public CalendarsControllerTests(CustomWebApplicationFactory factory)
		{
			_factory = factory;
			_client = factory.CreateClient();
		}

		public static IEnumerable<object[]> calendarsControllerEndpoints => new List<object[]>
		{
			new object[] { HttpMethod.Get, "/api/Calendars/1" },
			new object[] { HttpMethod.Get, "/api/Calendars/1/permissions" },
			new object[] { HttpMethod.Get, "/api/Calendars/accessible" },
			new object[] { HttpMethod.Get, "/api/Calendars/user/1" },

			new object[] { HttpMethod.Post, "/api/Calendars?userId=1" },
			new object[] { HttpMethod.Post, "/api/Calendars/1/permissions/test@example.com/write" },
			new object[] { HttpMethod.Post, "/api/Calendars/copy-all-tickets?calendarId=1" },

			new object[] { HttpMethod.Delete, "/api/Calendars/1/permissions/test@example.com" },
			new object[] { HttpMethod.Delete, "/api/Calendars/1" },
		};

		[Theory]
		[MemberData(nameof(calendarsControllerEndpoints))]
		public async Task Endpoint_ShouldReturn401_WhenTokenIsInvalid(HttpMethod method, string url)
		{
			var request = new HttpRequestMessage(method, url);
			request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "invalid.token.here");

			var response = await _client.SendAsync(request);

			Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
		}

		#region Gets

		[Fact]
		public async Task GetCalendar_ShouldReturn403Forbidden_WhenUserHasNoPermission()
		{
			// Arrange
			using var scope = _factory.Services.CreateScope();
			var context = scope.ServiceProvider.GetRequiredService<Shalendar.Contexts.ShalendarDbContext>();

			var user = new User { Email = "email@example.com", Username = "user", PasswordHash = "pw" };
			var calendar = new Calendar { Name = "Test Calendar" };

			context.Users.Add(user);
			context.Calendars.Add(calendar);
			await context.SaveChangesAsync();

			var token = JwtTokenGenerator.GenerateToken(user, Array.Empty<CalendarPermission>());

			var request = new HttpRequestMessage(HttpMethod.Get, $"/api/Calendars/{calendar.Id}");
			request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
			request.Headers.Add("X-Calendar-Id", calendar.Id.ToString());

			// Act
			var response = await _client.SendAsync(request);

			// Assert
			Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
		}

		[Fact]
		public async Task GetCalendar_ShouldReturn200_WhenUserHasPermission()
		{
			// Arrange
			using var scope = _factory.Services.CreateScope();
			var context = scope.ServiceProvider.GetRequiredService<Shalendar.Contexts.ShalendarDbContext>();

			var user = new User { Email = "email@example.com", Username = "user", PasswordHash = "pw" };
			var calendar = new Calendar { Name = "Test Calendar" };

			context.Users.Add(user);
			context.Calendars.Add(calendar);
			await context.SaveChangesAsync();

			var permission = new CalendarPermission
			{
				UserId = user.Id,
				CalendarId = calendar.Id,
				PermissionType = "read"
			};

			context.CalendarPermissions.Add(permission);
			await context.SaveChangesAsync();

			var token = JwtTokenGenerator.GenerateToken(user, new[] { permission });

			var request = new HttpRequestMessage(HttpMethod.Get, $"/api/Calendars/{calendar.Id}");
			request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
			request.Headers.Add("X-Calendar-Id", calendar.Id.ToString());

			// Act
			var response = await _client.SendAsync(request);

			// Assert
			Assert.Equal(HttpStatusCode.OK, response.StatusCode);

			var returnedCalendar = await response.Content.ReadFromJsonAsync<Calendar>();
			Assert.NotNull(returnedCalendar);
			Assert.Equal(calendar.Id, returnedCalendar?.Id);
			Assert.Equal(calendar.Name, returnedCalendar?.Name);
		}

		[Fact]
		public async Task GetCalendarNoPermissionNeeded_ShouldReturn200_WhenUserIsAuthenticated()
		{
			// Arrange
			using var scope = _factory.Services.CreateScope();
			var context = scope.ServiceProvider.GetRequiredService<Shalendar.Contexts.ShalendarDbContext>();

			var user = new User { Email = "email@example.com", Username = "user", PasswordHash = "pw" };
			var calendar = new Calendar { Name = "Public Calendar" };

			context.Users.Add(user);
			context.Calendars.Add(calendar);
			await context.SaveChangesAsync();

			var token = JwtTokenGenerator.GenerateToken(user, Array.Empty<CalendarPermission>());

			var request = new HttpRequestMessage(HttpMethod.Get, $"/api/Calendars/noPermissionNeeded/{calendar.Id}");
			request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

			// Act
			var response = await _client.SendAsync(request);

			// Assert
			Assert.Equal(HttpStatusCode.OK, response.StatusCode);

			var returnedCalendar = await response.Content.ReadFromJsonAsync<Calendar>();
			Assert.NotNull(returnedCalendar);
			Assert.Equal(calendar.Id, returnedCalendar?.Id);
			Assert.Equal(calendar.Name, returnedCalendar?.Name);
		}

		[Fact]
		public async Task GetUserCalendarPermissions_ShouldReturn401_WhenTokenIsInvalid()
		{
			// Arrange
			var request = new HttpRequestMessage(HttpMethod.Get, "/api/Calendars/user/1");
			request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "invalid.token");

			// Act
			var response = await _client.SendAsync(request);

			// Assert
			Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
		}

		[Fact]
		public async Task GetUserCalendarPermissions_ShouldReturn200_WhenUserIsAuthenticated()
		{
			// Arrange
			using var scope = _factory.Services.CreateScope();
			var context = scope.ServiceProvider.GetRequiredService<Shalendar.Contexts.ShalendarDbContext>();

			var user = new User { Email = "email@example.com", Username = "user", PasswordHash = "pw" };

			context.Users.Add(user);
			await context.SaveChangesAsync();

			var token = JwtTokenGenerator.GenerateToken(user, Array.Empty<CalendarPermission>());

			var request = new HttpRequestMessage(HttpMethod.Get, $"/api/Calendars/user/{user.Id}");
			request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

			// Act
			var response = await _client.SendAsync(request);

			// Assert
			Assert.Equal(HttpStatusCode.OK, response.StatusCode);

			var permissions = await response.Content.ReadFromJsonAsync<List<CalendarPermission>>();
			Assert.NotNull(permissions);
			Assert.Empty(permissions);
		}

		[Fact]
		public async Task GetCalendarPermissions_ShouldReturn403Forbidden_WhenUserHasNoPermission()
		{
			// Arrange
			using var scope = _factory.Services.CreateScope();
			var context = scope.ServiceProvider.GetRequiredService<Shalendar.Contexts.ShalendarDbContext>();

			var user = new User { Email = "email@example.com", Username = "user", PasswordHash = "pw" };
			var calendar = new Calendar { Name = "Calendar" };

			context.Users.Add(user);
			context.Calendars.Add(calendar);
			await context.SaveChangesAsync();

			var permission = new CalendarPermission
			{
				UserId = user.Id,
				CalendarId = calendar.Id,
				PermissionType = "write"
			};

			context.CalendarPermissions.Add(permission);
			await context.SaveChangesAsync();

			var token = JwtTokenGenerator.GenerateToken(user, new[] { permission });

			var request = new HttpRequestMessage(HttpMethod.Get, $"/api/Calendars/{calendar.Id}/permissions");
			request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
			request.Headers.Add("X-Calendar-Id", calendar.Id.ToString());

			// Act
			var response = await _client.SendAsync(request);

			// Assert
			Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
		}

		[Fact]
		public async Task GetCalendarPermissions_ShouldReturn200_WhenUserHasPermission()
		{
			// Arrange
			using var scope = _factory.Services.CreateScope();
			var context = scope.ServiceProvider.GetRequiredService<Shalendar.Contexts.ShalendarDbContext>();

			var user = new User { Email = "email@example.com", Username = "user", PasswordHash = "pw" };
			var calendar = new Calendar { Name = "Calendar" };

			context.Users.Add(user);
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

			var request = new HttpRequestMessage(HttpMethod.Get, $"/api/Calendars/{calendar.Id}/permissions");
			request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
			request.Headers.Add("X-Calendar-Id", calendar.Id.ToString());

			// Act
			var response = await _client.SendAsync(request);

			// Assert
			Assert.Equal(HttpStatusCode.OK, response.StatusCode);

			var data = await response.Content.ReadFromJsonAsync<List<JsonElement>>();
			Assert.NotNull(data);
			Assert.Single(data);
			Assert.Equal("email@example.com", data[0].GetProperty("email").GetString());
			Assert.Equal("owner", data[0].GetProperty("permissionType").GetString());

		}

		[Fact]
		public async Task GetUserAccessibleCalendars_ShouldReturn404_WhenUserHasNoWriteOrOwnerPermissions()
		{
			// Arrange
			using var scope = _factory.Services.CreateScope();
			var context = scope.ServiceProvider.GetRequiredService<Shalendar.Contexts.ShalendarDbContext>();

			var user = new User { Email = "user@example.com", Username = "user", PasswordHash = "pw" };
			var calendar = new Calendar { Name = "Cal" };

			context.Users.Add(user);
			context.Calendars.Add(calendar);
			await context.SaveChangesAsync();

			var permission = new CalendarPermission
			{
				UserId = user.Id,
				CalendarId = calendar.Id,
				PermissionType = "read"
			};

			context.CalendarPermissions.Add(permission);
			await context.SaveChangesAsync();

			var token = JwtTokenGenerator.GenerateToken(user, new[] { permission });

			var request = new HttpRequestMessage(HttpMethod.Get, "/api/Calendars/accessible");
			request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

			// Act
			var response = await _client.SendAsync(request);

			// Assert
			Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
		}

		[Fact]
		public async Task GetUserAccessibleCalendars_ShouldReturn200_WhenUserHasPermission()
		{
			// Arrange
			using var scope = _factory.Services.CreateScope();
			var context = scope.ServiceProvider.GetRequiredService<Shalendar.Contexts.ShalendarDbContext>();

			var user = new User { Email = "user@example.com", Username = "user", PasswordHash = "pw" };
			var calendar = new Calendar { Name = "Cal" };

			context.Users.Add(user);
			context.Calendars.Add(calendar);
			await context.SaveChangesAsync();

			var permission = new CalendarPermission
			{
				UserId = user.Id,
				CalendarId = calendar.Id,
				PermissionType = "write"
			};

			context.CalendarPermissions.Add(permission);
			await context.SaveChangesAsync();

			var token = JwtTokenGenerator.GenerateToken(user, new[] { permission });

			var request = new HttpRequestMessage(HttpMethod.Get, "/api/Calendars/accessible");
			request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

			// Act
			var response = await _client.SendAsync(request);

			// Assert
			Assert.Equal(HttpStatusCode.OK, response.StatusCode);

			var result = await response.Content.ReadFromJsonAsync<List<JsonElement>>();
			Assert.NotNull(result);
			Assert.Single(result);
			Assert.Equal(calendar.Id, result[0].GetProperty("id").GetInt32());
			Assert.Equal(calendar.Name, result[0].GetProperty("name").GetString());
		}

		#endregion



		#region Posts

		[Fact]
		public async Task CreateCalendar_ShouldReturn201Created_WhenTokenIsValid()
		{
			// Arrange
			using var scope = _factory.Services.CreateScope();
			var context = scope.ServiceProvider.GetRequiredService<Shalendar.Contexts.ShalendarDbContext>();

			var user = new User { Email = "user@example.com", Username = "user", PasswordHash = "pw" };
			context.Users.Add(user);
			await context.SaveChangesAsync();

			var calendar = new Calendar { Name = "New Calendar" };

			var token = JwtTokenGenerator.GenerateToken(user, Array.Empty<CalendarPermission>());

			var request = new HttpRequestMessage(HttpMethod.Post, $"/api/Calendars?userId={user.Id}");
			request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
			request.Content = JsonContent.Create(calendar);

			// Act
			var response = await _client.SendAsync(request);

			// Assert
			Assert.Equal(HttpStatusCode.Created, response.StatusCode);

			var created = await response.Content.ReadFromJsonAsync<Calendar>();
			Assert.NotNull(created);
			Assert.Equal("New Calendar", created?.Name);

			using var verifyScope = _factory.Services.CreateScope();
			var verifyContext = verifyScope.ServiceProvider.GetRequiredService<Shalendar.Contexts.ShalendarDbContext>();
			var savedCalendar = await verifyContext.Calendars.FindAsync(created?.Id);
			Assert.NotNull(savedCalendar);

			var defaultList = await verifyContext.CalendarLists.FirstOrDefaultAsync(cl => cl.CalendarId == created!.Id);
			Assert.NotNull(defaultList);
			Assert.Equal("Default List", defaultList!.Name);
		}

		[Fact]
		public async Task AddCalendarPermission_ShouldReturn403Forbidden_WhenUserHasNoPermission()
		{
			// Arrange
			using var scope = _factory.Services.CreateScope();
			var context = scope.ServiceProvider.GetRequiredService<Shalendar.Contexts.ShalendarDbContext>();

			var owner = new User { Email = "owner@example.com", Username = "owner", PasswordHash = "pw" };
			var other = new User { Email = "other@example.com", Username = "other", PasswordHash = "pw" };
			var calendar = new Calendar { Name = "Team Calendar" };

			context.Users.AddRange(owner, other);
			context.Calendars.Add(calendar);
			await context.SaveChangesAsync();

			var permission = new CalendarPermission
			{
				UserId = owner.Id,
				CalendarId = calendar.Id,
				PermissionType = "write"
			};
			context.CalendarPermissions.Add(permission);
			await context.SaveChangesAsync();

			var token = JwtTokenGenerator.GenerateToken(owner, new[] { permission });

			var request = new HttpRequestMessage(HttpMethod.Post, $"/api/Calendars/{calendar.Id}/permissions/{other.Email}/read");
			request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

			// Act
			var response = await _client.SendAsync(request);

			// Assert
			Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
		}

		[Fact]
		public async Task AddCalendarPermission_ShouldReturn200_WhenUserHasPermission()
		{
			// Arrange
			using var scope = _factory.Services.CreateScope();
			var context = scope.ServiceProvider.GetRequiredService<Shalendar.Contexts.ShalendarDbContext>();

			var owner = new User { Email = "owner@example.com", Username = "owner", PasswordHash = "pw" };
			var other = new User { Email = "other@example.com", Username = "other", PasswordHash = "pw" };
			var calendar = new Calendar { Name = "Team Calendar" };

			context.Users.AddRange(owner, other);
			context.Calendars.Add(calendar);
			await context.SaveChangesAsync();

			var permission = new CalendarPermission
			{
				UserId = owner.Id,
				CalendarId = calendar.Id,
				PermissionType = "owner"
			};
			context.CalendarPermissions.Add(permission);
			await context.SaveChangesAsync();

			var token = JwtTokenGenerator.GenerateToken(owner, new[] { permission });

			var request = new HttpRequestMessage(HttpMethod.Post, $"/api/Calendars/{calendar.Id}/permissions/{other.Email}/write");
			request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

			// Act
			var response = await _client.SendAsync(request);

			// Assert
			Assert.Equal(HttpStatusCode.OK, response.StatusCode);
		}

		[Fact]
		public async Task CopyAllTickets_ShouldReturn403Forbidden_WhenUserHasNoPermission()
		{
			// Arrange
			using var scope = _factory.Services.CreateScope();
			var context = scope.ServiceProvider.GetRequiredService<Shalendar.Contexts.ShalendarDbContext>();

			var user = new User { Email = "user@example.com", Username = "user", PasswordHash = "pw" };
			var sourceCalendar = new Calendar { Name = "Original" };
			var targetCalendar = new Calendar { Name = "Target" };

			context.Users.Add(user);
			context.Calendars.AddRange(sourceCalendar, targetCalendar);
			await context.SaveChangesAsync();

			var permission = new CalendarPermission
			{
				UserId = user.Id,
				CalendarId = sourceCalendar.Id,
				PermissionType = "read"
			};

			context.CalendarPermissions.Add(permission);
			await context.SaveChangesAsync();

			var token = JwtTokenGenerator.GenerateToken(user, new[] { permission });

			var request = new HttpRequestMessage(HttpMethod.Post, $"/api/Calendars/copy-all-tickets?calendarId={targetCalendar.Id}");
			request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
			request.Headers.Add("X-Calendar-Id", sourceCalendar.Id.ToString());

			// Act
			var response = await _client.SendAsync(request);

			// Assert
			Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
		}

		[Fact]
		public async Task CopyAllTickets_ShouldReturn200_WhenUserHasPermissions()
		{
			// Arrange
			using var scope = _factory.Services.CreateScope();
			var context = scope.ServiceProvider.GetRequiredService<Shalendar.Contexts.ShalendarDbContext>();

			var user = new User { Email = "user@example.com", Username = "user", PasswordHash = "pw" };
			var sourceCalendar = new Calendar { Name = "Original" };
			var targetCalendar = new Calendar { Name = "Target" };

			context.Users.Add(user);
			context.Calendars.AddRange(sourceCalendar, targetCalendar);
			await context.SaveChangesAsync();

			var permissions = new[]
			{
				new CalendarPermission
				{
					UserId = user.Id,
					CalendarId = sourceCalendar.Id,
					PermissionType = "read"
				},
				new CalendarPermission
				{
					UserId = user.Id,
					CalendarId = targetCalendar.Id,
					PermissionType = "write"
				}
			};

			context.CalendarPermissions.AddRange(permissions);
			await context.SaveChangesAsync();

			var token = JwtTokenGenerator.GenerateToken(user, permissions);

			var request = new HttpRequestMessage(HttpMethod.Post, $"/api/Calendars/copy-all-tickets?calendarId={targetCalendar.Id}");
			request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
			request.Headers.Add("X-Calendar-Id", sourceCalendar.Id.ToString());

			// Act
			var response = await _client.SendAsync(request);

			// Assert
			Assert.Equal(HttpStatusCode.OK, response.StatusCode);

			var responseText = await response.Content.ReadAsStringAsync();
			Assert.Contains("successfully copied", responseText);
		}

		#endregion



		#region Puts

		#endregion



		#region Deletes

		[Fact]
		public async Task DeleteCalendarPermission_ShouldReturn200_WhenPermissionExists()
		{
			// Arrange
			using var scope = _factory.Services.CreateScope();
			var context = scope.ServiceProvider.GetRequiredService<Shalendar.Contexts.ShalendarDbContext>();

			var user = new User { Email = "test@example.com", Username = "u", PasswordHash = "pw" };
			var owner = new User { Email = "owner@example.com", Username = "owner", PasswordHash = "pw" };
			var calendar = new Calendar { Name = "Test Calendar" };

			context.Users.AddRange(user, owner);
			context.Calendars.Add(calendar);
			await context.SaveChangesAsync();

			var permission = new CalendarPermission
			{
				UserId = user.Id,
				CalendarId = calendar.Id,
				PermissionType = "read"
			};

			context.CalendarPermissions.Add(permission);
			await context.SaveChangesAsync();

			var token = JwtTokenGenerator.GenerateToken(owner, Array.Empty<CalendarPermission>());

			var request = new HttpRequestMessage(HttpMethod.Delete, $"/api/Calendars/{calendar.Id}/permissions/{user.Email}");
			request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

			// Act
			var response = await _client.SendAsync(request);

			// Assert
			Assert.Equal(HttpStatusCode.OK, response.StatusCode);

			using var verifyScope = _factory.Services.CreateScope();
			var verifyContext = verifyScope.ServiceProvider.GetRequiredService<Shalendar.Contexts.ShalendarDbContext>();
			var deleted = await verifyContext.CalendarPermissions.FirstOrDefaultAsync(p => p.UserId == user.Id && p.CalendarId == calendar.Id);
			Assert.Null(deleted);
		}


		[Fact]
		public async Task DeleteCalendar_ShouldReturn200_WhenUserIsOwner()
		{
			// Arrange
			using var scope = _factory.Services.CreateScope();
			var context = scope.ServiceProvider.GetRequiredService<Shalendar.Contexts.ShalendarDbContext>();

			var user = new User { Email = "owner@example.com", Username = "user", PasswordHash = "pw" };
			var calendar = new Calendar { Name = "To Delete" };

			context.Users.Add(user);
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

			var request = new HttpRequestMessage(HttpMethod.Delete, $"/api/Calendars/{calendar.Id}");
			request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

			// Act
			var response = await _client.SendAsync(request);

			// Assert
			Assert.Equal(HttpStatusCode.OK, response.StatusCode);

			using var verifyScope = _factory.Services.CreateScope();
			var verifyContext = verifyScope.ServiceProvider.GetRequiredService<Shalendar.Contexts.ShalendarDbContext>();

			var deletedCalendar = await verifyContext.Calendars.FindAsync(calendar.Id);
			Assert.Null(deletedCalendar);

			var deletedPermission = await verifyContext.CalendarPermissions
				.FirstOrDefaultAsync(p => p.CalendarId == calendar.Id && p.UserId == user.Id);
			Assert.Null(deletedPermission);
		}

		#endregion

	}
}
