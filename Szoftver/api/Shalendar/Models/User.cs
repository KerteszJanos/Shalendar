using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

public class User
{
	[Key]
	public int Id { get; set; }

	[Required]
	[MaxLength(255)]
	public string Email { get; set; } = string.Empty;

	[Required]
	[MaxLength(255)]
	public string Username { get; set; } = string.Empty;

	[Required]
	[MaxLength(255)]
	[JsonPropertyName("password")]
	public string PasswordHash { get; set; } = string.Empty;

	public int? DefaultCalendarId { get; set; }
}
