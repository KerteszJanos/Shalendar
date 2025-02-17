using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace Shalendar.Models
{
	public class CalendarPermission
	{
		[Key]
		public int Id { get; set; }

		public int CalendarId { get; set; }

		public int UserId { get; set; }

		[Required]
		[MaxLength(50)]
		public string PermissionType { get; set; }
	}
}
