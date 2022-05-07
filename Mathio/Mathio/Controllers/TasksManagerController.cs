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
}