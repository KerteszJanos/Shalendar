using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Shalendar.Models
{
	public class Day
	{
		[Key]
		public int Id { get; set; }

		[Required]
		public DateTime Date { get; set; }

		[Required]
		public int CalendarId { get; set; }
	}
}
