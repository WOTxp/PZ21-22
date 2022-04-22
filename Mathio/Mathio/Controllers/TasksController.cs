using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Mathio.Models;
using Google.Cloud.Firestore;


namespace Mathio.Controllers;

public class TasksController : Controller
{
    private FirestoreDb _db;
    private List<string> classes;
    public TasksController()
    {
        _db = FirestoreDb.Create("pz202122-cf12f");
        classes = new List<string>();
        classes.Add("Test1");
        classes.Add("Test2");
        classes.Add("Test3");
    }
    // GET
    public IActionResult Index()
    {
        HttpContext.Session.SetString("_lastDoc", "");
        return View();
    }
    
    public async Task<List<TasksModel>> GetTasksCategoryBatch(string category, int batchSize)
    {
        Query  tasksQuery = _db.Collection("Tasks").WhereEqualTo("Category", category);


        var sessionValue = HttpContext.Session.GetString("_lastDoc");
        if (!String.IsNullOrEmpty(sessionValue))
        {
            DocumentSnapshot lastDoc  = await _db.Collection("Tasks").Document(sessionValue).GetSnapshotAsync();
            tasksQuery = tasksQuery.StartAfter(lastDoc);
        }
        tasksQuery = tasksQuery.Limit(batchSize);

        QuerySnapshot snapshot = await tasksQuery.GetSnapshotAsync();
        List<TasksModel> tasks = new List<TasksModel>();
        foreach (DocumentSnapshot document in snapshot.Documents)
        {
            tasks.Add(document.ConvertTo<TasksModel>());
        }

        HttpContext.Session.SetString("_lastDoc", snapshot.Documents.Count > 0 ? snapshot.Documents.Last().Id : "");
        return tasks;
    }

    public async Task<IActionResult> LoadMoreTasks()
    {
        int batchSize = 2;
        int currentnum = 0;
        int category = HttpContext.Session.GetInt32("_CurrentNum") ?? 0;

        Dictionary<string,List<TasksModel>> tasksBatchAll = new Dictionary<string, List<TasksModel>>();
        while(category < classes.Count && currentnum < batchSize)
        {
            List<TasksModel> tasksBatch = await  GetTasksCategoryBatch(classes[category], batchSize - currentnum);
            currentnum += tasksBatch.Count;
            tasksBatchAll.TryAdd(classes[category],new List<TasksModel>());
            tasksBatchAll[classes[category]].AddRange(tasksBatch);
            if (currentnum < batchSize)
            {
                category += 1;
                HttpContext.Session.SetInt32("_CurrentNum", category);
                HttpContext.Session.SetString("_lastDoc", "");
            }
        }

        return PartialView("_TasksBatch", tasksBatchAll);
    }

    public async Task<string> ShowTasks()
    {
        var sessionValue = HttpContext.Session.GetString("_lastDoc");
        if (!String.IsNullOrEmpty(sessionValue))
        {
            DocumentSnapshot lastDoc  = await _db.Collection("Tasks").Document(sessionValue).GetSnapshotAsync();
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