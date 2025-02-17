using Microsoft.EntityFrameworkCore;
using Shalendar.Contexts;

namespace Shalendar
{
	public class Program
	{
		public static void Main(string[] args)
		{
			var builder = WebApplication.CreateBuilder(args);

			// CORS Policy hozzáadása
			builder.Services.AddCors(options =>
			{
				options.AddPolicy("AllowSpecificOrigin",
					policy => policy.WithOrigins("http://localhost:5173")
									.AllowAnyHeader()
									.AllowAnyMethod()
									.AllowCredentials());
			});

			builder.Services.AddDbContext<ShalendarDbContext>(options =>
				options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

			// Add services to the container.
			builder.Services.AddControllers();
			builder.Services.AddEndpointsApiExplorer();
			builder.Services.AddSwaggerGen();

			var app = builder.Build();

			// Configure the HTTP request pipeline.
			if (app.Environment.IsDevelopment())
			{
				app.UseSwagger();
				app.UseSwaggerUI();
			}

			app.UseHttpsRedirection();

			// CORS engedélyezése az alkalmazásban
			app.UseCors("AllowSpecificOrigin");

			app.UseAuthorization();

			app.MapControllers();

			app.Run();
		}
	}
}
