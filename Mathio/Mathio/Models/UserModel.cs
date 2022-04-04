using System.ComponentModel;
using System.ComponentModel.DataAnnotations;


namespace Mathio.Models;

public class UserModel
{
    public string ID { get; set; }
    public string UserName { get; set; }
    [Required]
    [EmailAddress]
    public string Email { get; set; }
    [Required]
    public string Password { get; set; }
    public string Type { get; set; }
    


    
}