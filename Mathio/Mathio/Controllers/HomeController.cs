using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Mathio.Models;
using Google.Cloud.Firestore;

namespace Mathio.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;

    public HomeController(ILogger<HomeController> logger)
    {
        _logger = logger;
    }

    public IActionResult Index()
    {
        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }
    
    public string Databasetest()
    {
        FirestoreDb db = FirestoreDb.Create("pz202122-cf12f");
        return "Success";
    }

    
    public async Task<string> Databasegettasks()
    {
        string ret = "";
        FirestoreDb db = FirestoreDb.Create("pz202122-cf12f");
        
        CollectionReference usersRef = db.Collection("Tasks");
        QuerySnapshot snapshot = await usersRef.GetSnapshotAsync();
        foreach (DocumentSnapshot doc in snapshot.Documents)
        {
            ret += string.Format("Document Id: {0}\n", doc.Id);
            Dictionary<string, object> docDict = doc.ToDictionary();
            ret += string.Join(Environment.NewLine, docDict);
        }

        return ret;
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel {RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier});
    }
}