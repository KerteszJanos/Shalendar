using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Shalendar.Models;
using Shalendar.Tests.Integration.Helpers;
using Xunit;

namespace Shalendar.Tests.Integration.Controllers
{
	public class CalendarListControllerTests : IClassFixture<CustomWebApplicationFactory>
	{
		private readonly HttpClient _client;
		private readonly CustomWebApplicationFactory _factory;

		public CalendarListControllerTests(CustomWebApplicationFactory factory)
		{
			_factory = factory;
			_client = factory.CreateClient();
		}

		public static IEnumerable<object[]> UnauthorizedEndpoints => new List<object[]>
		{
			//All endpoints from CalendarListsController
			new object[] { HttpMethod.Get, "/api/CalendarLists/calendar/1", true },
			new object[] { HttpMethod.Post, "/api/CalendarLists", false },
			new object[] { HttpMethod.Put, "/api/CalendarLists/1", false },
			new object[] { HttpMethod.Delete, "/api/CalendarLists/1", false },
		};

		[Theory]
		[MemberData(nameof(UnauthorizedEndpoints))]
		public async Task Endpoint_ShouldReturn401_WhenTokenIsInvalid(HttpMethod method, string url, bool includeCalendarHeader)
		{
			var request = new HttpRequestMessage(method, url);
			request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "invalid.token");

			if (includeCalendarHeader)
			{
				request.Headers.Add("X-Calendar-Id", "1");
			}

			var response = await _client.SendAsync(request);

			Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
		}

		#region Gets

		[Fact]
		public async Task GetCalendarListsByCalendarId_ShouldReturn200_WhenUserHasReadPermission()
		{
			// Arrange
			using var scope = _factory.Services.CreateScope();
			var context = scope.ServiceProvider.GetRequiredService<Shalendar.Contexts.ShalendarDbContext>();

			var user = new User { Email = "reader@example.com", Username = "reader", PasswordHash = "pw" };
			var calendar = new Calendar { Name = "My Calendar" };

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
				Name = "Test List",
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

			var response = await _client.SendAsync(request);


			// Assert
			Assert.Equal(HttpStatusCode.OK, response.StatusCode);

			var responseData = await response.Content.ReadFromJsonAsync<JsonElement>();
			Assert.True(responseData.ValueKind == JsonValueKind.Array);
		}

		#endregion

		#region Posts
		#endregion

		#region Puts
		#endregion

		#region Deletes
		#endregion
	}
}
