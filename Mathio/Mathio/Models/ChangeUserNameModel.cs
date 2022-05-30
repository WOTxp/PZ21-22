using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Mathio.Models;

public class ChangeUserNameModel
{
    [Required(ErrorMessage = "Nazwa użytkownika jest wymagana")]
    [RegularExpression("\\w*", ErrorMessage = "Dozwolone tylko litery, cyfry i znak _")]
    [DisplayName("Nazwa użytkownika")]
    public string? UserName { get; init; }
}