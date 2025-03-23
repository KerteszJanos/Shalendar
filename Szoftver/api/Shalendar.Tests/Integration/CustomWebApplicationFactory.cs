using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shalendar.Contexts;

namespace Shalendar.Tests.Integration
{
	public class CustomWebApplicationFactory : WebApplicationFactory<Program>
	{
		private const string InMemoryDbName = "ShalendarTestDb";

		protected override void ConfigureWebHost(IWebHostBuilder builder)
		{
			builder.ConfigureAppConfiguration((context, configBuilder) =>
			{
				var testSettings = new Dictionary<string, string>
			{
				{ "JwtSettings:Issuer", "ShalendarIssuer" },
				{ "JwtSettings:Audience", "ShalendarAudience" },
				{ "JwtSettings:SecretKey", "MySuperSecretKeyForJwtAuth123!TEST1234" }
			};

				configBuilder.AddInMemoryCollection(testSettings);
			});

			builder.ConfigureServices(services =>
			{
				var dbContextDescriptors = services
					.Where(d => d.ServiceType == typeof(ShalendarDbContext)
							|| d.ServiceType == typeof(DbContextOptions<ShalendarDbContext>)
							|| d.ServiceType.FullName?.Contains("ShalendarDbContext") == true)
					.ToList();

				foreach (var descriptor in dbContextDescriptors)
				{
					services.Remove(descriptor);
				}

				services.AddDbContext<ShalendarDbContext>(options =>
				{
					options.UseInMemoryDatabase(InMemoryDbName);
				});
			});
		}
	}
}
