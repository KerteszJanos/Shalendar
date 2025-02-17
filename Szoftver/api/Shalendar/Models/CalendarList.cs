using System.ComponentModel.DataAnnotations;

namespace Shalendar.Models
{
	public class CalendarList
	{
		[Key]
		public int Id { get; set; }

		[Required]
		[StringLength(255)]
		public string Name { get; set; }

		[StringLength(50)]
		public string? Color { get; set; }

		[Required]
		public int CalendarId { get; set; }
	}
}
