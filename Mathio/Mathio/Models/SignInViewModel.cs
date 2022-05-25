using System.ComponentModel.DataAnnotations;

namespace Mathio.Models;

public class SignInViewModel
{
    [Required]
    [EmailAddress]
    public string? Email { get; init; }
    [Required]
    public string? Password { get; init; }

}