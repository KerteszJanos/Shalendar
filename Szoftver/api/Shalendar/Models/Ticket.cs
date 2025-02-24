namespace Shalendar.Models
{
	using System;
	using System.ComponentModel.DataAnnotations;
	using System.ComponentModel.DataAnnotations.Schema;

	public class Ticket
	{
		[Key]
		public int Id { get; set; }

		[Required]
		[StringLength(255)]
		public string Name { get; set; }

		public string? Description { get; set; }

		public int? CurrentPosition { get; set; }

		public TimeSpan? StartTime { get; set; }

		public TimeSpan? EndTime { get; set; }

		public int? Priority { get; set; }

		public int CalendarListId { get; set; }

		[StringLength(50)]
		public string CurrentParentType { get; set; }

		[Required]
		public int ParentId { get; set; }
	}

}
