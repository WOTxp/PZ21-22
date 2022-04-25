using System.ComponentModel.DataAnnotations;
using Google.Cloud.Firestore;

namespace Mathio.Models;

[FirestoreData]
public class NotesModel
{
    [FirestoreDocumentId]
    public string ID { get; set; }
    [Required]
    [FirestoreDocumentId]
    public string Task { get; set; }
    [Required]
    [FirestoreProperty]
    public string Content { get; set; }
}