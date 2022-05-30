using System.ComponentModel.DataAnnotations;
using Google.Cloud.Firestore;

namespace Mathio.Models;

[FirestoreData]
public class QuestionModel
{
    [FirestoreDocumentId]
    public string? ID { get; set; }
    [Required]
    [FirestoreProperty]
    public string Question { get; set; }
    [Required]
    [FirestoreProperty]
    public string Type { get; set; }
    [FirestoreProperty]
    public List<string>? Answers { get; set; }
    [Required]
    [FirestoreProperty]
    public string CorrectAnswer { get; set; }

    public QuestionAnswerModel AnswerModel { get; set; }
    public bool Deleted { get; set; }
    public QuestionModel()
    {
        AnswerModel = new QuestionAnswerModel();
        Deleted = false;
    }
    public override string ToString()
    {
        return String.Format("\nQuestion: {1}\n",Question);
    }
}