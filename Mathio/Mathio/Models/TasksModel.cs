using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography.X509Certificates;
using Google.Cloud.Firestore;

namespace Mathio.Models;

[FirestoreData]
public class TasksModel
{
    [FirestoreDocumentId]
    public DocumentReference? SelfReference{ get; set; }
    [FirestoreProperty]
    public DocumentReference? AuthorReference { get; set; }
    [Required]
    [FirestoreProperty]
    public string? Title { get; set; }
    [FirestoreProperty]
    public string? Description { get; set; }
    [FirestoreProperty]
    public string? Category { get; set; }
    [Required]
    [FirestoreProperty]
    public int QuestionsPerTest { get; set; }
    [FirestoreProperty]
    public int NumPages { get; set; }
    [FirestoreProperty]
    public Timestamp? LastUpdate { get; set; }
    
    public UserModel? Author { get; set; }
    public List<LessonModel>? Lessons { get; set; }
    public List<QuestionModel>? Questions { get; set; }
    
    public LessonModel? currentLesson { get; set; }
    
    public string? SelfRefId { get; set; }
    public string? AuthorRefId { get; set; }
    public async Task<UserModel?> DownloadAuthor()
    {
        if (AuthorReference == null) return null;
        if (Author != null) return Author;
        
        var snapshot = await AuthorReference.GetSnapshotAsync();
        Author = snapshot.ConvertTo<UserModel>();
        return Author;
    }

    public async Task DownloadAllLessons()
    {
        if (SelfReference == null) return;
        var lessonsDocs = await SelfReference.Collection("Lessons").GetSnapshotAsync();
        Lessons = new List<LessonModel>();
        foreach (var doc in lessonsDocs)
        {
            Lessons.Add(doc.ConvertTo<LessonModel>());
        }
    }
    public async Task DownloadAllQuestions()
    {
        if (SelfReference == null) return;
        var questionsDocs = await SelfReference.Collection("Questions").GetSnapshotAsync();
        Questions = new List<QuestionModel>();
        foreach (var doc in questionsDocs)
        {
            Questions.Add(doc.ConvertTo<QuestionModel>());
        }
    }
    public async Task<LessonModel?> GetLesson(int page)
    {
        if (currentLesson != null && currentLesson.Page==page) return currentLesson;
        if (Lessons != null)
        {
            foreach (var lesson in Lessons.Where(lesson => lesson.Page == page))
            {
                currentLesson = lesson;
                return currentLesson;
            }
        }

        if (SelfReference == null)
        {
            currentLesson = null;
            return null;
        }
        
        var lessonsRef = await SelfReference.Collection("Lessons").WhereEqualTo("Page", page).GetSnapshotAsync();
        if (lessonsRef.Documents.Count == 0)
        {
            currentLesson = null;
            return null;
        }
        currentLesson = lessonsRef.Documents[0].ConvertTo<LessonModel>();
        Lessons ??= new List<LessonModel>();
        Lessons.Add(currentLesson);
        
        return currentLesson;
    }
}