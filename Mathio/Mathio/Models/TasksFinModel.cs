using System.ComponentModel.DataAnnotations;
using Google.Cloud.Firestore;

namespace Mathio.Models;

[FirestoreData]
public class TasksFinModel
{
    [FirestoreDocumentId]
    public string ID { get; set; }
    [Required]
    [FirestoreDocumentId]
    public DocumentReference Task { get; set; }
    [FirestoreProperty]
    public int Score { get; set; }
}