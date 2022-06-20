using System.Diagnostics;
using Firebase.Auth;
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
    private readonly FirebaseAuthProvider _auth;

    public TasksController()
    {
        _auth = new FirebaseAuthProvider(
            new FirebaseConfig("AIzaSyAFjhO8zLz4S-nUoZyEtXZbzawQ0oor78k"));
        
        _db = FirestoreDb.Create("pz202122-cf12f");
    }

    //GET: /Tasks
    public async Task<IActionResult> Index()
    {
        var isAuthorized = await IsAuthorized();
        if (!isAuthorized)
        {
            return RedirectToAction("SignIn", "Profile", new {returnUrl = "/Tasks"});
        }
        _openedTask = null;
        _testManager = null;
        _lastDoc = null;
        return View();
    }

    private async void UpdateTaskStatus(TasksStatusModel taskStatus)
    {
        var user = await _auth.GetUserAsync(HttpContext.Session.GetString("_UserToken"));
        var tasksStatusRef = _db.Collection("Users").Document(user.LocalId).Collection("TasksStatus");
        
        var statusQ = await tasksStatusRef.WhereEqualTo("TaskReference", taskStatus.TaskReference).GetSnapshotAsync();
        var statusRef = statusQ.Documents.FirstOrDefault();
        if(statusRef != null)
        {
            var status = statusRef.ConvertTo<TasksStatusModel>();
            
            var updates = new Dictionary<FieldPath, object>();
            if (taskStatus.CurrentPage > status.CurrentPage)
            {
                updates.Add(new FieldPath("CurrentPage"), taskStatus.CurrentPage);
            }
            if (taskStatus.TestScore > status.TestScore)
            {
                updates.Add(new FieldPath("TestScore"), taskStatus.TestScore);
            }
            if (updates.Count > 0)
                await statusRef.Reference.UpdateAsync(updates);
        }
        else
        {
            await tasksStatusRef.AddAsync(taskStatus);
        }
    }
    //GET: /Tasks/ID/Lessons
    [Route("Tasks/{id}/Lessons")]
    public async Task<IActionResult> Lessons(string id, int page=1)
    {
        var isAuthorized = await IsAuthorized();
        if (!isAuthorized)
        {
            return RedirectToAction("SignIn", "Profile", new {returnUrl = "/Tasks/"+id+"/Lessons?page="+page});
        }
        if (_openedTask?.SelfReference?.Id != id)
        {
            var taskDoc = await _db.Collection("Tasks").Document(id).GetSnapshotAsync();
            _openedTask = taskDoc.ConvertTo<TasksModel>();
            await _openedTask.DownloadAuthor();
        }

        await _openedTask.GetLesson(page);
        var taskStatus = new TasksStatusModel
        {
            TaskReference = _openedTask.SelfReference,
            CurrentPage = page,
            TestScore = 0
        };
        UpdateTaskStatus(taskStatus);
        return View(_openedTask);
    }
    //GET: /Tasks/ID/Questions
    [Route("Tasks/{id}/Questions")]
    public async Task<IActionResult> Questions(string id, int num = 1)
    {
        var isAuthorized = await IsAuthorized();
        if (!isAuthorized)
        {
            return RedirectToAction("SignIn", "Profile", new {returnUrl = "/Tasks/"+id+"/Questions?num="+num});
        }
        if (_testManager == null || _openedTask?.SelfReference?.Id != id)
        {
            var taskDoc = await _db.Collection("Tasks").Document(id).GetSnapshotAsync();
            _openedTask = taskDoc.ConvertTo<TasksModel>();
            await _openedTask.DownloadAuthor();
            _testManager = new TestManager(_openedTask);
            Console.WriteLine(_testManager.Task.SelfReference?.Id);
            await _testManager.SetupTest();
        }

        _testManager.GetQuestion(num - 1);
        TempData["num"] = num;
        return View(_testManager);
    }
    
    [Route("Tasks/{id}/Summary")]
    public IActionResult Summary(string id)
    {
        var isAuthorized = IsAuthorized().Result;
        if (!isAuthorized)
        {
            return RedirectToAction("SignIn", "Profile", new {returnUrl = "/Tasks/"+id+"/Summary"});
        }
        if (_testManager == null || _openedTask?.SelfReference?.Id != id)
        {
            return RedirectToAction("Questions", "Tasks", new {id});
        }

        return View(_testManager);
    }
    
    [Route("Tasks/{id}/Result")]
    public async Task<IActionResult> Result(string id)
    {
        var isAuthorized = IsAuthorized().Result;
        if (!isAuthorized)
        {
            return RedirectToAction("SignIn", "Profile", new {returnUrl = "/Tasks/"+id+"/Result"});
        }
        if (_testManager == null || _openedTask?.SelfReference?.Id != id || _testManager.testQuestions == null)
        {
            return RedirectToAction("Questions", "Tasks", new {id});
        }
        var user = await _auth.GetUserAsync(HttpContext.Session.GetString("_UserToken"));
        var testsHistoryRef = _db.Collection("Users").Document(user.LocalId).Collection("TestsHistory");
        
        var score = _testManager.testQuestions.Count(q => q.AnswerModel.Answer == q.CorrectAnswer);
        var result = new TestHistoryModel
        {
            TaskReference = _testManager.Task.SelfReference,
            Task = _testManager.Task,
            Score = score,
            Date = Timestamp.GetCurrentTimestamp()
        };
        await testsHistoryRef.AddAsync(result);
        
        var taskStatus = new TasksStatusModel
        {
            TaskReference = _testManager.Task.SelfReference,
            CurrentPage = 0,
            TestScore = score
        };
        UpdateTaskStatus(taskStatus);

        _openedTask = null;
        _testManager = null;
        return View(result);
    }
    //GET: /Tasks/LoadMoreTasks
    public async Task<IActionResult?> LoadMoreTasks(int batchSize = 2)
    {
        var isAuthorized = await IsAuthorized();
        if (!isAuthorized)
        {
            return Unauthorized();
        }
        var tasks = await GetTasksCategoryBatch(batchSize);
        foreach (var task in tasks)
        {
            await task.DownloadAuthor();
        }

        return PartialView("_TasksBatch", tasks);
    }
    //POST: /Tasks/SaveAnswer
    [HttpPost]
    public async Task<IActionResult> SaveAnswer([Bind("QuestionId, Answer")]QuestionAnswerModel model)
    {
        var isAuthorized = await IsAuthorized();
        if (!isAuthorized)
        {
            return Unauthorized();
        }
        if (!ModelState.IsValid) return Json(new {success = false, msg = "No model!"});
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

    private async Task<bool> IsAuthorized()
    {
        var token = HttpContext.Session.GetString("_UserToken");
        if (string.IsNullOrEmpty(token))
        {
            TempData["msg"] = "Zaloguj się aby kontynuować";
            return false;
        }
        try
        {
            await _auth.GetUserAsync(token);
            return true;
        }
        catch (FirebaseAuthException e)
        {
            if (e.Reason == AuthErrorReason.InvalidIDToken)
            {
                TempData["msg"] = "Nieprawidłowy token uwierzytelniający! Zaloguj się aby kontynuować";
            }
            return false;
        }
    }
    private async Task<List<TasksModel>> GetTasksCategoryBatch(int batchSize)
    {
        var tasksQuery = _db.Collection("Tasks").OrderBy("Category");

        if (_lastDoc != null)
        {
            tasksQuery = tasksQuery.StartAfter(_lastDoc);
        }

        tasksQuery = tasksQuery.Limit(batchSize);

        var snapshot = await tasksQuery.GetSnapshotAsync();
        var tasks = snapshot.Documents.Select(document => document.ConvertTo<TasksModel>()).ToList();

        _lastDoc = snapshot.Documents.Count > 0 ? snapshot.Documents.Last() : null;
        return tasks;
    }
}