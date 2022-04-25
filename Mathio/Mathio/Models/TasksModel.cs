using System.ComponentModel.DataAnnotations;
using Google.Cloud.Firestore;

namespace Mathio.Models;

[FirestoreData]
public class TasksModel
{
    [FirestoreDocumentId]
    public string ID { get; set; }
    [Required]
    [FirestoreProperty]
    public DocumentReference Author { get; set; }
    [Required]
    [FirestoreProperty]
    public string Title { get; set; }
    [FirestoreProperty]
    public string Description { get; set; }
    [FirestoreProperty]
    public string Category { get; set; }
    public override string ToString()
    {
        return String.Format("ID: {0}\nAuthor: {1}\nTitle: {2}\nDescription: {3}\n",ID,Author.ToString(),Title,Description);
    }
}