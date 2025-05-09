﻿using System.ComponentModel.DataAnnotations;

namespace Shalendar.Models.Dtos
{
    public class UpdateTicketDto
    {
        [Required]
        public int Id { get; set; }

        [Required]
        [StringLength(255)]
        public string Name { get; set; }

        public string? Description { get; set; }

        public int? Priority { get; set; }

        public TimeSpan? StartTime { get; set; }

        public TimeSpan? EndTime { get; set; }
    }
}
