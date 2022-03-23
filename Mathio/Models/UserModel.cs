using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using System.Text;

namespace TestingApp.Models;

public class UserModel
{
    public string UserName { get; set; }
    [Required]
    [EmailAddress]
    public string Email { get; set; }
    
    [Required]
    public string Password { get; set; }
    


    
}