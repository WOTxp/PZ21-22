using System.ComponentModel.DataAnnotations;
using Google.Cloud.Firestore;

namespace Mathio.Models;

[FirestoreData]
public class TasksModel
{
    [FirestoreDocumentId]
    public string? ID { get; set; }
    [FirestoreProperty]
    public DocumentReference? AuthorReference { get; set; }
    [Required]
    [FirestoreProperty]
    public string Title { get; set; }
    [FirestoreProperty]
    public string Description { get; set; }
    [FirestoreProperty]
    public string Category { get; set; }
    public UserModel? Author { get; set; }

    public async Task<UserModel?> DownloadAuthor()
    {
        if (AuthorReference == null) return null;
        if (Author != null) return Author;
        
        var snapshot = await AuthorReference.GetSnapshotAsync();
        Author = snapshot.ConvertTo<UserModel>();
        return Author;

    }
}