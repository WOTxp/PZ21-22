using System.ComponentModel.DataAnnotations;
using Google.Cloud.Firestore;

namespace Mathio.Models;

[FirestoreData]
public class QuestionModel
{
    [FirestoreDocumentId]
    public string ID { get; set; }
    [Required]
    [FirestoreProperty]
    public string Question { get; set; }
    [Required]
    [FirestoreProperty]
    public string Type { get; set; }
    [FirestoreProperty]
    public List<string> Answers { get; set; }
    [Required]
    [FirestoreProperty]
    public string CorrectAnswer { get; set; }
}