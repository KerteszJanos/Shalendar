using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Shalendar.Models;
using Shalendar.Tests.Integration.Helpers;
using Xunit;

namespace Shalendar.Tests.Integration.Controllers
{
	public class CalendarListUnauthorizedTests : IClassFixture<CustomWebApplicationFactory>
	{
		private readonly HttpClient _client;
		private readonly CustomWebApplicationFactory _factory;

		public CalendarListUnauthorizedTests(CustomWebApplicationFactory factory)
		{
			_factory = factory;
			_client = factory.CreateClient();
		}

		public static IEnumerable<object[]> calendarListsControllerEndpoints => new List<object[]>
		{
			new object[] { HttpMethod.Get, "/api/CalendarLists/calendar/1" },
			new object[] { HttpMethod.Post, "/api/CalendarLists" },
			new object[] { HttpMethod.Put, "/api/CalendarLists/1" },
			new object[] { HttpMethod.Delete, "/api/CalendarLists/1" },
		};

		[Theory]
		[MemberData(nameof(calendarListsControllerEndpoints))]
		public async Task Endpoint_ShouldReturn401_WhenTokenIsInvalid(HttpMethod method, string url)
		{
			var request = new HttpRequestMessage(method, url);
			request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "invalid.token.here");

			var response = await _client.SendAsync(request);

			Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
		}

		#region Gets

		[Fact]
		public async Task GetCalendarListsByCalendarId_ShouldReturn403Forbidden_WhenUserHasNoPermission()
		{
			// Arrange
			using var scope = _factory.Services.CreateScope();
			var context = scope.ServiceProvider.GetRequiredService<Shalendar.Contexts.ShalendarDbContext>();

			var user = new User { Email = "email@example.com", Username = "user", PasswordHash = "pw" };
			var calendar = new Calendar { Name = "Calendar" };

			context.Users.Add(user);
			context.Calendars.Add(calendar);
			await context.SaveChangesAsync();

			var list = new CalendarList
			{
				Name = "List",
				Color = "#FFFFFF",
				CalendarId = calendar.Id
			};

			context.CalendarLists.Add(list);
			await context.SaveChangesAsync();

			var token = JwtTokenGenerator.GenerateToken(user, Array.Empty<CalendarPermission>());
			var request = new HttpRequestMessage(HttpMethod.Get, $"/api/CalendarLists/calendar/{calendar.Id}");
			request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
			request.Headers.Add("X-Calendar-Id", calendar.Id.ToString());

			// Act
			var response = await _client.SendAsync(request);

			// Assert
			Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
		}

		[Fact]
		public async Task GetCalendarListsByCalendarId_ShouldReturn200_WhenUserHasPermission()
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
				PermissionType = "read"
			};

			var list = new CalendarList
			{
				Name = "List",
				Color = "#FFFFFF",
				CalendarId = calendar.Id
			};

			context.CalendarPermissions.Add(permission);
			context.CalendarLists.Add(list);
			await context.SaveChangesAsync();

			var token = JwtTokenGenerator.GenerateToken(user, new[] { permission });
			var request = new HttpRequestMessage(HttpMethod.Get, $"/api/CalendarLists/calendar/{calendar.Id}");
			request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
			request.Headers.Add("X-Calendar-Id", calendar.Id.ToString());

			// Act
			var response = await _client.SendAsync(request);

			// Assert
			Assert.Equal(HttpStatusCode.OK, response.StatusCode);

			var responseData = await response.Content.ReadFromJsonAsync<JsonElement>();
			Assert.True(responseData.ValueKind == JsonValueKind.Array);
		}
		#endregion



		#region Posts

		[Fact]
		public async Task PostCalendarList_ShouldReturn403Forbidden_WhenUserHasNoPermission()
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

			var token = JwtTokenGenerator.GenerateToken(user, [permission]);

			var newList = new CalendarList
			{
				Name = "List",
				Color = "#123456",
				CalendarId = calendar.Id
			};

			var request = new HttpRequestMessage(HttpMethod.Post, "/api/CalendarLists");
			request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
			request.Headers.Add("X-Calendar-Id", calendar.Id.ToString());
			request.Content = JsonContent.Create(newList);

			// Act
			var response = await _client.SendAsync(request);

			// Assert
			Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
		}

		[Fact]
		public async Task PostCalendarList_ShouldReturn201Created_WhenUserHasPermission()
		{
			// Arrange
			using var scope = _factory.Services.CreateScope();
			var context = scope.ServiceProvider.GetRequiredService<Shalendar.Contexts.ShalendarDbContext>();

			var user = new User { Email = "email@example.com", Username = "name", PasswordHash = "pw" };
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

			var token = JwtTokenGenerator.GenerateToken(user, [permission]);

			var newList = new CalendarList
			{
				Name = "List",
				Color = "#123456",
				CalendarId = calendar.Id
			};

			var request = new HttpRequestMessage(HttpMethod.Post, "/api/CalendarLists");
			request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
			request.Headers.Add("X-Calendar-Id", calendar.Id.ToString());
			request.Content = JsonContent.Create(newList);

			// Act
			var response = await _client.SendAsync(request);

			// Assert
			Assert.Equal(HttpStatusCode.Created, response.StatusCode);

			var createdList = await response.Content.ReadFromJsonAsync<CalendarList>();
			Assert.Equal("List", createdList?.Name);
			Assert.Equal("#123456", createdList?.Color);
			Assert.Equal(calendar.Id, createdList?.CalendarId);
		}

		#endregion



		#region Puts

		[Fact]
		public async Task UpdateCalendarList_ShouldReturn403Forbidden_WhenUserHasNoPermission()
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

			var list = new CalendarList
			{
				Name = "Name",
				Color = "#000000",
				CalendarId = calendar.Id
			};

			context.CalendarPermissions.Add(permission);
			context.CalendarLists.Add(list);
			await context.SaveChangesAsync();

			var token = JwtTokenGenerator.GenerateToken(user, new[] { permission });

			var updatedList = new CalendarList
			{
				Id = list.Id,
				Name = "Updated Name",
				Color = "#FF00FF",
				CalendarId = calendar.Id
			};

			var request = new HttpRequestMessage(HttpMethod.Put, $"/api/CalendarLists/{list.Id}");
			request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
			request.Headers.Add("X-Calendar-Id", calendar.Id.ToString());
			request.Content = JsonContent.Create(updatedList);

			// Act
			var response = await _client.SendAsync(request);

			// Assert
			Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
		}

		[Fact]
		public async Task UpdateCalendarList_ShouldReturn204NoContent_WhenUserHasPermission()
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

			var list = new CalendarList
			{
				Name = "Name",
				Color = "#000000",
				CalendarId = calendar.Id
			};

			context.CalendarPermissions.Add(permission);
			context.CalendarLists.Add(list);
			await context.SaveChangesAsync();

			var token = JwtTokenGenerator.GenerateToken(user, new[] { permission });

			var updatedList = new CalendarList
			{
				Id = list.Id,
				Name = "Updated Name",
				Color = "#FF00FF",
				CalendarId = calendar.Id
			};

			var request = new HttpRequestMessage(HttpMethod.Put, $"/api/CalendarLists/{list.Id}");
			request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
			request.Headers.Add("X-Calendar-Id", calendar.Id.ToString());
			request.Content = JsonContent.Create(updatedList);

			// Act
			var response = await _client.SendAsync(request);

			// Assert
			Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

			using var verifyScope = _factory.Services.CreateScope();
			var verifyContext = verifyScope.ServiceProvider.GetRequiredService<Shalendar.Contexts.ShalendarDbContext>();
			var updatedEntity = await verifyContext.CalendarLists.FindAsync(list.Id);
			Assert.Equal("Updated Name", updatedEntity?.Name);
			Assert.Equal("#FF00FF", updatedEntity?.Color);

		}

		#endregion


		#region Deletes

		[Fact]
		public async Task DeleteCalendarList_ShouldReturn403Forbidden_WhenUserHasNoPermission()
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

			var list = new CalendarList
			{
				Name = "Name",
				Color = "#000000",
				CalendarId = calendar.Id
			};

			context.CalendarPermissions.Add(permission);
			context.CalendarLists.Add(list);
			await context.SaveChangesAsync();

			var token = JwtTokenGenerator.GenerateToken(user, new[] { permission });

			var request = new HttpRequestMessage(HttpMethod.Delete, $"/api/CalendarLists/{list.Id}");
			request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
			request.Headers.Add("X-Calendar-Id", calendar.Id.ToString());

			// Act
			var response = await _client.SendAsync(request);

			// Assert
			Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
		}

		[Fact]
		public async Task DeleteCalendarList_ShouldReturn204NoContent_WhenUserHasPermission()
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

			var list = new CalendarList
			{
				Name = "Name",
				Color = "#000000",
				CalendarId = calendar.Id
			};

			context.CalendarPermissions.Add(permission);
			context.CalendarLists.Add(list);
			await context.SaveChangesAsync();

			var ticket = new Ticket
			{
				Name = "Test Ticket",
				Description = "Test Description",
				CalendarListId = list.Id,
				CurrentParentType = "SomeType",
				ParentId = 1,
				IsCompleted = false
			};
			context.Tickets.Add(ticket);
			await context.SaveChangesAsync();

			var token = JwtTokenGenerator.GenerateToken(user, new[] { permission });

			var request = new HttpRequestMessage(HttpMethod.Delete, $"/api/CalendarLists/{list.Id}");
			request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
			request.Headers.Add("X-Calendar-Id", calendar.Id.ToString());

			// Act
			var response = await _client.SendAsync(request);

			// Assert
			if (response.StatusCode != HttpStatusCode.NoContent)
			{
				var content = await response.Content.ReadAsStringAsync();
				throw new Exception($"Unexpected response: {response.StatusCode} - {content}");
			}
			Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

			using var verifyScope = _factory.Services.CreateScope();
			var verifyContext = verifyScope.ServiceProvider.GetRequiredService<Shalendar.Contexts.ShalendarDbContext>();
			var deletedList = await verifyContext.CalendarLists.FindAsync(list.Id);
			Assert.Null(deletedList);

			var deletedTicket = await verifyContext.Tickets.FindAsync(ticket.Id);
			Assert.Null(deletedTicket);
		}

		#endregion
	}
}