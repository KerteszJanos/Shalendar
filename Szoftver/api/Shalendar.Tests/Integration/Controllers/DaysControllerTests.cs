using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Shalendar.Models;
using System.Text.Json;
using Shalendar.Tests.Integration.Helpers;
using Xunit;
using Microsoft.EntityFrameworkCore;

namespace Shalendar.Tests.Integration.Controllers
{
	public class DaysControllerTests : IClassFixture<CustomWebApplicationFactory>
	{
		private readonly HttpClient _client;
		private readonly CustomWebApplicationFactory _factory;

		public DaysControllerTests(CustomWebApplicationFactory factory)
		{
			_factory = factory;
			_client = factory.CreateClient();
		}

		public static IEnumerable<object[]> daysControllerEndpoints => new List<object[]>
		{
			new object[] { HttpMethod.Get, "/api/Days/2025-03-24/1" },
			new object[] { HttpMethod.Get, "/api/Days/range/2025-03-24/2025-03-26/1" },

			new object[] { HttpMethod.Post, "/api/Days/create" },

			new object[] { HttpMethod.Delete, "/api/Days/1/2025-03-24" }
		};

		[Theory]
		[MemberData(nameof(daysControllerEndpoints))]
		public async Task Endpoint_ShouldReturn401_WhenTokenIsInvalid(HttpMethod method, string url)
		{
			var request = new HttpRequestMessage(method, url);
			request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "invalid.token.here");

			if (method == HttpMethod.Post)
				request.Content = new StringContent("{}", Encoding.UTF8, "application/json");

			var response = await _client.SendAsync(request);

			Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
		}



		#region Gets

		[Fact]
		public async Task GetDayId_ShouldReturn403Forbidden_WhenUserHasNoPermission()
		{
			// Arrange
			using var scope = _factory.Services.CreateScope();
			var context = scope.ServiceProvider.GetRequiredService<Shalendar.Contexts.ShalendarDbContext>();

			var user = new User { Email = "user@example.com", Username = "user", PasswordHash = "pw" };
			var calendar = new Calendar { Name = "Test Calendar" };
			var date = new DateTime(2025, 3, 24);

			context.Users.Add(user);
			context.Calendars.Add(calendar);
			await context.SaveChangesAsync();

			var day = new Day { CalendarId = calendar.Id, Date = date };
			context.Days.Add(day);
			await context.SaveChangesAsync();

			var token = JwtTokenGenerator.GenerateToken(user, Array.Empty<CalendarPermission>());

			var request = new HttpRequestMessage(HttpMethod.Get, $"/api/Days/{date:yyyy-MM-dd}/{calendar.Id}");
			request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

			// Act
			var response = await _client.SendAsync(request);

			// Assert
			Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
		}

		[Fact]
		public async Task GetDayId_ShouldReturn200_WhenUserHasPermission()
		{
			// Arrange
			using var scope = _factory.Services.CreateScope();
			var context = scope.ServiceProvider.GetRequiredService<Shalendar.Contexts.ShalendarDbContext>();

			var user = new User { Email = "user@example.com", Username = "user", PasswordHash = "pw" };
			var calendar = new Calendar { Name = "Test Calendar" };
			var date = new DateTime(2025, 3, 24);

			context.Users.Add(user);
			context.Calendars.Add(calendar);
			await context.SaveChangesAsync();

			var permission = new CalendarPermission
			{
				UserId = user.Id,
				CalendarId = calendar.Id,
				PermissionType = "read"
			};

			var day = new Day { CalendarId = calendar.Id, Date = date };
			context.CalendarPermissions.Add(permission);
			context.Days.Add(day);
			await context.SaveChangesAsync();

			var token = JwtTokenGenerator.GenerateToken(user, new[] { permission });

			var request = new HttpRequestMessage(HttpMethod.Get, $"/api/Days/{date:yyyy-MM-dd}/{calendar.Id}");
			request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
			request.Headers.Add("X-Calendar-Id", calendar.Id.ToString());

			// Act
			var response = await _client.SendAsync(request);

			// Assert
			Assert.Equal(HttpStatusCode.OK, response.StatusCode);

			var result = await response.Content.ReadFromJsonAsync<JsonElement>();
			Assert.True(result.TryGetProperty("id", out var idProp));
			Assert.Equal(day.Id, idProp.GetInt32());
		}

		[Fact]
		public async Task GetExistingDaysInRange_ShouldReturn403Forbidden_WhenUserHasNoPermission()
		{
			// Arrange
			using var scope = _factory.Services.CreateScope();
			var context = scope.ServiceProvider.GetRequiredService<Shalendar.Contexts.ShalendarDbContext>();

			var user = new User { Email = "user@example.com", Username = "user", PasswordHash = "pw" };
			var calendar = new Calendar { Name = "Range Test" };
			var date = new DateTime(2025, 3, 24);

			context.Users.Add(user);
			context.Calendars.Add(calendar);
			await context.SaveChangesAsync();

			var day = new Day { CalendarId = calendar.Id, Date = date };
			context.Days.Add(day);
			await context.SaveChangesAsync();

			var token = JwtTokenGenerator.GenerateToken(user, Array.Empty<CalendarPermission>());

			var request = new HttpRequestMessage(HttpMethod.Get, $"/api/Days/range/2025-03-24/2025-03-26/{calendar.Id}");
			request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

			// Act
			var response = await _client.SendAsync(request);

			// Assert
			Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
		}

		[Fact]
		public async Task GetExistingDaysInRange_ShouldReturn200_WhenUserHasPermission()
		{
			// Arrange
			using var scope = _factory.Services.CreateScope();
			var context = scope.ServiceProvider.GetRequiredService<Shalendar.Contexts.ShalendarDbContext>();

			var user = new User { Email = "user@example.com", Username = "user", PasswordHash = "pw" };
			var calendar = new Calendar { Name = "Range Test" };
			var date = new DateTime(2025, 3, 24);

			context.Users.Add(user);
			context.Calendars.Add(calendar);
			await context.SaveChangesAsync();

			var permission = new CalendarPermission
			{
				UserId = user.Id,
				CalendarId = calendar.Id,
				PermissionType = "read"
			};

			var day = new Day { CalendarId = calendar.Id, Date = date };
			context.CalendarPermissions.Add(permission);
			context.Days.Add(day);
			await context.SaveChangesAsync();

			var token = JwtTokenGenerator.GenerateToken(user, new[] { permission });

			var request = new HttpRequestMessage(HttpMethod.Get, $"/api/Days/range/2025-03-24/2025-03-26/{calendar.Id}");
			request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
			request.Headers.Add("X-Calendar-Id", calendar.Id.ToString());

			// Act
			var response = await _client.SendAsync(request);

			// Assert
			Assert.Equal(HttpStatusCode.OK, response.StatusCode);

			var result = await response.Content.ReadFromJsonAsync<JsonElement>();
			Assert.True(result.TryGetProperty("days", out var days));
			Assert.Equal(JsonValueKind.Array, days.ValueKind);
			Assert.Single(days.EnumerateArray());
		}

		#endregion



		#region Posts

		[Fact]
		public async Task CreateDay_ShouldReturn200AndCreate_WhenDayDoesNotExist()
		{
			// Arrange
			using var scope = _factory.Services.CreateScope();
			var context = scope.ServiceProvider.GetRequiredService<Shalendar.Contexts.ShalendarDbContext>();

			var user = new User { Email = "user@example.com", Username = "user", PasswordHash = "pw" };
			var calendar = new Calendar { Name = "Test Calendar" };
			context.Users.Add(user);
			context.Calendars.Add(calendar);
			await context.SaveChangesAsync();

			var token = JwtTokenGenerator.GenerateToken(user, Array.Empty<CalendarPermission>());

			var dto = new { CalendarId = calendar.Id, Date = "2025-03-24" };
			var request = new HttpRequestMessage(HttpMethod.Post, "/api/Days/create");
			request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
			request.Content = JsonContent.Create(dto);

			// Act
			var response = await _client.SendAsync(request);

			// Assert
			Assert.Equal(HttpStatusCode.OK, response.StatusCode);

			var result = await response.Content.ReadFromJsonAsync<JsonElement>();
			Assert.True(result.TryGetProperty("id", out var idProp));
			Assert.True(idProp.GetInt32() > 0);

			using var verifyScope = _factory.Services.CreateScope();
			var verifyContext = verifyScope.ServiceProvider.GetRequiredService<Shalendar.Contexts.ShalendarDbContext>();
			var created = await verifyContext.Days.FindAsync(idProp.GetInt32());
			Assert.NotNull(created);
			Assert.Equal(calendar.Id, created!.CalendarId);
			Assert.Equal(new DateTime(2025, 3, 24), created.Date.Date);
		}

		#endregion



		#region Puts

		#endregion



		#region Deletes

		[Fact]
		public async Task DeleteDayIfNoTickets_ShouldReturn200AndDelete_WhenDayHasNoTickets()
		{
			// Arrange
			using var scope = _factory.Services.CreateScope();
			var context = scope.ServiceProvider.GetRequiredService<Shalendar.Contexts.ShalendarDbContext>();

			var user = new User { Email = "user@example.com", Username = "user", PasswordHash = "pw" };
			var calendar = new Calendar { Name = "NoTicketCal" };
			var date = new DateTime(2025, 3, 24);

			context.Users.Add(user);
			context.Calendars.Add(calendar);
			await context.SaveChangesAsync();

			var day = new Day { CalendarId = calendar.Id, Date = date };
			context.Days.Add(day);
			await context.SaveChangesAsync();

			var token = JwtTokenGenerator.GenerateToken(user, Array.Empty<CalendarPermission>());

			var request = new HttpRequestMessage(HttpMethod.Delete, $"/api/Days/{calendar.Id}/{date:yyyy-MM-dd}");
			request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

			// Act
			var response = await _client.SendAsync(request);

			// Assert
			Assert.Equal(HttpStatusCode.OK, response.StatusCode);

			var text = await response.Content.ReadAsStringAsync();
			Assert.Contains("deleted successfully", text);

			using var verifyScope = _factory.Services.CreateScope();
			var verifyContext = verifyScope.ServiceProvider.GetRequiredService<Shalendar.Contexts.ShalendarDbContext>();
			var deleted = await verifyContext.Days.FirstOrDefaultAsync(d => d.Id == day.Id);
			Assert.Null(deleted);
		}

		[Fact]
		public async Task DeleteDayIfNoTickets_ShouldReturn200AndNotDelete_WhenDayHasTickets()
		{
			// Arrange
			using var scope = _factory.Services.CreateScope();
			var context = scope.ServiceProvider.GetRequiredService<Shalendar.Contexts.ShalendarDbContext>();

			var user = new User { Email = "user@example.com", Username = "user", PasswordHash = "pw" };
			var calendar = new Calendar { Name = "TicketCal" };
			var date = new DateTime(2025, 3, 24);

			context.Users.Add(user);
			context.Calendars.Add(calendar);
			await context.SaveChangesAsync();

			var day = new Day { CalendarId = calendar.Id, Date = date };
			context.Days.Add(day);
			await context.SaveChangesAsync();

			var ticket = new Ticket
			{
				Name = "Test Ticket",
				Description = "Desc",
				CalendarListId = 1,
				ParentId = day.Id,
				CurrentParentType = "TodoList",
				IsCompleted = false
			};

			context.Tickets.Add(ticket);
			await context.SaveChangesAsync();

			var token = JwtTokenGenerator.GenerateToken(user, Array.Empty<CalendarPermission>());

			var request = new HttpRequestMessage(HttpMethod.Delete, $"/api/Days/{calendar.Id}/{date:yyyy-MM-dd}");
			request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

			// Act
			var response = await _client.SendAsync(request);

			// Assert
			Assert.Equal(HttpStatusCode.OK, response.StatusCode);

			var text = await response.Content.ReadAsStringAsync();
			Assert.Contains("not deleted", text);

			using var verifyScope = _factory.Services.CreateScope();
			var verifyContext = verifyScope.ServiceProvider.GetRequiredService<Shalendar.Contexts.ShalendarDbContext>();
			var stillExists = await verifyContext.Days.FirstOrDefaultAsync(d => d.Id == day.Id);
			Assert.NotNull(stillExists);
		}

		#endregion

	}
}
