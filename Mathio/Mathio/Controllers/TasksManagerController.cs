using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Mathio.Models;
using Google.Cloud.Firestore;
using Newtonsoft.Json;


namespace Mathio.Controllers;


public class TasksManager : Controller
{
    private FirestoreDb _db;
    public TasksManager()
    {
        _db = FirestoreDb.Create("pz202122-cf12f");
    }
    // GET
    public IActionResult Index()
    {
        return View();
    }
    
    public IActionResult AddTask()
    {
        return View();
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
    
    
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel {RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier});
    }
}