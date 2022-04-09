using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Mathio.Models;
using Firebase.Auth;
using Google.Cloud.Firestore;

namespace Mathio.Controllers;

public class ProfileController : Controller
{
    private FirebaseAuthProvider _auth;
    private FirestoreDb _db;

    public ProfileController()
    {
        _auth = new FirebaseAuthProvider(
            new FirebaseConfig("AIzaSyAFjhO8zLz4S-nUoZyEtXZbzawQ0oor78k"));
        _db = FirestoreDb.Create("pz202122-cf12f");
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
    [HttpPost]
    public async Task<IActionResult> Register(UserModel userModel)
    {
        try
        {
            //create the user
            var fbAuth = await _auth.
                CreateUserWithEmailAndPasswordAsync(userModel.Email, userModel.Password, userModel.UserName,
                true);
            
            userModel.ID = fbAuth.User.LocalId;
            userModel.Type = "user";
            userModel.Points = 0;
            CollectionReference usersRef = _db.Collection("Users");
            await usersRef.AddAsync(userModel);
            
            TempData["msg"] = "Pomyślnie zarejestrowano";
            return RedirectToAction("SignIn");
        }
        catch(FirebaseAuthException e){
            switch (e.Reason)
            {
                case AuthErrorReason.EmailExists:
                    ViewBag.Reason = "Konto z tym adresem e-mail już istnieje";
                    break;
                case AuthErrorReason.WeakPassword:
                    ViewBag.Reason = "Hasło nie spełnia wymagań bezpieczeństwa";
                    break;
                default:
                    ViewBag.Reason = e.Reason;
                    break;
            }

            return View();
        }
    }
    
    public IActionResult SignIn()
    {
        return View();
    }
    [HttpPost]
    public async Task<IActionResult> SignIn(UserModel userModel)
    {
        try
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
            else if (!fbAuth.User.IsEmailVerified)
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
        catch (FirebaseAuthException e)
        {
            switch (e.Reason)
            {
                case AuthErrorReason.WrongPassword:
                    ViewBag.Reason = "Nieprawidłowe hasło";
                    break;
                case AuthErrorReason.UnknownEmailAddress:
                    ViewBag.Reason = "Nieznany adres e-mail";
                    break;
                default:
                    ViewBag.Reason = e.Reason;
                    break;
            }
            return View();
        }
    }

    public IActionResult LogOut(){
        HttpContext.Session.Remove("_UserToken");
        return RedirectToAction("SignIn");
    }
    
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel {RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier});
    }
}