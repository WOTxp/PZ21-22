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

    public IActionResult EditTask(string id)
    {
        TasksModel t = _db.Collection("Tasks").Document(path: id).GetSnapshotAsync().Result.ConvertTo<TasksModel>();
        TasksAllModel tall = new TasksAllModel
        {
            Task = t
        };
        Console.WriteLine(t.Title+" "+t.ID);
        return View(tall);
    }
    
    [HttpPost]
    public IActionResult AddTask([Bind("Task, Lessons, Questions")] TasksAllModel newTask)
    {
        if (ModelState.IsValid)
        {
            return RedirectToAction("Index");
        }
        Console.WriteLine(ModelState.ErrorCount);
        return View(newTask);
    }
    
    
    [HttpPost]
    public IActionResult AddLesson([Bind("Task, Lessons, Questions")] TasksAllModel m)
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