using System.ComponentModel.DataAnnotations;

namespace Mathio.Models;

public class ChangePasswordModel
{
    [Required(ErrorMessage = "To pole jest wymagane")]
    public string? OldPassword { get; init; }
    [Required(ErrorMessage = "To pole jest wymagane")]
    public string? NewPassword { get; init; }

}