using System.ComponentModel.DataAnnotations;
using Google.Cloud.Firestore;

namespace Mathio.Models;

[FirestoreData]
public class TasksStatusModel
{
    [FirestoreDocumentId]
    public string? Id { get; set; }
    [Required]
    [FirestoreProperty]
    public DocumentReference? TaskReference { get; set; }
    [FirestoreProperty]
    public int TestScore { get; set; }
    [FirestoreProperty]
    public int CurrentPage { get; set; }
    public TasksModel? Task { get; set; }

    public async Task<TasksModel?> DownloadTask()
    {
        if (TaskReference == null) return null;
        if (Task != null) return Task;
        
        var snapshot = await TaskReference.GetSnapshotAsync();
        Task = snapshot.ConvertTo<TasksModel>();
        return Task;
    }
}