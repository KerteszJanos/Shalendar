using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Shalendar.Contexts;
using Shalendar.Functions;
using System.Text;

namespace Shalendar
{
	public class Program
	{
		public static void Main(string[] args)
		{
			var builder = WebApplication.CreateBuilder(args);

			//Configure CORS policy to allow frontend communication
			builder.Services.AddCors(options =>
			{
				options.AddPolicy("AllowSpecificOrigin",
					policy => policy.WithOrigins("http://localhost:5173") // Allow frontend at localhost:5173
									.AllowAnyHeader()
									.AllowAnyMethod()
									.AllowCredentials());
			});

			// Add JWT Helper to DI container
			builder.Services.AddScoped<JwtHelper>();

			// Add Delete Calendar Helper to DI container
			builder.Services.AddScoped<DeleteCalendarHelper>();

			builder.Services.AddSignalR();

			// Add Copy Ticket Helper to DI container
			builder.Services.AddScoped<CopyTicketHelper>();

			//Add database context (SQL Server)
			builder.Services.AddDbContext<ShalendarDbContext>(options =>
				options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

			//Add JWT Authentication
			builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
				.AddJwtBearer(options =>
				{
					options.TokenValidationParameters = new TokenValidationParameters
					{
						ValidateIssuer = true,
						ValidateAudience = true,
						ValidateLifetime = true,
						ValidateIssuerSigningKey = true,
						ValidIssuer = builder.Configuration["JwtSettings:Issuer"], // JWT Issuer
						ValidAudience = builder.Configuration["JwtSettings:Audience"], // JWT Audience
						IssuerSigningKey = new SymmetricSecurityKey(
							Encoding.UTF8.GetBytes(builder.Configuration["JwtSettings:SecretKey"]) // Secret key for token validation
						)
					};
				});

			//Add authorization service
			builder.Services.AddAuthorization();

			//Add controllers and API documentation (Swagger)
			builder.Services.AddControllers();
			builder.Services.AddEndpointsApiExplorer();
			builder.Services.AddSwaggerGen();

			var app = builder.Build();

			//Configure the HTTP request pipeline
			if (app.Environment.IsDevelopment())
			{
				app.UseSwagger();
				app.UseSwaggerUI();
			}

			app.UseHttpsRedirection();

			//Enable CORS globally
			app.UseCors("AllowSpecificOrigin");

			//Enable authentication and authorization
			app.UseAuthentication();
			app.UseAuthorization();

			//Map controllers to handle API requests
			app.MapControllers();

			app.MapHub<CalendarHub>("/calendarHub");

			//Start the application
			app.Run();
		}
	}
}
