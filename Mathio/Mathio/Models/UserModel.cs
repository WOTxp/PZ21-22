using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Google.Cloud.Firestore;


namespace Mathio.Models;

[FirestoreData]
public class UserModel
{
    [FirestoreDocumentId]
    public DocumentReference? Self { get; set; }
    [FirestoreProperty]
    [Required]
    [RegularExpression("\\w*", ErrorMessage = "Dozwolone tylko litery, cyfry i znak _")]
    [DisplayName("Nazwa użytkownika")]
    public string? UserName { get; set; }
    [FirestoreProperty]
    public string? Type { get; set; }
    [FirestoreProperty]
    public int Points { get; set; }
    [FirestoreProperty]
    [DisplayName("Imię")]
    public string? FirstName { get; set; }
    [FirestoreProperty]
    [DisplayName("Nazwisko")]
    public string? LastName { get; set; }
    [FirestoreProperty]
    [DisplayName("Opis")]
    public string? Description { get; set; }
    public string? Email { get; set; }
    public ICollection<TasksStatusModel>? TasksStatus { get; set; }

    public async Task DownloadTasksStatus()
    {
        TasksStatus = new List<TasksStatusModel>();
        if (Self != null)
        {
            var finishedTasksQuery =
                await Self.Collection("TasksStatus").GetSnapshotAsync();
            foreach (var document in finishedTasksQuery)
            {
                var finishedTask = document.ConvertTo<TasksStatusModel>();
                TasksStatus.Add(finishedTask);
            }
        }
    }



}