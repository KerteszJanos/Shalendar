using System.ComponentModel.DataAnnotations;

namespace Shalendar.Models
{
	public class Calendar
	{
		[Key]
		public int Id { get; set; }

		[Required]
		[MaxLength(255)]
		public string Name { get; set; } = string.Empty;
	}
}
