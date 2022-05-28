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
    public int Number { get; set; }
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
    public bool Deleted = false;
    
    public QuestionAnswerModel AnswerModel { get; set; }

    public QuestionModel()
    {
        AnswerModel = new QuestionAnswerModel();
    }
    public override string ToString()
    {
        return String.Format("{0}\nQuestion: {1}\n",Number,Question);
    }
}