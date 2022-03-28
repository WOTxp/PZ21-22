using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Mathio.Models;
using Firebase.Auth;

namespace Mathio.Controllers;

public class ProfileController : Controller
{
    private FirebaseAuthProvider _auth;

    public ProfileController()
    {
        _auth = new FirebaseAuthProvider(
            new FirebaseConfig("AIzaSyAFjhO8zLz4S-nUoZyEtXZbzawQ0oor78k"));
    }
    // GET
    public IActionResult Index()
    {
        return View();
    }

    public IActionResult Register()
    {
        return View();
    }
    
    public IActionResult SignIn()
    {
        return View();
    }
    [HttpPost]
    public async Task<IActionResult> SignIn(UserModel userModel)
    {
        //log in the new user
        var fbAuth = await _auth
            .SignInWithEmailAndPasswordAsync(userModel.Email, userModel.Password);
        await fbAuth.RefreshUserDetails();
        string token = fbAuth.FirebaseToken;
        //saving the token in a session variable
        if (token != null && fbAuth.User.IsEmailVerified)
        {
            HttpContext.Session.SetString("_UserToken", token);

            return RedirectToAction("Index");
        }
        else if(!fbAuth.User.IsEmailVerified)
        {
            ViewBag.Reason = "Login failed: Email Not Verified!";
            return View();
        }
        else
        {
            ViewBag.Reason = "Login failed!";
            return View();
        }
    }
    
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel {RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier});
    }
}