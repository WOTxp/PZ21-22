using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Mathio.Models;
using Google.Cloud.Firestore;


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
    
    [HttpPost]
    public IActionResult AddTask(TasksAllModel t)
    {
        Console.WriteLine(t.Task);
        foreach (var lesson in t.Lessons)
        {
            Console.WriteLine(lesson);
        }
        foreach (var question in t.Questions)
        {
            Console.WriteLine(question);
        }
        return View();
    }

    public IActionResult GetLessonManagerPartial(int page)
    {
        Console.WriteLine(page);
        LessonModel lesson = new LessonModel
        {
            Page = page,
            Content = " ",
        };
        return PartialView("_LessonManager", lesson);
    }
    public IActionResult GetQuestionManagerPartial(int nr)
    {
        Console.WriteLine(nr);
        QuestionModel question = new QuestionModel
        {
            Number = nr,
            Type = " ",
            CorrectAnswer = " ",
        };
        return PartialView("_QuestionManager", question);
    }
    
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel {RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier});
    }
}