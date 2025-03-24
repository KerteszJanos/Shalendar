using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shalendar.Models;
using Shalendar.Models.Dtos;
using Shalendar.Tests.Integration.Helpers;
using Xunit;

namespace Shalendar.Tests.Integration.Controllers
{
	public class TicketsControllerTests : IClassFixture<CustomWebApplicationFactory>
	{
		private readonly HttpClient _client;
		private readonly CustomWebApplicationFactory _factory;

		public TicketsControllerTests(CustomWebApplicationFactory factory)
		{
			_factory = factory;
			_client = factory.CreateClient();
		}

		public static IEnumerable<object[]> ticketsControllerEndpoints => new List<object[]>
		{
			new object[] { HttpMethod.Get, "/api/Tickets/todolist/2025-03-24/1" },
			new object[] { HttpMethod.Get, "/api/Tickets/scheduled/2025-03-24/1" },
			new object[] { HttpMethod.Get, "/api/Tickets/AllDailyTickets/2025-03-24/1" },
			new object[] { HttpMethod.Get, "/api/Tickets/AllDailyTickets/1" },

			new object[] { HttpMethod.Post, "/api/Tickets" },
			new object[] { HttpMethod.Post, "/api/Tickets/ScheduleTicket" },
			new object[] { HttpMethod.Post, "/api/Tickets/copy-ticket" },

			new object[] { HttpMethod.Put, "/api/Tickets/reorder" },
			new object[] { HttpMethod.Put, "/api/Tickets/move-to-calendar/1" },
			new object[] { HttpMethod.Put, "/api/Tickets/updateTicket" },
			new object[] { HttpMethod.Put, "/api/Tickets/updateTicketCompleted?ticketId=1&isCompleted=true" },
			new object[] { HttpMethod.Put, "/api/Tickets/changeDate/1" },

			new object[] { HttpMethod.Delete, "/api/Tickets/1" }
		};

		[Theory]
		[MemberData(nameof(ticketsControllerEndpoints))]
		public async Task Endpoint_ShouldReturn401_WhenTokenIsInvalid(HttpMethod method, string url)
		{
			var request = new HttpRequestMessage(method, url);
			request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "invalid.token.here");

			if (method == HttpMethod.Post || method == HttpMethod.Put)
			{
				request.Content = new StringContent("{}", Encoding.UTF8, "application/json");
			}

			var response = await _client.SendAsync(request);

			Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
		}

		#region Gets

		[Fact]
		public async Task GetTodoListTicketsByDateAndCalendar_ShouldReturn403Forbidden_WhenUserHasNoPermission()
		{
			// Arrange
			using var scope = _factory.Services.CreateScope();
			var context = scope.ServiceProvider.GetRequiredService<Shalendar.Contexts.ShalendarDbContext>();

			var user = new User { Email = "user1@example.com", Username = "user1", PasswordHash = "pw" };
			var calendar = new Calendar { Name = "Calendar1" };
			var day = new Day { CalendarId = calendar.Id, Date = new DateTime(2025, 3, 24) };

			context.Users.Add(user);
			context.Calendars.Add(calendar);
			context.Days.Add(day);
			await context.SaveChangesAsync();

			var token = JwtTokenGenerator.GenerateToken(user, Array.Empty<CalendarPermission>());
			var request = new HttpRequestMessage(HttpMethod.Get, $"/api/Tickets/todolist/2025-03-24/{calendar.Id}");
			request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
			request.Headers.Add("X-Calendar-Id", calendar.Id.ToString());

			// Act
			var response = await _client.SendAsync(request);

			// Assert
			Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
		}

		[Fact]
		public async Task GetTodoListTicketsByDateAndCalendar_ShouldReturn200_WhenUserHasPermission()
		{
			// Arrange
			using var scope = _factory.Services.CreateScope();
			var context = scope.ServiceProvider.GetRequiredService<Shalendar.Contexts.ShalendarDbContext>();

			var user = new User { Email = "user2@example.com", Username = "user2", PasswordHash = "pw" };
			var calendar = new Calendar { Name = "Calendar2" };
			var day = new Day { CalendarId = calendar.Id, Date = new DateTime(2025, 3, 24) };
			context.Users.Add(user);
			context.Calendars.Add(calendar);
			context.Days.Add(day);
			await context.SaveChangesAsync();

			var permission = new CalendarPermission { CalendarId = calendar.Id, UserId = user.Id, PermissionType = "read" };
			context.CalendarPermissions.Add(permission);
			await context.SaveChangesAsync();

			var token = JwtTokenGenerator.GenerateToken(user, new[] { permission });
			var request = new HttpRequestMessage(HttpMethod.Get, $"/api/Tickets/todolist/2025-03-24/{calendar.Id}");
			request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
			request.Headers.Add("X-Calendar-Id", calendar.Id.ToString());

			// Act
			var response = await _client.SendAsync(request);

			// Assert
			Assert.Equal(HttpStatusCode.OK, response.StatusCode);
		}

		[Fact]
		public async Task GetScheduledListTicketsByDateAndCalendar_ShouldReturn403Forbidden_WhenUserHasNoPermission()
		{
			// Arrange
			using var scope = _factory.Services.CreateScope();
			var context = scope.ServiceProvider.GetRequiredService<Shalendar.Contexts.ShalendarDbContext>();

			var user = new User { Email = "user3@example.com", Username = "user3", PasswordHash = "pw" };
			var calendar = new Calendar { Name = "Calendar3" };
			var day = new Day { CalendarId = calendar.Id, Date = new DateTime(2025, 3, 24) };
			context.Users.Add(user);
			context.Calendars.Add(calendar);
			context.Days.Add(day);
			await context.SaveChangesAsync();

			var token = JwtTokenGenerator.GenerateToken(user, Array.Empty<CalendarPermission>());
			var request = new HttpRequestMessage(HttpMethod.Get, $"/api/Tickets/scheduled/2025-03-24/{calendar.Id}");
			request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
			request.Headers.Add("X-Calendar-Id", calendar.Id.ToString());

			// Act
			var response = await _client.SendAsync(request);

			// Assert
			Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
		}

		[Fact]
		public async Task GetScheduledListTicketsByDateAndCalendar_ShouldReturn200_WhenUserHasPermission()
		{
			// Arrange
			using var scope = _factory.Services.CreateScope();
			var context = scope.ServiceProvider.GetRequiredService<Shalendar.Contexts.ShalendarDbContext>();

			var user = new User { Email = "user4@example.com", Username = "user4", PasswordHash = "pw" };
			var calendar = new Calendar { Name = "Calendar4" };
			var day = new Day { CalendarId = calendar.Id, Date = new DateTime(2025, 3, 24) };
			context.Users.Add(user);
			context.Calendars.Add(calendar);
			context.Days.Add(day);
			await context.SaveChangesAsync();

			var permission = new CalendarPermission { CalendarId = calendar.Id, UserId = user.Id, PermissionType = "read" };
			context.CalendarPermissions.Add(permission);
			await context.SaveChangesAsync();

			var token = JwtTokenGenerator.GenerateToken(user, new[] { permission });
			var request = new HttpRequestMessage(HttpMethod.Get, $"/api/Tickets/scheduled/2025-03-24/{calendar.Id}");
			request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
			request.Headers.Add("X-Calendar-Id", calendar.Id.ToString());

			// Act
			var response = await _client.SendAsync(request);

			// Assert
			Assert.Equal(HttpStatusCode.OK, response.StatusCode);
		}

		[Fact]
		public async Task GetAllDailyTicketsByDateAndCalendar_ShouldReturn403Forbidden_WhenUserHasNoPermission()
		{
			// Arrange
			using var scope = _factory.Services.CreateScope();
			var context = scope.ServiceProvider.GetRequiredService<Shalendar.Contexts.ShalendarDbContext>();

			var user = new User { Email = "noaccess@example.com", Username = "noaccess", PasswordHash = "pw" };
			var calendar = new Calendar { Name = "ForbiddenCalendar" };
			var day = new Day { CalendarId = calendar.Id, Date = new DateTime(2025, 3, 24) };

			context.Users.Add(user);
			context.Calendars.Add(calendar);
			context.Days.Add(day);
			await context.SaveChangesAsync();

			var token = JwtTokenGenerator.GenerateToken(user, Array.Empty<CalendarPermission>());
			var request = new HttpRequestMessage(HttpMethod.Get, $"/api/Tickets/AllDailyTickets/2025-03-24/{calendar.Id}");
			request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
			request.Headers.Add("X-Calendar-Id", calendar.Id.ToString());

			// Act
			var response = await _client.SendAsync(request);

			// Assert
			Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
		}

		[Fact]
		public async Task GetAllDailyTicketsByDateAndCalendar_ShouldReturn200_WhenUserHasPermission()
		{
			// Arrange
			using var scope = _factory.Services.CreateScope();
			var context = scope.ServiceProvider.GetRequiredService<Shalendar.Contexts.ShalendarDbContext>();

			var user = new User { Email = "access@example.com", Username = "access", PasswordHash = "pw" };
			var calendar = new Calendar { Name = "PermittedCalendar" };
			var day = new Day { CalendarId = calendar.Id, Date = new DateTime(2025, 3, 24) };
			context.Users.Add(user);
			context.Calendars.Add(calendar);
			context.Days.Add(day);
			await context.SaveChangesAsync();

			var permission = new CalendarPermission { CalendarId = calendar.Id, UserId = user.Id, PermissionType = "read" };
			context.CalendarPermissions.Add(permission);
			await context.SaveChangesAsync();

			var token = JwtTokenGenerator.GenerateToken(user, new[] { permission });
			var request = new HttpRequestMessage(HttpMethod.Get, $"/api/Tickets/AllDailyTickets/2025-03-24/{calendar.Id}");
			request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
			request.Headers.Add("X-Calendar-Id", calendar.Id.ToString());

			// Act
			var response = await _client.SendAsync(request);

			// Assert
			Assert.Equal(HttpStatusCode.OK, response.StatusCode);
		}

		[Fact]
		public async Task GetAllDailyTicketsByDayId_ShouldReturn403Forbidden_WhenUserHasNoPermission()
		{
			// Arrange
			using var scope = _factory.Services.CreateScope();
			var context = scope.ServiceProvider.GetRequiredService<Shalendar.Contexts.ShalendarDbContext>();

			var user = new User { Email = "noperm@example.com", Username = "noperm", PasswordHash = "pw" };
			var calendar = new Calendar { Name = "NoPermCalendar" };
			var day = new Day { CalendarId = calendar.Id, Date = new DateTime(2025, 3, 24) };

			context.Users.Add(user);
			context.Calendars.Add(calendar);
			context.Days.Add(day);
			await context.SaveChangesAsync();

			var token = JwtTokenGenerator.GenerateToken(user, Array.Empty<CalendarPermission>());
			var request = new HttpRequestMessage(HttpMethod.Get, $"/api/Tickets/AllDailyTickets/{day.Id}");
			request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
			request.Headers.Add("X-Calendar-Id", calendar.Id.ToString());

			// Act
			var response = await _client.SendAsync(request);

			// Assert
			Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
		}

		[Fact]
		public async Task GetAllDailyTicketsByDayId_ShouldReturn200_WhenUserHasPermission()
		{
			// Arrange
			using var scope = _factory.Services.CreateScope();
			var context = scope.ServiceProvider.GetRequiredService<Shalendar.Contexts.ShalendarDbContext>();

			var user = new User { Email = "hasperm@example.com", Username = "hasperm", PasswordHash = "pw" };
			var calendar = new Calendar { Name = "PermCalendar" };
			var day = new Day { CalendarId = calendar.Id, Date = new DateTime(2025, 3, 24) };

			context.Users.Add(user);
			context.Calendars.Add(calendar);
			context.Days.Add(day);
			await context.SaveChangesAsync();

			var permission = new CalendarPermission { CalendarId = calendar.Id, UserId = user.Id, PermissionType = "read" };
			context.CalendarPermissions.Add(permission);
			await context.SaveChangesAsync();

			var token = JwtTokenGenerator.GenerateToken(user, new[] { permission });
			var request = new HttpRequestMessage(HttpMethod.Get, $"/api/Tickets/AllDailyTickets/{day.Id}");
			request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
			request.Headers.Add("X-Calendar-Id", calendar.Id.ToString());

			// Act
			var response = await _client.SendAsync(request);

			// Assert
			Assert.Equal(HttpStatusCode.OK, response.StatusCode);
		}

		#endregion



		#region Posts

		[Fact]
		public async Task CreateTicket_ShouldReturn403Forbidden_WhenUserHasNoPermission()
		{
			// Arrange
			using var scope = _factory.Services.CreateScope();
			var context = scope.ServiceProvider.GetRequiredService<Shalendar.Contexts.ShalendarDbContext>();
			var user = new User { Email = "unauthorized@example.com", Username = "unauth", PasswordHash = "pw" };
			var calendar = new Calendar { Name = "Restricted Calendar" };

			context.Users.Add(user);
			context.Calendars.Add(calendar);
			await context.SaveChangesAsync();

			var token = JwtTokenGenerator.GenerateToken(user, Array.Empty<CalendarPermission>());

			var ticket = new Ticket
			{
				Name = "Should not save",
				Description = "Forbidden test",
				CalendarListId = 1,
				CurrentParentType = "CalendarList",
				ParentId = 1
			};

			var request = new HttpRequestMessage(HttpMethod.Post, "/api/Tickets");
			request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
			request.Headers.Add("X-Calendar-Id", calendar.Id.ToString());
			request.Content = JsonContent.Create(ticket);

			// Act
			var response = await _client.SendAsync(request);

			// Assert
			Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

			using var verifyScope = _factory.Services.CreateScope();
			var verifyContext = verifyScope.ServiceProvider.GetRequiredService<Shalendar.Contexts.ShalendarDbContext>();
			var savedTicket = await verifyContext.Tickets.FirstOrDefaultAsync(t => t.Name == "Should not save");
			Assert.Null(savedTicket);
		}

		[Fact]
		public async Task CreateTicket_ShouldReturn201AndSave_WhenUserHasPermission()
		{
			// Arrange
			using var scope = _factory.Services.CreateScope();
			var context = scope.ServiceProvider.GetRequiredService<Shalendar.Contexts.ShalendarDbContext>();
			var user = new User { Email = "authorized@example.com", Username = "auth", PasswordHash = "pw" };
			var calendar = new Calendar { Name = "Writable Calendar" };

			context.Users.Add(user);
			context.Calendars.Add(calendar);
			await context.SaveChangesAsync();

			var permission = new CalendarPermission { CalendarId = calendar.Id, UserId = user.Id, PermissionType = "write" };
			context.CalendarPermissions.Add(permission);
			await context.SaveChangesAsync();

			var token = JwtTokenGenerator.GenerateToken(user, new[] { permission });

			var ticket = new Ticket
			{
				Name = "Valid Ticket",
				Description = "Should be saved",
				CalendarListId = 1,
				CurrentParentType = "CalendarList",
				ParentId = 1
			};

			var request = new HttpRequestMessage(HttpMethod.Post, "/api/Tickets");
			request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
			request.Headers.Add("X-Calendar-Id", calendar.Id.ToString());
			request.Content = JsonContent.Create(ticket);

			// Act
			var response = await _client.SendAsync(request);

			// Assert
			Assert.Equal(HttpStatusCode.Created, response.StatusCode);

			var result = await response.Content.ReadFromJsonAsync<Ticket>();
			Assert.NotNull(result);
			Assert.Equal("Valid Ticket", result!.Name);

			using var verifyScope = _factory.Services.CreateScope();
			var verifyContext = verifyScope.ServiceProvider.GetRequiredService<Shalendar.Contexts.ShalendarDbContext>();
			var savedTicket = await verifyContext.Tickets.FirstOrDefaultAsync(t => t.Name == "Valid Ticket");
			Assert.NotNull(savedTicket);
			Assert.Equal("Should be saved", savedTicket!.Description);
		}

		[Fact]
		public async Task ScheduleTicket_ShouldReturn403Forbidden_WhenUserHasNoPermission()
		{
			// Arrange
			using var scope = _factory.Services.CreateScope();
			var context = scope.ServiceProvider.GetRequiredService<Shalendar.Contexts.ShalendarDbContext>();

			var user = new User { Email = "noperm@example.com", Username = "noperm", PasswordHash = "pw" };
			var calendar = new Calendar { Name = "Restricted" };
			var ticket = new Ticket { Name = "Task", CalendarListId = 1, CurrentParentType = "CalendarList", ParentId = 1 };
			context.Users.Add(user);
			context.Calendars.Add(calendar);
			context.Tickets.Add(ticket);
			await context.SaveChangesAsync();

			var token = JwtTokenGenerator.GenerateToken(user, Array.Empty<CalendarPermission>());

			var dto = new { TicketId = ticket.Id, CalendarId = calendar.Id, Date = "2025-03-24" };
			var request = new HttpRequestMessage(HttpMethod.Post, "/api/Tickets/ScheduleTicket");
			request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
			request.Headers.Add("X-Calendar-Id", calendar.Id.ToString());
			request.Content = JsonContent.Create(dto);

			// Act
			var response = await _client.SendAsync(request);

			// Assert
			Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
		}

		[Fact]
		public async Task ScheduleTicket_ShouldReturn200AndSchedule_WhenUserHasPermission()
		{
			// Arrange
			using var scope = _factory.Services.CreateScope();
			var context = scope.ServiceProvider.GetRequiredService<Shalendar.Contexts.ShalendarDbContext>();

			var user = new User { Email = "perm@example.com", Username = "perm", PasswordHash = "pw" };
			var calendar = new Calendar { Name = "Schedulable" };
			var ticket = new Ticket { Name = "Schedulable", CalendarListId = 1, CurrentParentType = "CalendarList", ParentId = 1 };
			context.Users.Add(user);
			context.Calendars.Add(calendar);
			context.Tickets.Add(ticket);
			await context.SaveChangesAsync();

			var permission = new CalendarPermission { CalendarId = calendar.Id, UserId = user.Id, PermissionType = "write" };
			context.CalendarPermissions.Add(permission);
			await context.SaveChangesAsync();

			var token = JwtTokenGenerator.GenerateToken(user, new[] { permission });

			var dto = new ScheduleTicketDto
			{
				TicketId = ticket.Id,
				CalendarId = calendar.Id,
				Date = new DateTime(2025, 3, 24),
				StartTime = new TimeSpan(9, 0, 0),
				EndTime = new TimeSpan(10, 0, 0)
			};

			var request = new HttpRequestMessage(HttpMethod.Post, "/api/Tickets/ScheduleTicket");
			request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
			request.Headers.Add("X-Calendar-Id", calendar.Id.ToString());
			request.Content = JsonContent.Create(dto);

			// Act
			var response = await _client.SendAsync(request);

			// Assert
			Assert.Equal(HttpStatusCode.OK, response.StatusCode);

			using var verifyScope = _factory.Services.CreateScope();
			var verifyContext = verifyScope.ServiceProvider.GetRequiredService<Shalendar.Contexts.ShalendarDbContext>();
			var updated = await verifyContext.Tickets.FindAsync(ticket.Id);
			Assert.NotNull(updated);
			Assert.Equal("ScheduledList", updated!.CurrentParentType);
		}

		[Fact]
		public async Task CopyTicket_ShouldReturn403Forbidden_WhenUserLacksReadOrWrite()
		{
			// Arrange
			using var scope = _factory.Services.CreateScope();
			var context = scope.ServiceProvider.GetRequiredService<Shalendar.Contexts.ShalendarDbContext>();

			var user = new User { Email = "noaccess@example.com", Username = "noaccess", PasswordHash = "pw" };
			var calendar = new Calendar { Name = "NoAccess" };
			var ticket = new Ticket { Name = "Original", CalendarListId = 1, CurrentParentType = "CalendarList", ParentId = 1 };

			context.Users.Add(user);
			context.Calendars.Add(calendar);
			context.Tickets.Add(ticket);
			await context.SaveChangesAsync();

			var token = JwtTokenGenerator.GenerateToken(user, Array.Empty<CalendarPermission>());

			var request = new HttpRequestMessage(HttpMethod.Post, $"/api/Tickets/copy-ticket?ticketId={ticket.Id}&calendarId={calendar.Id}");
			request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
			request.Headers.Add("X-Calendar-Id", calendar.Id.ToString());

			// Act
			var response = await _client.SendAsync(request);

			// Assert
			Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
		}

		[Fact]
		public async Task CopyTicket_ShouldReturn200_WhenUserHasPermissions()
		{
			// Arrange
			using var scope = _factory.Services.CreateScope();
			var context = scope.ServiceProvider.GetRequiredService<Shalendar.Contexts.ShalendarDbContext>();

			var user = new User { Email = "access@example.com", Username = "access", PasswordHash = "pw" };
			var calendar = new Calendar { Name = "CopyAccess" };
			var ticket = new Ticket
			{
				Name = "CopyMe",
				CalendarListId = 1,
				CurrentParentType = "CalendarList",
				ParentId = 1
			};

			context.Users.Add(user);
			context.Calendars.Add(calendar);
			context.Tickets.Add(ticket);
			await context.SaveChangesAsync();

			var permissions = new[]
			{
				new CalendarPermission { CalendarId = calendar.Id, UserId = user.Id, PermissionType = "read" },
				new CalendarPermission { CalendarId = calendar.Id, UserId = user.Id, PermissionType = "write" }
			};
			context.CalendarPermissions.AddRange(permissions);
			await context.SaveChangesAsync();

			var token = JwtTokenGenerator.GenerateToken(user, permissions);

			var request = new HttpRequestMessage(HttpMethod.Post, $"/api/Tickets/copy-ticket?ticketId={ticket.Id}&calendarId={calendar.Id}");
			request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
			request.Headers.Add("X-Calendar-Id", calendar.Id.ToString());

			// Act
			var response = await _client.SendAsync(request);

			// Assert
			Assert.Equal(HttpStatusCode.OK, response.StatusCode);
			var text = await response.Content.ReadAsStringAsync();
			Assert.Contains("successfully copied", text);
		}

		#endregion



		#region Puts

		[Fact]
		public async Task ReorderTickets_ShouldReturn403Forbidden_WhenUserHasNoPermission()
		{
			// Arrange
			using var scope = _factory.Services.CreateScope();
			var context = scope.ServiceProvider.GetRequiredService<Shalendar.Contexts.ShalendarDbContext>();
			var user = new User { Email = "noperm@example.com", Username = "noperm", PasswordHash = "pw" };
			var calendar = new Calendar { Name = "TestCal" };
			var ticket = new Ticket { Name = "T1", CalendarListId = 1, CurrentPosition = 1, CurrentParentType = "CalendarList", ParentId = 1 };

			context.Users.Add(user);
			context.Calendars.Add(calendar);
			context.Tickets.Add(ticket);
			await context.SaveChangesAsync();

			var token = JwtTokenGenerator.GenerateToken(user, Array.Empty<CalendarPermission>());

			var updates = new[] { new { TicketId = ticket.Id, NewPosition = 3 } };
			var request = new HttpRequestMessage(HttpMethod.Put, "/api/Tickets/reorder");
			request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
			request.Headers.Add("X-Calendar-Id", calendar.Id.ToString());
			request.Content = JsonContent.Create(updates);

			// Act
			var response = await _client.SendAsync(request);

			// Assert
			Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
		}

		[Fact]
		public async Task ReorderTickets_ShouldUpdatePosition_WhenUserHasPermission()
		{
			// Arrange
			using var scope = _factory.Services.CreateScope();
			var context = scope.ServiceProvider.GetRequiredService<Shalendar.Contexts.ShalendarDbContext>();
			var user = new User { Email = "perm@example.com", Username = "perm", PasswordHash = "pw" };
			var calendar = new Calendar { Name = "Reorderable" };
			var ticket = new Ticket { Name = "T1", CalendarListId = 1, CurrentPosition = 1, CurrentParentType = "CalendarList", ParentId = 1 };

			context.Users.Add(user);
			context.Calendars.Add(calendar);
			context.Tickets.Add(ticket);
			await context.SaveChangesAsync();

			var permission = new CalendarPermission { CalendarId = calendar.Id, UserId = user.Id, PermissionType = "write" };
			context.CalendarPermissions.Add(permission);
			await context.SaveChangesAsync();

			var token = JwtTokenGenerator.GenerateToken(user, new[] { permission });

			var updates = new[] { new { TicketId = ticket.Id, NewPosition = 5 } };
			var request = new HttpRequestMessage(HttpMethod.Put, "/api/Tickets/reorder");
			request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
			request.Headers.Add("X-Calendar-Id", calendar.Id.ToString());
			request.Content = JsonContent.Create(updates);

			// Act
			var response = await _client.SendAsync(request);

			// Assert
			Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

			using var verifyScope = _factory.Services.CreateScope();
			var verifyContext = verifyScope.ServiceProvider.GetRequiredService<Shalendar.Contexts.ShalendarDbContext>();
			var updated = await verifyContext.Tickets.FindAsync(ticket.Id);
			Assert.Equal(5, updated!.CurrentPosition);
		}

		[Fact]
		public async Task MoveTicketToCalendar_ShouldReturn403Forbidden_WhenUserHasNoPermission()
		{
			// Arrange
			using var scope = _factory.Services.CreateScope();
			var context = scope.ServiceProvider.GetRequiredService<Shalendar.Contexts.ShalendarDbContext>();
			var user = new User { Email = "noperm2@example.com", Username = "noperm2", PasswordHash = "pw" };
			var calendar = new Calendar { Name = "MoveTest" };
			var ticket = new Ticket { Name = "MoveMe", CalendarListId = 1, CurrentParentType = "ScheduledList", ParentId = 1, StartTime = TimeSpan.FromHours(9) };

			context.Users.Add(user);
			context.Calendars.Add(calendar);
			context.Tickets.Add(ticket);
			await context.SaveChangesAsync();

			var token = JwtTokenGenerator.GenerateToken(user, Array.Empty<CalendarPermission>());

			var request = new HttpRequestMessage(HttpMethod.Put, $"/api/Tickets/move-to-calendar/{ticket.Id}");
			request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
			request.Headers.Add("X-Calendar-Id", calendar.Id.ToString());

			// Act
			var response = await _client.SendAsync(request);

			// Assert
			Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
		}

		[Fact]
		public async Task MoveTicketToCalendar_ShouldUpdateParent_WhenUserHasPermission()
		{
			// Arrange
			using var scope = _factory.Services.CreateScope();
			var context = scope.ServiceProvider.GetRequiredService<Shalendar.Contexts.ShalendarDbContext>();
			var user = new User { Email = "perm2@example.com", Username = "perm2", PasswordHash = "pw" };
			var calendar = new Calendar { Name = "Movable" };
			context.Users.Add(user);
			context.Calendars.Add(calendar);
			await context.SaveChangesAsync();

			var day = new Day { CalendarId = calendar.Id, Date = new DateTime(2025, 3, 25) };
			context.Days.Add(day);
			await context.SaveChangesAsync();

			var ticket = new Ticket
			{
				Name = "MoveMe",
				CalendarListId = 99,
				ParentId = day.Id,
				CurrentParentType = "ScheduledList",
				StartTime = TimeSpan.FromHours(8),
				EndTime = TimeSpan.FromHours(10)
			};
			context.Tickets.Add(ticket);
			await context.SaveChangesAsync();

			var permission = new CalendarPermission { CalendarId = calendar.Id, UserId = user.Id, PermissionType = "write" };
			context.CalendarPermissions.Add(permission);
			await context.SaveChangesAsync();

			var token = JwtTokenGenerator.GenerateToken(user, new[] { permission });

			var request = new HttpRequestMessage(HttpMethod.Put, $"/api/Tickets/move-to-calendar/{ticket.Id}");
			request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
			request.Headers.Add("X-Calendar-Id", calendar.Id.ToString());

			// Act
			var response = await _client.SendAsync(request);

			// Assert
			Assert.Equal(HttpStatusCode.OK, response.StatusCode);

			using var verifyScope = _factory.Services.CreateScope();
			var verifyContext = verifyScope.ServiceProvider.GetRequiredService<Shalendar.Contexts.ShalendarDbContext>();
			var moved = await verifyContext.Tickets.FindAsync(ticket.Id);
			Assert.Equal("CalendarList", moved!.CurrentParentType);
			Assert.Equal(ticket.CalendarListId, moved.ParentId);
			Assert.Null(moved.StartTime);
			Assert.Null(moved.EndTime);
		}

		[Fact]
		public async Task UpdateTicket_ShouldReturn403_WhenUserHasNoPermission()
		{
			// Arrange
			using var scope = _factory.Services.CreateScope();
			var context = scope.ServiceProvider.GetRequiredService<Shalendar.Contexts.ShalendarDbContext>();
			var user = new User { Email = "noedit@example.com", Username = "noedit", PasswordHash = "pw" };
			var calendar = new Calendar { Name = "NoPermEdit" };
			var day = new Day { CalendarId = calendar.Id, Date = new DateTime(2025, 3, 24) };
			var ticket = new Ticket { Name = "Ticket", ParentId = day.Id, CurrentParentType = "TodoList", CalendarListId = 1 };
			context.Users.Add(user);
			context.Calendars.Add(calendar);
			context.Days.Add(day);
			context.Tickets.Add(ticket);
			await context.SaveChangesAsync();

			var token = JwtTokenGenerator.GenerateToken(user, Array.Empty<CalendarPermission>());

			var dto = new { Id = ticket.Id, Name = "Changed", Description = "New", Priority = 2 };
			var request = new HttpRequestMessage(HttpMethod.Put, "/api/Tickets/updateTicket");
			request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
			request.Headers.Add("X-Calendar-Id", calendar.Id.ToString());
			request.Content = JsonContent.Create(dto);

			// Act
			var response = await _client.SendAsync(request);

			// Assert
			Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
		}

		[Fact]
		public async Task UpdateTicket_ShouldModifyFields_WhenUserHasPermission()
		{
			// Arrange
			using var scope = _factory.Services.CreateScope();
			var context = scope.ServiceProvider.GetRequiredService<Shalendar.Contexts.ShalendarDbContext>();
			var user = new User { Email = "edit@example.com", Username = "edit", PasswordHash = "pw" };
			var calendar = new Calendar { Name = "Editable" };
			var day = new Day { CalendarId = calendar.Id, Date = new DateTime(2025, 3, 24) };
			context.Users.Add(user);
			context.Calendars.Add(calendar);
			context.Days.Add(day);
			await context.SaveChangesAsync();

			var ticket = new Ticket
			{
				Name = "Old Name",
				Description = "Old Desc",
				Priority = 1,
				ParentId = day.Id,
				CurrentParentType = "TodoList",
				CalendarListId = 1
			};
			context.Tickets.Add(ticket);
			await context.SaveChangesAsync();

			var permission = new CalendarPermission { CalendarId = calendar.Id, UserId = user.Id, PermissionType = "write" };
			context.CalendarPermissions.Add(permission);
			await context.SaveChangesAsync();

			var token = JwtTokenGenerator.GenerateToken(user, new[] { permission });
			var dto = new
			{
				Id = ticket.Id,
				Name = "New Name",
				Description = "New Desc",
				Priority = 2,
				StartTime = "08:00:00",
				EndTime = "09:00:00"
			};

			var request = new HttpRequestMessage(HttpMethod.Put, "/api/Tickets/updateTicket");
			request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
			request.Headers.Add("X-Calendar-Id", calendar.Id.ToString());
			request.Content = JsonContent.Create(dto);

			// Act
			var response = await _client.SendAsync(request);

			// Assert
			Assert.Equal(HttpStatusCode.OK, response.StatusCode);

			using var verifyScope = _factory.Services.CreateScope();
			var verifyContext = verifyScope.ServiceProvider.GetRequiredService<Shalendar.Contexts.ShalendarDbContext>();
			var updated = await verifyContext.Tickets.FindAsync(ticket.Id);
			Assert.Equal("New Name", updated!.Name);
			Assert.Equal("New Desc", updated.Description);
			Assert.Equal(2, updated.Priority);
			Assert.Equal(TimeSpan.FromHours(8), updated.StartTime);
			Assert.Equal(TimeSpan.FromHours(9), updated.EndTime);
			Assert.Equal("ScheduledList", updated.CurrentParentType);
		}

		[Fact]
		public async Task UpdateTicketCompleted_ShouldReturn403_WhenUserHasNoPermission()
		{
			// Arrange
			using var scope = _factory.Services.CreateScope();
			var context = scope.ServiceProvider.GetRequiredService<Shalendar.Contexts.ShalendarDbContext>();
			var user = new User { Email = "nodone@example.com", Username = "nodone", PasswordHash = "pw" };
			var calendar = new Calendar { Name = "NoPermDone" };
			var day = new Day { CalendarId = calendar.Id, Date = new DateTime(2025, 3, 24) };
			var ticket = new Ticket { Name = "Incomplete", ParentId = day.Id, CurrentParentType = "TodoList", CalendarListId = 1 };
			context.Users.Add(user);
			context.Calendars.Add(calendar);
			context.Days.Add(day);
			context.Tickets.Add(ticket);
			await context.SaveChangesAsync();

			var token = JwtTokenGenerator.GenerateToken(user, Array.Empty<CalendarPermission>());

			var request = new HttpRequestMessage(HttpMethod.Put, $"/api/Tickets/updateTicketCompleted?ticketId={ticket.Id}&isCompleted=true");
			request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
			request.Headers.Add("X-Calendar-Id", calendar.Id.ToString());

			// Act
			var response = await _client.SendAsync(request);

			// Assert
			Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
		}

		[Fact]
		public async Task UpdateTicketCompleted_ShouldMarkDone_WhenUserHasPermission()
		{
			// Arrange
			using var scope = _factory.Services.CreateScope();
			var context = scope.ServiceProvider.GetRequiredService<Shalendar.Contexts.ShalendarDbContext>();
			var user = new User { Email = "done@example.com", Username = "done", PasswordHash = "pw" };
			var calendar = new Calendar { Name = "DoneCalendar" };
			var day = new Day { CalendarId = calendar.Id, Date = new DateTime(2025, 3, 24) };

			context.Users.Add(user);
			context.Calendars.Add(calendar);
			context.Days.Add(day);
			await context.SaveChangesAsync();

			var ticket = new Ticket
			{
				Name = "Finish me",
				ParentId = day.Id,
				CurrentParentType = "TodoList",
				CalendarListId = 1
			};

			context.Tickets.Add(ticket);
			await context.SaveChangesAsync();

			var permission = new CalendarPermission { CalendarId = calendar.Id, UserId = user.Id, PermissionType = "write" };
			context.CalendarPermissions.Add(permission);
			await context.SaveChangesAsync();

			var token = JwtTokenGenerator.GenerateToken(user, new[] { permission });

			var request = new HttpRequestMessage(HttpMethod.Put, $"/api/Tickets/updateTicketCompleted?ticketId={ticket.Id}&isCompleted=true");
			request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
			request.Headers.Add("X-Calendar-Id", calendar.Id.ToString());

			// Act
			var response = await _client.SendAsync(request);

			// Assert
			Assert.Equal(HttpStatusCode.OK, response.StatusCode);

			using var verifyScope = _factory.Services.CreateScope();
			var verifyContext = verifyScope.ServiceProvider.GetRequiredService<Shalendar.Contexts.ShalendarDbContext>();
			var completed = await verifyContext.Tickets.FindAsync(ticket.Id);
			Assert.True(completed!.IsCompleted);
		}

		[Fact]
		public async Task ChangeTicketDate_ShouldReturn403_WhenUserHasNoPermission()
		{
			// Arrange
			using var scope = _factory.Services.CreateScope();
			var context = scope.ServiceProvider.GetRequiredService<Shalendar.Contexts.ShalendarDbContext>();
			var user = new User { Email = "nomove@example.com", Username = "nomove", PasswordHash = "pw" };
			var calendar = new Calendar { Name = "NoPermMove" };
			var day = new Day { CalendarId = calendar.Id, Date = new DateTime(2025, 3, 24) };
			var ticket = new Ticket { Name = "Don't move", ParentId = day.Id, CurrentParentType = "ScheduledList", CalendarListId = 1 };
			context.Users.Add(user);
			context.Calendars.Add(calendar);
			context.Days.Add(day);
			context.Tickets.Add(ticket);
			await context.SaveChangesAsync();

			var token = JwtTokenGenerator.GenerateToken(user, Array.Empty<CalendarPermission>());

			var request = new HttpRequestMessage(HttpMethod.Put, $"/api/Tickets/changeDate/{ticket.Id}");
			request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
			request.Headers.Add("X-Calendar-Id", calendar.Id.ToString());
			request.Content = JsonContent.Create("2025-03-26");

			// Act
			var response = await _client.SendAsync(request);

			// Assert
			Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
		}

		[Fact]
		public async Task ChangeTicketDate_ShouldUpdateDate_WhenUserHasPermission()
		{
			// Arrange
			using var scope = _factory.Services.CreateScope();
			var context = scope.ServiceProvider.GetRequiredService<Shalendar.Contexts.ShalendarDbContext>();
			var user = new User { Email = "mover@example.com", Username = "mover", PasswordHash = "pw" };
			var calendar = new Calendar { Name = "MoverCal" };

			context.Users.Add(user);
			context.Calendars.Add(calendar);
			await context.SaveChangesAsync();

			var oldDay = new Day { CalendarId = calendar.Id, Date = new DateTime(2025, 3, 24) };
			context.Days.Add(oldDay);
			await context.SaveChangesAsync();

			var ticket = new Ticket
			{
				Name = "Move me",
				ParentId = oldDay.Id,
				CurrentParentType = "ScheduledList",
				CalendarListId = 1
			};
			context.Tickets.Add(ticket);
			await context.SaveChangesAsync();

			var permission = new CalendarPermission { CalendarId = calendar.Id, UserId = user.Id, PermissionType = "write" };
			context.CalendarPermissions.Add(permission);
			await context.SaveChangesAsync();

			var token = JwtTokenGenerator.GenerateToken(user, new[] { permission });

			var request = new HttpRequestMessage(HttpMethod.Put, $"/api/Tickets/changeDate/{ticket.Id}");
			request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
			request.Headers.Add("X-Calendar-Id", calendar.Id.ToString());
			request.Content = JsonContent.Create("2025-03-26");

			// Act
			var response = await _client.SendAsync(request);

			// Assert
			Assert.Equal(HttpStatusCode.OK, response.StatusCode);

			using var verifyScope = _factory.Services.CreateScope();
			var verifyContext = verifyScope.ServiceProvider.GetRequiredService<Shalendar.Contexts.ShalendarDbContext>();
			var updated = await verifyContext.Tickets.FindAsync(ticket.Id);
			var newDay = await verifyContext.Days.FirstOrDefaultAsync(d => d.Id == updated!.ParentId);
			Assert.Equal(new DateTime(2025, 3, 26), newDay!.Date);
		}



		#endregion



		#region Deletes

		[Fact]
		public async Task DeleteTicket_ShouldReturnForbidden_WhenUserLacksPermission()
		{
			// Arrange
			using var scope = _factory.Services.CreateScope();
			var context = scope.ServiceProvider.GetRequiredService<Shalendar.Contexts.ShalendarDbContext>();

			var user = new User { Email = "noaccess@example.com", Username = "noaccess", PasswordHash = "pw" };
			var calendar = new Calendar { Name = "NoDelete" };
			var day = new Day { CalendarId = calendar.Id, Date = new DateTime(2025, 3, 24) };
			context.Users.Add(user);
			context.Calendars.Add(calendar);
			context.Days.Add(day);
			await context.SaveChangesAsync();

			var ticket = new Ticket
			{
				Name = "Try delete",
				ParentId = day.Id,
				CurrentParentType = "TodoList",
				CalendarListId = 1
			};
			context.Tickets.Add(ticket);
			await context.SaveChangesAsync();

			var token = JwtTokenGenerator.GenerateToken(user, Array.Empty<CalendarPermission>());

			var request = new HttpRequestMessage(HttpMethod.Delete, $"/api/Tickets/{ticket.Id}");
			request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
			request.Headers.Add("X-Calendar-Id", calendar.Id.ToString());

			// Act
			var response = await _client.SendAsync(request);

			// Assert
			Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

			using var verifyScope = _factory.Services.CreateScope();
			var verifyContext = verifyScope.ServiceProvider.GetRequiredService<Shalendar.Contexts.ShalendarDbContext>();
			var stillThere = await verifyContext.Tickets.FindAsync(ticket.Id);
			Assert.NotNull(stillThere);
		}

		[Fact]
		public async Task DeleteTicket_ShouldRemove_WhenUserHasPermission()
		{
			// Arrange
			using var scope = _factory.Services.CreateScope();
			var context = scope.ServiceProvider.GetRequiredService<Shalendar.Contexts.ShalendarDbContext>();

			var user = new User { Email = "deleteme@example.com", Username = "deleteme", PasswordHash = "pw" };
			var calendar = new Calendar { Name = "Deletable" };
			var day = new Day { CalendarId = calendar.Id, Date = new DateTime(2025, 3, 24) };
			context.Users.Add(user);
			context.Calendars.Add(calendar);
			context.Days.Add(day);
			await context.SaveChangesAsync();

			var ticket = new Ticket
			{
				Name = "To be deleted",
				ParentId = day.Id,
				CurrentParentType = "TodoList",
				CalendarListId = 1
			};
			context.Tickets.Add(ticket);
			await context.SaveChangesAsync();

			var permission = new CalendarPermission { CalendarId = calendar.Id, UserId = user.Id, PermissionType = "write" };
			context.CalendarPermissions.Add(permission);
			await context.SaveChangesAsync();

			var token = JwtTokenGenerator.GenerateToken(user, new[] { permission });

			var request = new HttpRequestMessage(HttpMethod.Delete, $"/api/Tickets/{ticket.Id}");
			request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
			request.Headers.Add("X-Calendar-Id", calendar.Id.ToString());

			// Act
			var response = await _client.SendAsync(request);

			// Assert
			Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

			using var verifyScope = _factory.Services.CreateScope();
			var verifyContext = verifyScope.ServiceProvider.GetRequiredService<Shalendar.Contexts.ShalendarDbContext>();
			var deleted = await verifyContext.Tickets.FindAsync(ticket.Id);
			Assert.Null(deleted);
		}

		#endregion
	}
}
