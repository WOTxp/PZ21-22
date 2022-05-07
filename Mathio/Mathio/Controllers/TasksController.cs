using System.Diagnostics;
using Firebase.Auth;
using Microsoft.AspNetCore.Mvc;
using Mathio.Models;
using Google.Cloud.Firestore;


namespace Mathio.Controllers;

public class TasksController : Controller
{
    private FirestoreDb _db;
    private static DocumentSnapshot? _lastDoc;

    public TasksController()
    {
        _db = FirestoreDb.Create("pz202122-cf12f");
    }

    // GET
    public IActionResult Index()
    {
        _lastDoc = null;
        return View();
    }

    public async Task<List<TasksModel>> GetTasksCategoryBatch(int batchSize)
    {
        Query tasksQuery = _db.Collection("Tasks").OrderBy("Category");

        if (_lastDoc != null)
        {
            tasksQuery = tasksQuery.StartAfter(_lastDoc);
        }

        tasksQuery = tasksQuery.Limit(batchSize);

        QuerySnapshot snapshot = await tasksQuery.GetSnapshotAsync();
        List<TasksModel> tasks = new List<TasksModel>();
        foreach (DocumentSnapshot document in snapshot.Documents)
        {
            tasks.Add(document.ConvertTo<TasksModel>());
        }

        _lastDoc = snapshot.Documents.Count > 0 ? snapshot.Documents.Last() : null;
        return tasks;
    }

    public async Task<IActionResult> LoadMoreTasks(int batchSize = 2)
    {
        List<TasksModel> tasksBatchAll = await GetTasksCategoryBatch(batchSize);
        List<Tuple<TasksModel, UserModel>> tasks = new List<Tuple<TasksModel, UserModel>>();
        foreach (TasksModel task in tasksBatchAll)
        {
            UserModel author = task.Author.GetSnapshotAsync().Result.ConvertTo<UserModel>();
            tasks.Add(new Tuple<TasksModel, UserModel>(task, author));
        }

        return PartialView("_TasksBatch", tasks);
    }
    
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel {RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier});
    }
}