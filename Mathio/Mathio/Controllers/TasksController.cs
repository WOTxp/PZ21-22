using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Mathio.Models;
using Google.Cloud.Firestore;


namespace Mathio.Controllers;

public class TasksController : Controller
{
    private readonly FirestoreDb _db;
    private static DocumentSnapshot? _lastDoc;
    private static TasksModel? _openedTask;
    private static TestManager? _testManager;

    public TasksController()
    {
        _db = FirestoreDb.Create("pz202122-cf12f");
    }

    // GET
    public IActionResult Index()
    {
        _openedTask = null;
        _testManager = null;
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
        foreach (var document in snapshot.Documents)
        {
            tasks.Add(document.ConvertTo<TasksModel>());
        }

        _lastDoc = snapshot.Documents.Count > 0 ? snapshot.Documents.Last() : null;
        return tasks;
    }
    //GET: /Tasks/ID/Lessons
    [Route("Tasks/{id}/Lessons")]
    public async Task<IActionResult> Lessons(string id, int page=1)
    {
        if (_openedTask?.SelfReference?.Id != id)
        {
            var taskDoc = await _db.Collection("Tasks").Document(id).GetSnapshotAsync();
            _openedTask = taskDoc.ConvertTo<TasksModel>();
            await _openedTask.DownloadAuthor();
        }

        await _openedTask.GetLesson(page);
        return View(_openedTask);
    }
    //GET: /Tasks/ID/Questions
    [Route("Tasks/{id}/Questions")]
    public async Task<IActionResult> Questions(string id, int num = 1)
    {
        if (_testManager ==null || _openedTask?.SelfReference?.Id != id)
        {
            var taskDoc = await _db.Collection("Tasks").Document(id).GetSnapshotAsync();
            _openedTask = taskDoc.ConvertTo<TasksModel>();
            _testManager = new TestManager(_openedTask);
            Console.WriteLine(_testManager.Task.SelfReference?.Id);
            await _testManager.SetupTest();
        }

        _testManager.GetQuestion(num - 1);
        TempData["num"] = num;
        return View(_testManager);
    }

    public async Task<IActionResult> LoadMoreTasks(int batchSize = 2)
    {
        List<TasksModel> tasks = await GetTasksCategoryBatch(batchSize);
        foreach (TasksModel task in tasks)
        {
            await task.DownloadAuthor();
        }

        return PartialView("_TasksBatch", tasks);
    }

    [HttpPost]
    public IActionResult SaveAnswer([Bind("QuestionId, Answer")]QuestionAnswerModel model)
    {
        Console.WriteLine("HI");
        if (!ModelState.IsValid) return Json(new {success = false, msg = "No model!"});
        Console.WriteLine("HI?");
        Console.WriteLine(model.QuestionId+" "+model.Answer);
        if (model.QuestionId == null) return Json(new {success = false, msg = "Wrong data!"});
       
        Console.WriteLine(model.QuestionId, model.Answer);
        
        if (_testManager == null) return Json(new {success = false, msg = "No Test Manager!"});
        
        if (_testManager.currentQuestion?.ID != model.QuestionId)
            return Json(new {success = false, msg = "Something went wrong!"});

        var saved = _testManager.SaveAnswer(model);
        return Json(saved ? new {success=true, msg="Answer saved!"} : new {success = false, msg = "Something went wrong!"});
    }
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel {RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier});
    }
}