using System.ComponentModel.DataAnnotations;
using Google.Cloud.Firestore;

namespace Mathio.Models;

[FirestoreData]
public class LessonModel
{
    [FirestoreDocumentId]
    public string ID { get; set; }
    [Required]
    [FirestoreProperty]
    public string Content { get; set; }
}