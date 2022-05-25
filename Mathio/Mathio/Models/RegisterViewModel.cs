using System.ComponentModel.DataAnnotations;

namespace Mathio.Models;

public class RegisterViewModel
{
    [Required]
    [EmailAddress]
    public string? Email { get; set; }
    [Required]
    public string? Password { get; init; }
    [Required]
    public string? UserName { get; set; }
}