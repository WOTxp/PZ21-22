using System.ComponentModel.DataAnnotations;

namespace Mathio.Models;

public class RegisterViewModel
{
    [Required]
    [EmailAddress]
    public string? Email { get; init; }
    [Required]
    public string? Password { get; init; }
    [Required]
    [RegularExpression("\\w*", ErrorMessage = "Dozwolone są tylko litery, cyfry i znak _")]
    public string? UserName { get; init; }
}