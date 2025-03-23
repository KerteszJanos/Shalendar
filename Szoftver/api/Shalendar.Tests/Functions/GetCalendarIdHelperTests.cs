using Microsoft.AspNetCore.Http;
using Shalendar.Functions;
using Xunit;
using FluentAssertions;

namespace Shalendar.Tests.Functions
{
	public class GetCalendarIdHelperTests
	{
		[Fact]
		public void TryGetCalendarId_ShouldReturnTrue_WhenHeaderIsValid()
		{
			var helper = new GetCalendarIdHelper();
			var context = new DefaultHttpContext();
			context.Request.Headers["X-Calendar-Id"] = "123";

			var result = helper.TryGetCalendarId(context, out var calendarId);

			result.Should().BeTrue();
			calendarId.Should().Be(123);
		}

		[Fact]
		public void TryGetCalendarId_ShouldReturnFalse_WhenHeaderIsMissing()
		{
			var helper = new GetCalendarIdHelper();
			var context = new DefaultHttpContext();

			var result = helper.TryGetCalendarId(context, out var calendarId);

			result.Should().BeFalse();
			calendarId.Should().Be(0);
		}

		[Fact]
		public void TryGetCalendarId_ShouldReturnFalse_WhenHeaderIsNotInteger()
		{
			var helper = new GetCalendarIdHelper();
			var context = new DefaultHttpContext();
			context.Request.Headers["X-Calendar-Id"] = "not-a-number";

			var result = helper.TryGetCalendarId(context, out var calendarId);

			result.Should().BeFalse();
			calendarId.Should().Be(0);
		}
	}
}
