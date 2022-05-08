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
    public int Page { get; set; }
    [Required]
    [FirestoreProperty]
    public string Content { get; set; }
    
    public override string ToString()
    {
        return String.Format("{0}\nContent: {1}\n",Page,Content);
    }
}