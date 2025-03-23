using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Shalendar.Contexts;
using Shalendar.Functions;
using Shalendar.Models;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Xunit;
using FluentAssertions;

namespace Shalendar.Tests.Unit.Functions
{
    public class JwtHelperTests
    {
        private ShalendarDbContext CreateDbContext(string dbName)
        {
            var options = new DbContextOptionsBuilder<ShalendarDbContext>()
                .UseInMemoryDatabase(dbName)
                .Options;

            return new ShalendarDbContext(options);
        }

        private HttpContext CreateHttpContextWithClaims(int? userId = null, string calendarIdHeader = null)
        {
            var context = new DefaultHttpContext();
            if (userId.HasValue)
            {
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, userId.Value.ToString())
                };
                var identity = new ClaimsIdentity(claims, "TestAuth");
                context.User = new ClaimsPrincipal(identity);
            }

            if (calendarIdHeader != null)
            {
                context.Request.Headers["X-Calendar-Id"] = calendarIdHeader;
            }

            return context;
        }

        [Fact]
        public async Task HasCalendarPermission_ShouldReturnFalse_WhenUserIdMissing()
        {
            var context = CreateDbContext(nameof(HasCalendarPermission_ShouldReturnFalse_WhenUserIdMissing));
            var helper = new JwtHelper(context);

            var httpContext = CreateHttpContextWithClaims(null, "1");

            var result = await helper.HasCalendarPermission(httpContext, "read");

            result.Should().BeFalse();
        }

        [Fact]
        public async Task HasCalendarPermission_ShouldReturnFalse_WhenCalendarIdHeaderMissing()
        {
            var context = CreateDbContext(nameof(HasCalendarPermission_ShouldReturnFalse_WhenCalendarIdHeaderMissing));
            var helper = new JwtHelper(context);

            var httpContext = CreateHttpContextWithClaims(1, null);

            var result = await helper.HasCalendarPermission(httpContext, "read");

            result.Should().BeFalse();
        }

        [Fact]
        public async Task HasCalendarPermission_ShouldReturnFalse_WhenNoPermissionFound()
        {
            var context = CreateDbContext(nameof(HasCalendarPermission_ShouldReturnFalse_WhenNoPermissionFound));
            var helper = new JwtHelper(context);

            var httpContext = CreateHttpContextWithClaims(1, "42");

            var result = await helper.HasCalendarPermission(httpContext, "read");

            result.Should().BeFalse();
        }

        [Theory]
        [InlineData("owner", "read", true)]
        [InlineData("owner", "write", true)]
        [InlineData("owner", "owner", true)]
        [InlineData("write", "read", true)]
        [InlineData("write", "write", true)]
        [InlineData("write", "owner", false)]
        [InlineData("read", "read", true)]
        [InlineData("read", "write", false)]
        public async Task HasCalendarPermission_ShouldMatchPermissionLogic_HeaderVersion(string actual, string required, bool expected)
        {
            var db = CreateDbContext(nameof(HasCalendarPermission_ShouldMatchPermissionLogic_HeaderVersion) + actual + required);
            db.CalendarPermissions.Add(new CalendarPermission
            {
                UserId = 1,
                CalendarId = 100,
                PermissionType = actual
            });
            await db.SaveChangesAsync();

            var httpContext = CreateHttpContextWithClaims(1, "100");
            var helper = new JwtHelper(db);

            var result = await helper.HasCalendarPermission(httpContext, required);
            result.Should().Be(expected);
        }

        [Theory]
        [InlineData("owner", "read", true)]
        [InlineData("owner", "write", true)]
        [InlineData("owner", "owner", true)]
        [InlineData("write", "read", true)]
        [InlineData("write", "write", true)]
        [InlineData("write", "owner", false)]
        [InlineData("read", "read", true)]
        [InlineData("read", "write", false)]
        public async Task HasCalendarPermission_ShouldMatchPermissionLogic_DirectVersion(string actual, string required, bool expected)
        {
            var db = CreateDbContext(nameof(HasCalendarPermission_ShouldMatchPermissionLogic_DirectVersion) + actual + required);
            db.CalendarPermissions.Add(new CalendarPermission
            {
                UserId = 1,
                CalendarId = 123,
                PermissionType = actual
            });
            await db.SaveChangesAsync();

            var httpContext = CreateHttpContextWithClaims(1);
            var helper = new JwtHelper(db);

            var result = await helper.HasCalendarPermission(httpContext, required, 123);
            result.Should().Be(expected);
        }
    }
}
