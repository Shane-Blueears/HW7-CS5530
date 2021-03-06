﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using LibraryWebServer;
using LibraryWebServer.Models;
using System.Globalization;

namespace LibraryWebServer.Controllers
{
    public class HomeController : Controller
    {
        // WARNING:
        // This very simple web server is designed to be as tiny and simple as possible
        // This is NOT the way to save user data.
        // This will only allow one user of the web server at a time (aside from major security concerns).
        private static string user = "";
        private static int card = -1;

        /// <summary>
        /// Given a Patron name and CardNum, verify that they exist and match in the database.
        /// If the login is successful, sets the global variables "user" and "card"
        /// </summary>
        /// <param name="name">The Patron's name</param>
        /// <param name="cardnum">The Patron's card number</param>
        /// <returns>A JSON object with a single field: "success" with a boolean value:
        /// true if the login is accepted, false otherwise.
        /// </returns>
        [HttpPost]
        public IActionResult CheckLogin(string name, int cardnum)
        {
            // TODO: Fill in. Determine if login is successful or not.
            bool loginSuccessful = false;
            using (Team69LibraryContext db = new Team69LibraryContext())
            {
                var query =
                from p in db.Patrons
                select p;

                String userName = name;
                userName = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(userName.ToLower());
                foreach (var v in query)
                {
                    if (userName.Equals(v.Name)&& cardnum == v.CardNum)
                    {
                        loginSuccessful = true;
                    }
                    //System.Diagnostics.Debug.WriteLine(v.CardNum);
                    //System.Diagnostics.Debug.WriteLine(v.Name);
                }
            }

            if (!loginSuccessful)
            {
                return Json(new { success = false });
            }
            else
            {
                user = name;
                card = cardnum;
                return Json(new { success = true });
            }
        }


        /// <summary>
        /// Logs a user out. This is implemented for you.
        /// </summary>
        /// <returns>Success</returns>
        [HttpPost]
        public ActionResult LogOut()
        {
            user = "";
            card = -1;
            return Json(new { success = true });
        }

        /// <summary>
        /// Returns a JSON array representing all known books.
        /// Each book should contain the following fields:
        /// {"isbn" (string), "title" (string), "author" (string), "serial" (uint?), "name" (string)}
        /// Every object in the list should have isbn, title, and author.
        /// Books that are not in the Library's inventory (such as Dune) should have a null serial.
        /// The "name" field is the name of the Patron who currently has the book checked out (if any)
        /// Books that are not checked out should have an empty string "" for name.
        /// </summary>
        /// <returns>The JSON representation of the books</returns>
        [HttpPost]
        public ActionResult AllTitles()
        {
      using(Team69LibraryContext db = new Team69LibraryContext())
            {
                var query =
                    from title in db.Titles
                    join inven in db.Inventory on title.Isbn equals inven.Isbn into tempTable
                    from tTable in tempTable.DefaultIfEmpty()

                    //from anotherTitle in db.Titles
                    //join inventory in db.Inventory on anotherTitle.Isbn equals inventory.Isbn
                    join checkedO in db.CheckedOut on tTable.Serial equals checkedO.Serial into tempTable2
                    from tTable2 in tempTable2.DefaultIfEmpty()

                    join patr in db.Patrons on tTable2.CardNum equals patr.CardNum into tempTable3
                    from tTable3 in tempTable3.DefaultIfEmpty()

                    select new
                    {
                        title.Title,
                        title.Author,
                        title.Isbn,
                        Serial = (tTable == null ? (uint?)null : tTable.Serial),
                        name = (tTable2 == null ? "" : tTable3.Name)

                    };
                String a = "";
                return Json(query.ToArray());
            }

        }

        /// <summary>
        /// Returns a JSON array representing all books checked out by the logged in user 
        /// The logged in user is tracked by the global variable "card".
        /// Every object in the array should contain the following fields:
        /// {"title" (string), "author" (string), "serial" (uint) (note this is not a nullable uint) }
        /// Every object in the list should have a valid (non-null) value for each field.
        /// </summary>
        /// <returns>The JSON representation of the books</returns>
        [HttpPost]
        public ActionResult ListMyBooks()
        {
            
            using (Team69LibraryContext db = new Team69LibraryContext())
            {
                var query =
                    (from patron in db.Patrons 
                     join checkedOut in db.CheckedOut on patron.CardNum equals checkedOut.CardNum
                     join inventory in db.Inventory on checkedOut.Serial equals inventory.Serial
                     join title in db.Titles on inventory.Isbn equals title.Isbn
                     where patron.Name == user
                     select new
                     {
                         title.Title,
                         title.Author,
                         checkedOut.Serial,

                     }).ToList();
                return Json(query);
            }

                //return Json(null);
        }


        /// <summary>
        /// Updates the database to represent that
        /// the given book is checked out by the logged in user (global variable "card").
        /// In other words, insert a row into the CheckedOut table.
        /// You can assume that the book is not currently checked out by anyone.
        /// </summary>
        /// <param name="serial">The serial number of the book to check out</param>
        /// <returns>success</returns>
        [HttpPost]
        public ActionResult CheckOutBook(int serial)
        {
            // You may have to cast serial to a (uint)
            using (Team69LibraryContext db = new Team69LibraryContext())
            {
                CheckedOut book = new CheckedOut
                {
                    CardNum = (uint)card,
                    Serial = (uint)serial

                };

                db.CheckedOut.Add(book);
                db.SaveChanges();
        
            }
            return Json(new { success = true });
        }

        
        /// <summary>
        /// Returns a book currently checked out by the logged in user (global variable "card").
        /// In other words, removes a row from the CheckedOut table.
        /// You can assume the book is checked out by the user.
        /// </summary>
        /// <param name="serial">The serial number of the book to return</param>
        /// <returns>Success</returns>
        [HttpPost]
        public ActionResult ReturnBook(int serial)
        {
            // You may have to cast serial to a (uint)
            using(Team69LibraryContext db = new Team69LibraryContext())
            {
                CheckedOut book = new CheckedOut
                {
                    CardNum = (uint)card,
                    Serial = (uint)serial
                };
                db.CheckedOut.Remove(book);
                db.SaveChanges();
            }

            return Json(new { success = true });
        }

        /*******************************************/
        /****** Do not modify below this line ******/
        /*******************************************/

        /// <summary>
        /// Return the home page.
        /// </summary>
        /// <returns></returns>
        public IActionResult Index()
        {
            if (user == "" && card == -1)
                return View("Login");

            return View();
        }

        /// <summary>
        /// Return the MyBooks page.
        /// </summary>
        /// <returns></returns>
        public IActionResult MyBooks()
        {
            if (user == "" && card == -1)
                return View("Login");

            return View();
        }

        /// <summary>
        /// Return the About page.
        /// </summary>
        /// <returns></returns>
        public IActionResult About()
        {
            ViewData["Message"] = "Your application description page.";

            return View();
        }

        /// <summary>
        /// Return the Login page.
        /// </summary>
        /// <returns></returns>
        public IActionResult Login()
        {
            user = "";
            card = -1;

            ViewData["Message"] = "Please login.";

            return View();
        }


        /// <summary>
        /// Return the Contact page.
        /// </summary>
        /// <returns></returns>
        public IActionResult Contact()
        {
            ViewData["Message"] = "Your contact page.";

            return View();
        }

        /// <summary>
        /// Return the Error page.
        /// </summary>
        /// <returns></returns>
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}

