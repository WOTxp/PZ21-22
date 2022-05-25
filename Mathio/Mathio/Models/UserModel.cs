using System.ComponentModel.DataAnnotations;
using Google.Cloud.Firestore;


namespace Mathio.Models;

[FirestoreData]
public class UserModel
{
    [FirestoreDocumentId]
    public string? Id { get; set; }
    public string? UserName { get; set; }
    [Required]
    [EmailAddress]
    public string? Email { get; set; }
    [FirestoreProperty]
    public string? Type { get; set; }
    [FirestoreProperty]
    public int Points { get; set; }
    
    public ICollection<TasksModel>? FinishedTasks { get; set; }



}