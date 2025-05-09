﻿using Microsoft.EntityFrameworkCore;
using System.Xml;
using Shalendar.Models;

namespace Shalendar.Contexts
{
    public class ShalendarDbContext : DbContext
    {
        public ShalendarDbContext(DbContextOptions<ShalendarDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<Calendar> Calendars { get; set; }
		public DbSet<CalendarPermission> CalendarPermissions { get; set; }
		public DbSet<CalendarList> CalendarLists { get; set; }

		public DbSet<Ticket> Tickets { get; set; }
		public DbSet<Day> Days { get; set; }

	}
}
