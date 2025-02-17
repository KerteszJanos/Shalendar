using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Globalization;

namespace Shalendar.Models
{
	public class User
	{
		[Key]
		public int Id { get; set; }

		[Required]
		[MaxLength(255)]
		public string Username { get; set; } = string.Empty;

		[Required]
		[MaxLength(255)]
		public string PasswordHash { get; set; } = string.Empty;

		[ForeignKey("DefaultCalendar")]
		public int? DefaultCalendarId { get; set; }
	}
}
