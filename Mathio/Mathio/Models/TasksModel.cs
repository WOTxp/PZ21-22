using System.ComponentModel.DataAnnotations;
using Google.Cloud.Firestore;

namespace Mathio.Models;

[FirestoreData]
public class TasksModel
{
    [FirestoreDocumentId]
    public string? ID { get; set; }
    [FirestoreProperty]
    public DocumentReference? Author { get; set; }
    [Required]
    [FirestoreProperty]
    public string Title { get; set; }
    [FirestoreProperty]
    public string Description { get; set; }
    [FirestoreProperty]
    public string Category { get; set; }
    public override string ToString()
    {
        return String.Format("Category: {0}\nTitle: {1}\nDescription: {2}\n",Category,Title,Description);
    }
}