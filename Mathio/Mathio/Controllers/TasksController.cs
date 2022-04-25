using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Mathio.Models;
using Google.Cloud.Firestore;


namespace Mathio.Controllers;

public class TasksController : Controller
{
    private FirestoreDb _db;
    private List<string> classes;
    private static int _currentCategory;
    private static string? _lastDocID;
    private static int _lastDocCategory;
    public TasksController()
    {
        _db = FirestoreDb.Create("pz202122-cf12f");
        classes = new List<string>
        {
            "Klasa 1",
            "Test1",
            "Test2",
            "Test3"
        };
    }
    // GET
    public IActionResult Index()
    {
        _currentCategory = 0;
        _lastDocCategory = -1;
        _lastDocID = "";
        return View();
    }
    
    public async Task<List<TasksModel>> GetTasksCategoryBatch(string category, int batchSize)
    {
        Query  tasksQuery = _db.Collection("Tasks").WhereEqualTo("Category", category);
        
        if (!String.IsNullOrEmpty(_lastDocID))
        {
            DocumentSnapshot lastDoc  = await _db.Collection("Tasks").Document(_lastDocID).GetSnapshotAsync();
            tasksQuery = tasksQuery.StartAfter(lastDoc);
        }
        tasksQuery = tasksQuery.Limit(batchSize);

        QuerySnapshot snapshot = await tasksQuery.GetSnapshotAsync();
        List<TasksModel> tasks = new List<TasksModel>();
        foreach (DocumentSnapshot document in snapshot.Documents)
        {
            tasks.Add(document.ConvertTo<TasksModel>());
        }

        _lastDocID = snapshot.Documents.Count > 0 ? snapshot.Documents.Last().Id : "";
        _lastDocCategory = _currentCategory;
        return tasks;
    }

    public async Task<IActionResult> LoadMoreTasks()
    {
        int batchSize = 2;
        int currentnum = 0;

        List<TasksModel> tasksBatchAll = new List<TasksModel>();
        while(_currentCategory < classes.Count && currentnum < batchSize)
        {
            List<TasksModel> tasksBatch = await  GetTasksCategoryBatch(classes[_currentCategory], batchSize - currentnum);
            currentnum += tasksBatch.Count;
            tasksBatchAll.AddRange(tasksBatch);
            if (currentnum < batchSize)
            {
                _currentCategory += 1;
                _lastDocID = "";
            }
        }

        return PartialView("_TasksBatch", tasksBatchAll);
    }

    public async Task<string> ShowTasks()
    {
        if (!String.IsNullOrEmpty(_lastDocID))
        {
            DocumentSnapshot lastDoc  = await _db.Collection("Tasks").Document(_lastDocID).GetSnapshotAsync();
            return lastDoc.Id;
        }
        return "Brak";
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel {RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier});
    }
}