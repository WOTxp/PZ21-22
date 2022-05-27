﻿using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Google.Cloud.Firestore;


namespace Mathio.Models;

[FirestoreData]
public class UserModel
{
    [FirestoreDocumentId]
    public string? Id { get; set; }
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
    public ICollection<TasksFinModel>? FinishedTasks { get; set; }

    public async Task DownloadFinishedTasks(FirestoreDb db)
    {
        FinishedTasks = new List<TasksFinModel>();
        var finishedTasksQuery = 
            await db.Collection("Users").Document(Id).Collection("FinishedTasks").GetSnapshotAsync();
        foreach (var document in finishedTasksQuery)
        {
            var finishedTask = document.ConvertTo<TasksFinModel>();
            FinishedTasks.Add(finishedTask);
        }
    }



}