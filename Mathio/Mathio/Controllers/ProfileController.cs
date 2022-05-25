﻿using System.Diagnostics;
using Firebase.Auth;
using Microsoft.AspNetCore.Mvc;
using Mathio.Models;
using Google.Cloud.Firestore;
using FirebaseAuthException = Firebase.Auth.FirebaseAuthException;

namespace Mathio.Controllers;

public class ProfileController : Controller
{
    private readonly FirebaseAuthProvider _auth;
    private readonly FirestoreDb _db;

    public ProfileController()
    {
        _auth = new FirebaseAuthProvider(
            new FirebaseConfig("AIzaSyAFjhO8zLz4S-nUoZyEtXZbzawQ0oor78k"));
        /*if (FirebaseAuth.DefaultInstance==null)
        {
            FirebaseApp.Create();
        }*/
        
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
    [Route("Profile/Settings")]
    public IActionResult Settings()
    {
        string? token = HttpContext.Session.GetString("_UserToken");
        if (!String.IsNullOrEmpty(token))
        {
            try
            {
                var user = _auth.GetUserAsync(token).Result;
                UserModel userModel = new UserModel()
                {
                    Email = user.Email,
                    UserName = user.DisplayName
                };
                return View("Settings/Index", userModel);
            }
            catch (Exception)
            {
                return RedirectToAction("SignIn");
            }
        }
        return RedirectToAction("SignIn");
    }
    [HttpPost]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        try
        {
            //create the user
            var fbAuth = await _auth.
                CreateUserWithEmailAndPasswordAsync(model.Email, model.Password, model.UserName,
                true);
            
            /*userModel.ID = fbAuth.User.LocalId;
            userModel.Type = "user";
            userModel.Points = 0;
            CollectionReference usersRef = _db.Collection("Users");
            await usersRef.Document(userModel.ID).SetAsync(userModel);*/

            TempData["msg"] = "Pomyślnie zarejestrowano";
            return RedirectToAction("SignIn");
        }
        catch(FirebaseAuthException e){
            switch (e.Reason)
            {
                case AuthErrorReason.EmailExists:
                    ModelState.AddModelError("Email", "Konto z tym adresem e-mail już istnieje"); 
                    break;
                case AuthErrorReason.WeakPassword:
                    ModelState.AddModelError("Password", "Hasło nie spełnia wymagań bezpieczeństwa");
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
    public async Task<IActionResult> SignIn(SignInViewModel model)
    {
        try
        {
            //log in the new user
            var fbAuth = await _auth
                .SignInWithEmailAndPasswordAsync(model.Email, model.Password);
            await fbAuth.RefreshUserDetails();
            var token = fbAuth.FirebaseToken;
            //saving the token in a session variable
            if (token != null && fbAuth.User.IsEmailVerified)
            {
                HttpContext.Session.SetString("_UserToken", token);
                return RedirectToAction("Index");
            }
            else if (!fbAuth.User.IsEmailVerified)
            {
                ViewBag.Reason = "Email nie zweryfikowany!";
                return View();
            }
            else
            {
                ViewBag.Reason = "Błąd logowania!";
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
    [HttpGet]
    public IActionResult LogOut(string msg="Pomyślnie wylogowano"){
        HttpContext.Session.Remove("_UserToken");
        TempData["msg"] = msg;
        return RedirectToAction("SignIn");
    }
    //GET: /Profile/Settings/ChangePassword
    [Route("Profile/Settings/ChangePassword")]
    [HttpGet]
    public async Task<IActionResult> ChangePassword()
    {
        var token = HttpContext.Session.GetString("_UserToken");
        if (string.IsNullOrEmpty(token))
        {
            TempData["msg"] = "Zaloguj się aby zmienić hasło";
            return RedirectToAction("SignIn", routeValues: new{returnUrl="/Profile/Settings/ChangePassword"});
        }
        
        try
        {
            await _auth.GetUserAsync(token);
            return View("Settings/ChangePassword");
        }
        catch (FirebaseAuthException e)
        {
            if (e.Reason == AuthErrorReason.InvalidIDToken)
            {
                TempData["msg"] = "Nieprawidłowy token uwierzytelniania! Zaloguj się aby zmienić hasło";
                return RedirectToAction("SignIn", routeValues: new{returnUrl="/Profile/Settings/ChangePassword"});
            }
        }
        
        return RedirectToAction("SignIn");
    }
    //POST: /Profile/Settings/ChangePassword
    [Route("Profile/Settings/ChangePassword")]
    [HttpPost]
    public async Task<IActionResult> ChangePassword(ChangePasswordModel data)
    {
        if (!ModelState.IsValid) return View("Settings/ChangePassword");
        try
        {
            var token = HttpContext.Session.GetString("_UserToken");
            if (string.IsNullOrEmpty(token))
            {
                TempData["msg"] = "Zaloguj się aby zmienić hasło";
                return RedirectToAction("SignIn", routeValues: new{returnUrl="/Profile/Settings/ChangePassword"});
            }
            var loggedUser = await  _auth.GetUserAsync(token);
            var authLink = await _auth.SignInWithEmailAndPasswordAsync(loggedUser.Email, data.OldPassword);
            HttpContext.Session.SetString("_UserToken", authLink.FirebaseToken);
            authLink = await _auth.ChangeUserPassword(token, data.NewPassword);
            HttpContext.Session.SetString("_UserToken", authLink.FirebaseToken);
            ViewBag.Success = "Pomyślnie zmieniono hasło.";
            return View("Settings/ChangePassword");

        }
        catch (FirebaseAuthException e)
        {
            switch (e.Reason)
            {
                case AuthErrorReason.WrongPassword:
                    ModelState.AddModelError("OldPassword", "Nieprawidłowe hasło");
                    break;
                case AuthErrorReason.MissingPassword:
                    ModelState.AddModelError("OldPassword", "Nie podano aktualnego hasła");
                    break;
                case AuthErrorReason.WeakPassword:
                    ModelState.AddModelError("NewPassword", "Hasło nie spełnia wymagań bezpieczeństwa");
                    break;
                case AuthErrorReason.InvalidIDToken:
                    TempData["msg"] = "Nieprawidłowy token uwierzytelniania! Zaloguj się aby zmienić hasło";
                    return RedirectToAction("SignIn", routeValues: new{returnUrl="/Profile/Settings/ChangePassword"});
                default:
                    ViewBag.Reason = e.Reason;
                    break;
            }
        }
        catch (Exception e)
        {
            ViewBag.Reason = e.Message;
        }
        
        return View("Settings/ChangePassword");
    }
    [Route("Profile/Settings/UpdateEmail")]
    [HttpPost]
    public async Task<IActionResult> UpdateEmail(string email)
    {
        Console.WriteLine("Email:");
        Console.WriteLine(email);
        string? token = HttpContext.Session.GetString("_UserToken");
        return Settings();
    }
    [Route("Profile/Settings/UpdateDisplayName")]
    [HttpPost]
    public async Task<IActionResult> UpdateDisplayName(string dName)
    {
        Console.WriteLine("DisplayName:");
        Console.WriteLine(dName);
        string? token = HttpContext.Session.GetString("_UserToken");
        return Settings();
    }
    
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel {RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier});
    }
}