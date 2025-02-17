using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace Shalendar.Models
{
	public class CalendarPermission
	{
		[Key]
		public int Id { get; set; }

		[ForeignKey("Calendar")]
		public int CalendarId { get; set; }

		[ForeignKey("User")]
		public int UserId { get; set; }

		[Required]
		[MaxLength(50)]
		public string PermissionType { get; set; }
	}
}
