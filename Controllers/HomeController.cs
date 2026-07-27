using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PrimaEstates.Data;
using PrimaEstates.Models;

namespace PrimaEstates.Controllers;

public class HomeController : Controller
{
    private readonly AppDbContext _db;
    public HomeController(AppDbContext db) => _db = db;

    public async Task<IActionResult> Index()
    {
        var featured = await _db.Properties
            .Where(p => p.IsFeatured && p.Status == PropertyStatus.Available)
            .OrderByDescending(p => p.CreatedAt)
            .Take(5)
            .ToListAsync();

        var latest = await _db.Properties
            .Where(p => p.Status == PropertyStatus.Available)
            .OrderByDescending(p => p.CreatedAt)
            .Take(5)
            .ToListAsync();

        ViewBag.Featured = featured;
        ViewBag.Latest = latest;
        ViewBag.Cities = await _db.Properties.Select(p => p.City).Distinct().OrderBy(c => c).ToListAsync();
        return View();
    }

    public async Task<IActionResult> Agents()
    {
        var agents = await _db.Agents
            .Include(a => a.Properties)
            .OrderBy(a => a.Name)
            .ToListAsync();
        return View(agents);
    }

    public IActionResult Contact() => View();

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Contact(Enquiry enquiry)
    {
        enquiry.PropertyId = null;
        if (!ModelState.IsValid) return View(enquiry);

        _db.Enquiries.Add(enquiry);
        await _db.SaveChangesAsync();
        TempData["Success"] = "Thank you — we received your message and will be in touch within one working day.";
        return RedirectToAction(nameof(Contact));
    }

    public IActionResult Error() => View();
}
