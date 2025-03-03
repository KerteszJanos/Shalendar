using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Shalendar.Contexts;

public class JwtHelper
{
	private readonly ShalendarDbContext _context;

	public JwtHelper(ShalendarDbContext context)
	{
		_context = context;
	}

	public async Task<bool> HasCalendarPermission(HttpContext httpContext, string requiredPermissionLevel)
	{
		var userIdClaim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
		if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
		{
			return false;
		}

		if (!httpContext.Request.Headers.TryGetValue("X-Calendar-Id", out var calendarIdHeader) ||
			!int.TryParse(calendarIdHeader, out int calendarId))
		{
			return false;
		}

		var userPermission = await _context.CalendarPermissions
			.Where(p => p.CalendarId == calendarId && p.UserId == userId)
			.Select(p => p.PermissionType)
			.FirstOrDefaultAsync();

		if (string.IsNullOrEmpty(userPermission))
		{
			return false;
		}

		if (userPermission == "owner")
		{
			return true;
		}

		if ((requiredPermissionLevel == "read" || requiredPermissionLevel == "write") && userPermission == "write")
		{
			return true;
		}

		return userPermission == requiredPermissionLevel;
	}
}
