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
        return View();
    }
    
    [HttpPost]
    public async Task<IActionResult> AddTask([Bind("Task, Lessons, Questions")] TasksAllModel newTask)
    {
        if (ModelState.IsValid)
        {
            CollectionReference tasks = _db.Collection("Tasks");
            DocumentReference task = await tasks.AddAsync(newTask.Task);
            
            CollectionReference lessons = task.Collection("Lessons");
            foreach (LessonModel l in newTask.Lessons)
            {
                await lessons.AddAsync(l);
            }

            CollectionReference questions = task.Collection("Questions");
            foreach (QuestionModel q in newTask.Questions)
            {
                await questions.AddAsync(q);
            }
            
            return RedirectToAction("Index");
        }
        return View(newTask);
    }
    
    public async Task<IActionResult> DeleteTask(string id)
    {
        if (!ModelState.IsValid)
            return RedirectToAction("Index", "TasksManager");
        DocumentReference task = _db.Collection("Tasks").Document(path: id);
        TasksModel t = task.GetSnapshotAsync().Result.ConvertTo<TasksModel>();
        return View(t);
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
    
    public async Task<IActionResult> EditTask(string id="")
    {
        if (id == "")
            return RedirectToAction("Index", "TasksManager");
        
        DocumentReference task = _db.Collection("Tasks").Document(path: id);
        TasksModel t = task.GetSnapshotAsync().Result.ConvertTo<TasksModel>();
        
        List<LessonModel> lessons = new List<LessonModel>(); 
        QuerySnapshot snap_lessons = await task.Collection("Lessons").GetSnapshotAsync();
        foreach (DocumentSnapshot l in snap_lessons.Documents)
        {
            lessons.Add(l.ConvertTo<LessonModel>());
        }

        List<QuestionModel> questions = new List<QuestionModel>();
        QuerySnapshot snap_questions = await task.Collection("Questions").GetSnapshotAsync();
        foreach (DocumentSnapshot q in snap_questions.Documents)
        {
            questions.Add(q.ConvertTo<QuestionModel>());
        }
        
        TasksAllModel tall = new TasksAllModel
        {
            Task = t,
            Lessons = lessons,
            Questions = questions
        };
        Console.WriteLine(t.Title+" "+t.ID);
        return View(tall);
    }

    [HttpPost]
    public async Task<IActionResult> EditTask(TasksAllModel task)
    {
        if (ModelState.IsValid)
        {
            return RedirectToAction("Index", "TasksManager");
        }
        return View(task);
    }

    [HttpPost]
    public IActionResult AddLesson([Bind("Lessons")] TasksAllModel m)
    {
        m.Lessons.Add(new LessonModel());
        Console.WriteLine(m.Lessons.Count);
        return PartialView("TasksManager/_LessonsList", m);
    }
    [HttpPost]
    public IActionResult AddQuestion([Bind("Questions")] TasksAllModel m)
    {
        m.Questions.Add(new QuestionModel());
        Console.WriteLine(m.Questions.Count);
        return PartialView("TasksManager/_QuestionsList", m);
    }
    
    public async Task<IActionResult> LoadMoreTasksM(int batchSize = 2)
    {
        string? token = HttpContext.Session.GetString("_UserToken");
        if (!String.IsNullOrEmpty(token))
        {
            var UserId = _auth.GetUserAsync(token).Result.LocalId;
            DocumentReference author = _db.Collection("Users").Document(UserId);
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