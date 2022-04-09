using System.ComponentModel.DataAnnotations;
using Google.Cloud.Firestore;

namespace Mathio.Models;

[FirestoreData]
public class TasksModel
{
    [FirestoreDocumentId]
    public string ID { get; set; }
    [Required]
    [FirestoreDocumentId]
    public string Author { get; set; }
    [Required]
    [FirestoreProperty]
    public string Title { get; set; }
    [FirestoreProperty]
    public string Description { get; set; }
}