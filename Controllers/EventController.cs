using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using H4G_Project.DAL;
using H4G_Project.Models;
using Newtonsoft.Json;
using System.Threading.Tasks;
using Google.Cloud.Firestore;
using Microsoft.AspNetCore.Http;
using System.Diagnostics;
using Google.Cloud.Firestore.V1;
using System.Dynamic;


namespace H4G_Project.Controllers
{
    public class EventController : Controller
    {
        UserDAL userContext = new UserDAL();
        EventsDAL eventContext = new EventsDAL();
        public IActionResult Event()
        {
            return View();
        }
        // GET: EventController

        // GET: EventController/Details/5
        public ActionResult Details(int id)
        {
            return View();
        }

        public async Task<ActionResult> Index(int eventId)
        {
            TempData["EventID"] = eventId;
            TempData.Keep("EventID");
            dynamic mymodel = new ExpandoObject();
            mymodel.Event = await eventContext.GetAllEvents();
            return View(mymodel);

        }

        public async Task<ActionResult> DetailedIndex(int eventId)
        {
            TempData["EventID"] = eventId;
            List<Event> eventList = await eventContext.GetAllEvents();
            return View(eventList);
        }
    }
}