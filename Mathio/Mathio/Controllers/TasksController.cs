using System.Diagnostics;
using Firebase.Auth;
using Microsoft.AspNetCore.Mvc;
using Mathio.Models;
using Google.Cloud.Firestore;
using Newtonsoft.Json;


namespace Mathio.Controllers;

public class TasksController : Controller
{
    private readonly FirestoreDb _db;
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
        SaveOpenedTask(null);
        SaveTestManager(null);
        SaveLastDoc(null);
        return View();
    }

    private async void UpdateTaskStatus(TasksStatusModel taskStatus)
    {
        var user = await _auth.GetUserAsync(HttpContext.Session.GetString("_UserToken"));
        var tasksStatusRef = _db.Collection("Users").Document(user.LocalId).Collection("TasksStatus");
        
        var statusQ = await tasksStatusRef.WhereEqualTo("TaskReference", taskStatus.TaskReference).GetSnapshotAsync();
        var statusRef = statusQ.Documents.FirstOrDefault();
        var pointsToAdd = 0;
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
                pointsToAdd = taskStatus.TestScore - status.TestScore;
            }
            if (updates.Count > 0)
                await statusRef.Reference.UpdateAsync(updates);
        }
        else
        {
            await tasksStatusRef.AddAsync(taskStatus);
            pointsToAdd = taskStatus.TestScore;
        }
        
        //Aktualizacja punktów użytkownika
        var userRef = _db.Collection("Users").Document(user.LocalId);
        var userDoc = await userRef.GetSnapshotAsync();
        var userModel = userDoc.ConvertTo<UserModel>();
        var points = userModel.Points + pointsToAdd;
        var update = new Dictionary<FieldPath, object>();
        update.Add(new FieldPath("Points"), points);
        await userRef.UpdateAsync(update);
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

        var openedTask = await GetOpenedTask();
        if (openedTask?.SelfReference?.Id != id)
        {
            var taskDoc = await _db.Collection("Tasks").Document(id).GetSnapshotAsync();
            openedTask = taskDoc.ConvertTo<TasksModel>();
            await openedTask.DownloadAuthor();
            SaveOpenedTask(openedTask);
        }

        await openedTask.GetLesson(page);
        var taskStatus = new TasksStatusModel
        {
            TaskReference = openedTask.SelfReference,
            CurrentPage = page,
            TestScore = 0
        };
        UpdateTaskStatus(taskStatus);
        return View(openedTask);
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

        var openedTask = await GetOpenedTask();
        var testManager = await GetTestManager();
        if (testManager == null || openedTask?.SelfReference?.Id != id)
        {
            var taskDoc = await _db.Collection("Tasks").Document(id).GetSnapshotAsync();
            openedTask = taskDoc.ConvertTo<TasksModel>();
            await openedTask.DownloadAuthor();
            SaveOpenedTask(openedTask);
            
            testManager = new TestManager(openedTask);
            await testManager.SetupTest();
        }
        testManager.GetQuestion(num - 1);
        SaveTestManager(testManager);
        
        TempData["num"] = num;
        return View(testManager);
    }
    
    [Route("Tasks/{id}/Summary")]
    public async Task<IActionResult> Summary(string id)
    {
        var isAuthorized = await IsAuthorized();
        if (!isAuthorized)
        {
            return RedirectToAction("SignIn", "Profile", new {returnUrl = "/Tasks/"+id+"/Summary"});
        }
        
        var openedTask = await GetOpenedTask();
        var testManager = await GetTestManager();
        if (testManager == null || openedTask?.SelfReference?.Id != id)
        {
            return RedirectToAction("Questions", "Tasks", new {id});
        }

        return View(testManager);
    }
    
    [Route("Tasks/{id}/Result")]
    public async Task<IActionResult> Result(string id)
    {
        var isAuthorized = await IsAuthorized();
        if (!isAuthorized)
        {
            return RedirectToAction("SignIn", "Profile", new {returnUrl = "/Tasks/"+id+"/Result"});
        }

        var openedTask = await GetOpenedTask();
        var testManager = await GetTestManager();
        if (testManager == null || openedTask?.SelfReference?.Id != id || testManager.testQuestions == null)
        {
            return RedirectToAction("Questions", "Tasks", new {id});
        }
        var user = await _auth.GetUserAsync(HttpContext.Session.GetString("_UserToken"));
        var testsHistoryRef = _db.Collection("Users").Document(user.LocalId).Collection("TestsHistory");
        
        var score = testManager.testQuestions.Count(q => q.AnswerModel.Answer == q.CorrectAnswer);
        var result = new TestHistoryModel
        {
            TaskReference = testManager.Task.SelfReference,
            Task = testManager.Task,
            Score = score,
            Date = Timestamp.GetCurrentTimestamp()
        };
        await testsHistoryRef.AddAsync(result);
        
        var taskStatus = new TasksStatusModel
        {
            TaskReference = testManager.Task.SelfReference,
            CurrentPage = 0,
            TestScore = score
        };
        UpdateTaskStatus(taskStatus);
        SaveOpenedTask(null);
        SaveTestManager(null);
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
        var testManager = await GetTestManager();
        if (testManager == null) return Json(new {success = false, msg = "No Test Manager!"});
        
        if (testManager.currentQuestion?.ID != model.QuestionId)
            return Json(new {success = false, msg = "Something went wrong!"});

        var saved = testManager.SaveAnswer(model);
        SaveTestManager(testManager);
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
        var lastDoc = await GetLastDoc();
        if (lastDoc != null)
        {
            tasksQuery = tasksQuery.StartAfter(lastDoc);
        }

        tasksQuery = tasksQuery.Limit(batchSize);

        var snapshot = await tasksQuery.GetSnapshotAsync();
        var tasks = snapshot.Documents.Select(document => document.ConvertTo<TasksModel>()).ToList();

        lastDoc = snapshot.Documents.Count > 0 ? snapshot.Documents[^1] : lastDoc;
        SaveLastDoc(lastDoc);
        return tasks;
    }
    
    private async Task<DocumentSnapshot?> GetLastDoc()
    {
        var lastDocRef = HttpContext.Session.GetString("_lastDoc");
        DocumentSnapshot? lastDoc = null;
        if (!String.IsNullOrEmpty(lastDocRef))
        {
            Console.WriteLine(lastDocRef);
            var reference = _db.Document(lastDocRef);
            lastDoc = await reference.GetSnapshotAsync();
        }

        return lastDoc;
    }
    private void SaveLastDoc(DocumentSnapshot? doc)
    {
        var reference = "";
        if (doc != null)
        {
            reference = doc.Reference.Parent.Id+"/"+doc.Reference.Id;
        }
        HttpContext.Session.SetString("_lastDoc", reference);
    }

    private async Task<TasksModel?> GetOpenedTask()
    {
        var openedTaskRef = HttpContext.Session.GetString("_lastDoc");
        TasksModel? task = null;
        if (!String.IsNullOrEmpty(openedTaskRef))
        {
            var reference = _db.Document(openedTaskRef);
            var documentSnapshot = await reference.GetSnapshotAsync();
            if (documentSnapshot != null)
            {
                task = documentSnapshot.ConvertTo<TasksModel>();
                await task.DownloadAuthor();
            }
        }
        return task;
    }
    private void SaveOpenedTask(TasksModel? task)
    {
        var reference = "";
        if (task != null)
        {
            reference = task.SelfReference?.Parent.Id+"/"+task.SelfReference?.Id;
        }
        HttpContext.Session.SetString("_lastDoc", reference);
    }

    private async Task<TestManager?> GetTestManager()
    {
        var testMStr = HttpContext.Session.GetString("_testManager");
        if (testMStr == null) return null;
        
        var testManager = JsonConvert.DeserializeObject<TestManager?>(testMStr);
        if(testManager != null)
            testManager.Task = (await GetOpenedTask())!;
        return testManager;

    }

    private void SaveTestManager(TestManager? testManager)
    {
        if (testManager != null)
        {
            var testMStr = JsonConvert.SerializeObject(testManager);
            HttpContext.Session.SetString("_testManager", testMStr);
            return;
        }
        HttpContext.Session.Remove("_testManager");
    }
}