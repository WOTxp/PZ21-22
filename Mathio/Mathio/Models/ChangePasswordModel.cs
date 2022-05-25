using System.ComponentModel.DataAnnotations;

namespace Mathio.Models;

public class ChangePasswordModel
{
    [Required]
    public string? OldPassword { get; set; }
    [Required]
    public string? NewPassword { get; set; }

}