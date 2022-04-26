using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Google.Cloud.Firestore;


namespace Mathio.Models;

[FirestoreData]
public class UserModel
{
    [FirestoreDocumentId]
    public string ID { get; set; }
    public string UserName { get; set; }
    [Required]
    [EmailAddress]
    public string Email { get; set; }
    [Required]
    public string Password { get; set; }
    
    [FirestoreProperty]
    public string Type { get; set; }
    [FirestoreProperty]
    public int Points { get; set; }


    
}