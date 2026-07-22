using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PrimaEstates.Data;
using PrimaEstates.Models;
using PrimaEstates.Services;

namespace PrimaEstates.Controllers;

[Authorize(Roles = "Admin")]
public class AdminController : Controller
{
    private readonly AppDbContext _db;
    private readonly IImageStorage _storage;

    public AdminController(AppDbContext db, IImageStorage storage)
    {
        _db = db;
        _storage = storage;
    }

    // ---------- Dashboard ----------
    public async Task<IActionResult> Dashboard()
    {
        ViewBag.TotalProperties = await _db.Properties.CountAsync();
        ViewBag.ForSale = await _db.Properties.CountAsync(p => p.ListingType == ListingType.Sale && p.Status == PropertyStatus.Available);
        ViewBag.ForRent = await _db.Properties.CountAsync(p => p.ListingType == ListingType.Rent && p.Status == PropertyStatus.Available);
        ViewBag.SoldOrRented = await _db.Properties.CountAsync(p => p.Status == PropertyStatus.Sold || p.Status == PropertyStatus.Rented);
        ViewBag.UnreadEnquiries = await _db.Enquiries.CountAsync(e => !e.IsRead);
        ViewBag.AgentCount = await _db.Agents.CountAsync();

        ViewBag.RecentEnquiries = await _db.Enquiries
            .Include(e => e.Property)
            .OrderByDescending(e => e.CreatedAt)
            .Take(5).ToListAsync();

        ViewBag.RecentProperties = await _db.Properties
            .OrderByDescending(p => p.CreatedAt)
            .Take(5).ToListAsync();

        return View();
    }

    // ---------- Properties ----------
    public async Task<IActionResult> Properties(string? q)
    {
        var query = _db.Properties.Include(p => p.Agent).AsQueryable();
        if (!string.IsNullOrWhiteSpace(q))
            query = query.Where(p => p.Title.Contains(q) || p.City.Contains(q));
        ViewBag.Query = q;
        return View(await query.OrderByDescending(p => p.CreatedAt).ToListAsync());
    }

    [HttpGet]
    public async Task<IActionResult> PropertyForm(int? id)
    {
        Property model = id.HasValue
            ? await _db.Properties.Include(p => p.Images).FirstOrDefaultAsync(p => p.Id == id) ?? new Property()
            : new Property();

        ViewBag.Agents = await _db.Agents.OrderBy(a => a.Name).ToListAsync();
        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> PropertyForm(Property model, IFormFile? coverFile, List<IFormFile>? galleryFiles)
    {
        ViewBag.Agents = await _db.Agents.OrderBy(a => a.Name).ToListAsync();
        if (!ModelState.IsValid) return View(model);

        if (coverFile != null && coverFile.Length > 0)
        {
            var url = await _storage.SaveAsync(coverFile);
            if (url != null) model.CoverImageUrl = url;
        }

        if (model.Id == 0)
        {
            model.CreatedAt = DateTime.UtcNow;
            _db.Properties.Add(model);
        }
        else
        {
            var existing = await _db.Properties.FindAsync(model.Id);
            if (existing == null) return NotFound();
            var originalCreatedAt = existing.CreatedAt;
            _db.Entry(existing).CurrentValues.SetValues(model);
            existing.CreatedAt = originalCreatedAt == default ? DateTime.UtcNow : originalCreatedAt;
        }
        await _db.SaveChangesAsync();

        if (galleryFiles != null)
        {
            var maxSort = await _db.PropertyImages
                .Where(i => i.PropertyId == model.Id)
                .Select(i => (int?)i.SortOrder).MaxAsync() ?? -1;

            foreach (var file in galleryFiles.Where(f => f.Length > 0))
            {
                var url = await _storage.SaveAsync(file);
                if (url != null)
                    _db.PropertyImages.Add(new PropertyImage { PropertyId = model.Id, Url = url, SortOrder = ++maxSort });
            }
            await _db.SaveChangesAsync();
        }

        TempData["Success"] = "Property saved.";
        return RedirectToAction(nameof(Properties));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteProperty(int id)
    {
        var p = await _db.Properties.FindAsync(id);
        if (p != null)
        {
            _db.Properties.Remove(p);
            await _db.SaveChangesAsync();
            TempData["Success"] = "Property deleted.";
        }
        return RedirectToAction(nameof(Properties));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteImage(int id, int propertyId)
    {
        var img = await _db.PropertyImages.FindAsync(id);
        if (img != null)
        {
            _db.PropertyImages.Remove(img);
            await _db.SaveChangesAsync();
        }
        return RedirectToAction(nameof(PropertyForm), new { id = propertyId });
    }

    // ---------- Agents ----------
    public async Task<IActionResult> Agents() =>
        View(await _db.Agents.Include(a => a.Properties).OrderBy(a => a.Name).ToListAsync());

    [HttpGet]
    public async Task<IActionResult> AgentForm(int? id)
    {
        var model = id.HasValue ? await _db.Agents.FindAsync(id) ?? new Agent() : new Agent();
        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AgentForm(Agent model, IFormFile? photoFile)
    {
        if (!ModelState.IsValid) return View(model);

        if (photoFile != null && photoFile.Length > 0)
        {
            var url = await _storage.SaveAsync(photoFile);
            if (url != null) model.PhotoUrl = url;
        }

        if (model.Id == 0) _db.Agents.Add(model);
        else _db.Agents.Update(model);

        await _db.SaveChangesAsync();
        TempData["Success"] = "Agent saved.";
        return RedirectToAction(nameof(Agents));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteAgent(int id)
    {
        var a = await _db.Agents.FindAsync(id);
        if (a != null)
        {
            _db.Agents.Remove(a);
            await _db.SaveChangesAsync();
            TempData["Success"] = "Agent deleted.";
        }
        return RedirectToAction(nameof(Agents));
    }

    // ---------- Enquiries ----------
    public async Task<IActionResult> Enquiries() =>
        View(await _db.Enquiries.Include(e => e.Property)
            .OrderByDescending(e => e.CreatedAt).ToListAsync());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleEnquiryRead(int id)
    {
        var e = await _db.Enquiries.FindAsync(id);
        if (e != null)
        {
            e.IsRead = !e.IsRead;
            await _db.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Enquiries));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteEnquiry(int id)
    {
        var e = await _db.Enquiries.FindAsync(id);
        if (e != null)
        {
            _db.Enquiries.Remove(e);
            await _db.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Enquiries));
    }
}
