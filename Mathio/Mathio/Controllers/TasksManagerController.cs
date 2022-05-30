using System.Diagnostics;
using Firebase.Auth;
using Microsoft.AspNetCore.Mvc;
using Mathio.Models;
using Google.Cloud.Firestore;
using Newtonsoft.Json;


namespace Mathio.Controllers;


public class TasksManager : Controller
{
    private FirebaseAuthProvider _auth;
    private FirestoreDb _db;
    private static DocumentSnapshot? _lastDoc;
    public TasksManager()
    {
        _auth = new FirebaseAuthProvider(
            new FirebaseConfig("AIzaSyAFjhO8zLz4S-nUoZyEtXZbzawQ0oor78k"));
        _db = FirestoreDb.Create("pz202122-cf12f");
    }
    // GET
    public IActionResult Index()
    {
        _lastDoc = null;
        return View();
    }
    
    public IActionResult AddTask()
    {
        var token = HttpContext.Session.GetString("_UserToken");
        if (string.IsNullOrEmpty(token))
        {
            TempData["msg"] = "Zaloguj się aby kontynuować";
            return RedirectToAction("SignIn", "Profile", routeValues:new{returnUrl="/TasksManager/AddTask"});
        }
        return View();
    }
    
    [HttpPost]
    public async Task<IActionResult> AddTask(TasksModel newTask)
    {
        var token = HttpContext.Session.GetString("_UserToken");
        if (string.IsNullOrEmpty(token))
        {
            TempData["msg"] = "Zaloguj się aby kontynuować";
            return RedirectToAction("SignIn", "Profile", routeValues:new{returnUrl="/TasksManager/AddTask"});
        }
        if (!ModelState.IsValid) return View(newTask);
        
        if (newTask.Lessons == null)
        {
            ViewData["error_msg"] = "Brak lekcji!";
            return View(newTask);
        }
        var lessonsCount = newTask.Lessons.Count(q => !q.Deleted);
        if (lessonsCount == 0)
        {
            ViewData["error_msg"] = "Brak lekcji!";
            return View(newTask);
        }
        if (newTask.Questions == null)
        {
            ViewData["error_msg"] = "Brak pytań!";
            return View(newTask);
        }

        var questionsCount = newTask.Questions.Count(q => !q.Deleted);
        if (questionsCount < newTask.QuestionsPerTest)
        {
            ViewData["error_msg"] = "Za mało pytań, musi być minimalnie " + newTask.QuestionsPerTest + "!";
            return View(newTask);
        }
        
        var user = await _auth.GetUserAsync(token);
        var authorRef = _db.Collection("Users").Document(user.LocalId);
        newTask.AuthorReference = authorRef;
        newTask.NumPages = lessonsCount;
        newTask.LastUpdate = Timestamp.GetCurrentTimestamp();
        
        var task = await _db.Collection("Tasks").AddAsync(newTask);
            
        foreach (var l in newTask.Lessons.Where(l => !l.Deleted))
        {
            await task.Collection("Lessons").AddAsync(l);
        }
            
        foreach (var q in newTask.Questions.Where(q => !q.Deleted))
        {
            await task.Collection("Questions").AddAsync(q);
        }
            
        return RedirectToAction("Index");
    }
    
    public async Task<IActionResult> DeleteTask(string id)
    {
        if (!ModelState.IsValid)
            return RedirectToAction("Index", "TasksManager");
        var taskDoc = await _db.Collection("Tasks").Document(path: id).GetSnapshotAsync();
        var task = taskDoc.ConvertTo<TasksModel>();
        return View(task);
    }
    
    private async Task DeleteCollection(CollectionReference collectionReference, int batchSize){
        QuerySnapshot snapshot = await collectionReference.Limit(batchSize).GetSnapshotAsync();
        IReadOnlyList<DocumentSnapshot> documents = snapshot.Documents;
        while (documents.Count > 0)
        {
            foreach (DocumentSnapshot document in documents)
            {
                await document.Reference.DeleteAsync();
            }
            snapshot = await collectionReference.Limit(batchSize).GetSnapshotAsync();
            documents = snapshot.Documents;
        }
    }
    [HttpDelete]
    public async Task<IActionResult> DeleteTaskAll(string id)
    {
        Console.WriteLine(id);
        DocumentReference task = _db.Collection("Tasks").Document(path: id);
        var collections =  task.ListCollectionsAsync().GetAsyncEnumerator();
        try
        {
            while (await collections.MoveNextAsync())
            {
                await DeleteCollection(collections.Current, 32);
            }
        }
        finally
        {
            await collections.DisposeAsync();
        }

        await task.DeleteAsync();
        TempData["success"] = "Pomyślnie usunieto!";
        return Json(new {success = true, data=id});
    }
    
    public async Task<IActionResult> EditTask(string id)
    {
        if(!ModelState.IsValid)
            return RedirectToAction("Index", "TasksManager");
        
        var token = HttpContext.Session.GetString("_UserToken");
        if (string.IsNullOrEmpty(token))
        {
            TempData["msg"] = "Zaloguj się aby kontynuować";
            return RedirectToAction("SignIn", "Profile", routeValues:new{returnUrl="/TasksManager/EditTask/"+id});
        }
        var taskDoc = await _db.Collection("Tasks").Document(path: id).GetSnapshotAsync();
        var task = taskDoc.ConvertTo<TasksModel>();

        await task.DownloadAllLessons();
        await task.DownloadAllQuestions();
        
        return View(task);
    }

    [HttpPost]
    public async Task<IActionResult> EditTask(TasksModel task)
    {
        var token = HttpContext.Session.GetString("_UserToken");
        if (string.IsNullOrEmpty(token))
        {
            TempData["msg"] = "Zaloguj się aby kontynuować";
            return RedirectToAction("SignIn", "Profile", routeValues:new{returnUrl="/TasksManager"});
        }
        task.SelfReference = _db.Collection("Tasks").Document(task.SelfRefId);
        task.AuthorReference = _db.Collection("Users").Document(task.AuthorRefId);
        if (!ModelState.IsValid) return View(task);
        
        if (task.Lessons == null)
        {
            ViewData["error_msg"] = "Brak lekcji!";
            return View(task);
        }
        var lessonsCount = task.Lessons.Count(q => !q.Deleted);
        if (lessonsCount == 0)
        {
            ViewData["error_msg"] = "Brak lekcji!";
            return View(task);
        }
        if (task.Questions == null)
        {
            ViewData["error_msg"] = "Brak pytań!";
            return View(task);
        }
        if (task.SelfReference == null)
        {
            ViewData["error_msg"] = "Coś poszło nie tak!";
            return View(task);
        }

        var questionsCount = task.Questions.Count(q => !q.Deleted);
        if (questionsCount < task.QuestionsPerTest)
        {
            ViewData["error_msg"] = "Za mało pytań, musi być minimalnie " + task.QuestionsPerTest + "!";
            return View(task);
        }
        
        task.NumPages = lessonsCount;
        task.LastUpdate = Timestamp.GetCurrentTimestamp();
        
        await task.SelfReference.SetAsync(task);
            
        foreach (var l in task.Lessons.Where(l => !l.Deleted))
        {
            if (string.IsNullOrEmpty(l.ID))
            {
                await task.SelfReference.Collection("Lessons").AddAsync(l);
            }
            else
            {
                await task.SelfReference.Collection("Lessons").Document(l.ID).SetAsync(l);
            }
        }

        foreach (var l in task.Lessons.Where(l => !string.IsNullOrEmpty(l.ID) && l.Deleted))
        {
            await task.SelfReference.Collection("Lessons").Document(l.ID).DeleteAsync();
        }
            
        foreach (var q in task.Questions.Where(q => !q.Deleted))
        {
            if (string.IsNullOrEmpty(q.ID))
            {
                await task.SelfReference.Collection("Questions").AddAsync(q);
            }
            else
            {
                await task.SelfReference.Collection("Questions").Document(q.ID).SetAsync(q);
            }
        }
        
        foreach (var q in task.Questions.Where(q => !string.IsNullOrEmpty(q.ID) && q.Deleted))
        {
            await task.SelfReference.Collection("Questions").Document(q.ID).DeleteAsync();
        }
            
        return RedirectToAction("Index");
    }

    [HttpPost]
    public IActionResult AddLesson([Bind("Lessons")] TasksModel m)
    {
        m.Lessons ??= new List<LessonModel>();
        m.Lessons.Add(new LessonModel());
        Console.WriteLine(m.Lessons.Count);
        return PartialView("TasksManager/_LessonsList", m);
    }
    [HttpPost]
    public IActionResult AddQuestion([Bind("Questions")] TasksModel m)
    {
        m.Questions ??= new List<QuestionModel>();
        m.Questions.Add(new QuestionModel());
        Console.WriteLine(m.Questions.Count);
        return PartialView("TasksManager/_QuestionsList", m);
    }
    
    public async Task<IActionResult> LoadMoreTasksM(int batchSize = 2)
    {
        string? token = HttpContext.Session.GetString("_UserToken");
        if (!String.IsNullOrEmpty(token))
        {
            var userId = _auth.GetUserAsync(token).Result.LocalId;
            DocumentReference author = _db.Collection("Users").Document(userId);
            Query tasksQuery = _db.Collection("Tasks").WhereEqualTo("Author", author).OrderBy("Category");
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

            _lastDoc = snapshot.Documents.Count > 0 ? snapshot.Documents.Last() : _lastDoc;
            return PartialView("TasksManager/_TasksMBatch", tasks);
        }
        return PartialView("TasksManager/_TasksMBatch", new List<TasksModel>());
    }
    
    
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel {RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier});
    }
}