using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Shalendar.Models;

namespace Shalendar.Tests.Integration.Helpers
{
	public static class JwtTokenGenerator
	{
		public static string GenerateToken(User user, IEnumerable<CalendarPermission> permissions)
		{
			var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("MySuperSecretKeyForJwtAuth123!TEST1234"));
			var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

			var claims = new List<Claim>
			{
				new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
				new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString(), ClaimValueTypes.Integer32),
				new Claim(JwtRegisteredClaimNames.Email, user.Email),
				new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
			};

			foreach (var permission in permissions)
			{
				claims.Add(new Claim("CalendarPermission", $"{permission.CalendarId}:{permission.PermissionType}"));
			}

			var token = new JwtSecurityToken(
				issuer: "ShalendarIssuer",
				audience: "ShalendarAudience",
				claims: claims,
				expires: DateTime.UtcNow.AddHours(1),
				signingCredentials: credentials
			);

			return new JwtSecurityTokenHandler().WriteToken(token);
		}
	}
}
