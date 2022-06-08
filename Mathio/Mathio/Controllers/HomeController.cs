using System.Diagnostics;
using Firebase.Auth;
using Microsoft.AspNetCore.Mvc;
using Mathio.Models;
using Google.Cloud.Firestore;

namespace Mathio.Controllers;

public class HomeController : Controller
{
    private FirebaseAuthProvider _auth;
    private FirestoreDb _db;

    public HomeController()
    {
        _auth = new FirebaseAuthProvider(
            new FirebaseConfig("AIzaSyAFjhO8zLz4S-nUoZyEtXZbzawQ0oor78k"));
        _db = FirestoreDb.Create("pz202122-cf12f");
    }

    public IActionResult Index()
    {
        var token = HttpContext.Session.GetString("_UserToken");
        ViewBag.Layout = string.IsNullOrEmpty(token) ? "_Layout2" : "_Layout";
        
        return View();
    }

    public IActionResult Privacy()
    {
        var token = HttpContext.Session.GetString("_UserToken");
        ViewBag.Layout = string.IsNullOrEmpty(token) ? "_Layout2" : "_Layout";
        
        return View();
    }
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel {RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier});
    }
}