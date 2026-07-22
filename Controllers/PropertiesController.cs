using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PrimaEstates.Data;
using PrimaEstates.Models;

namespace PrimaEstates.Controllers;

public class PropertiesController : Controller
{
    private const int PageSize = 9;
    private readonly AppDbContext _db;
    public PropertiesController(AppDbContext db) => _db = db;

    public async Task<IActionResult> Index(
        string? q, ListingType? listingType, PropertyType? type,
        string? city, decimal? minPrice, decimal? maxPrice, int? beds, int page = 1)
    {
        var query = _db.Properties
            .Where(p => p.Status == PropertyStatus.Available)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(q))
            query = query.Where(p => p.Title.Contains(q) || p.City.Contains(q) || p.Address.Contains(q));
        if (listingType.HasValue) query = query.Where(p => p.ListingType == listingType);
        if (type.HasValue) query = query.Where(p => p.Type == type);
        if (!string.IsNullOrWhiteSpace(city)) query = query.Where(p => p.City == city);
        if (minPrice.HasValue) query = query.Where(p => p.Price >= minPrice);
        if (maxPrice.HasValue) query = query.Where(p => p.Price <= maxPrice);
        if (beds.HasValue) query = query.Where(p => p.Bedrooms >= beds);

        var total = await query.CountAsync();
        page = Math.Max(1, page);
        var items = await query
            .OrderByDescending(p => p.IsFeatured)
            .ThenByDescending(p => p.CreatedAt)
            .Skip((page - 1) * PageSize)
            .Take(PageSize)
            .ToListAsync();

        ViewBag.Total = total;
        ViewBag.Page = page;
        ViewBag.TotalPages = (int)Math.Ceiling(total / (double)PageSize);
        ViewBag.Cities = await _db.Properties.Select(p => p.City).Distinct().OrderBy(c => c).ToListAsync();
        return View(items);
    }

    public async Task<IActionResult> Details(int id)
    {
        var property = await _db.Properties
            .Include(p => p.Agent)
            .Include(p => p.Images.OrderBy(i => i.SortOrder))
            .FirstOrDefaultAsync(p => p.Id == id);

        if (property == null || property.Status == PropertyStatus.Hidden) return NotFound();

        ViewBag.Similar = await _db.Properties
            .Where(p => p.Id != id && p.City == property.City && p.Status == PropertyStatus.Available)
            .Take(3).ToListAsync();

        return View(property);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Enquire(Enquiry enquiry)
    {
        if (!ModelState.IsValid)
        {
            TempData["EnquiryError"] = "Please fill in your name, a valid email, and a message.";
            return RedirectToAction(nameof(Details), new { id = enquiry.PropertyId });
        }

        _db.Enquiries.Add(enquiry);
        await _db.SaveChangesAsync();
        TempData["EnquirySuccess"] = "Enquiry sent. The listing agent will contact you shortly.";
        return RedirectToAction(nameof(Details), new { id = enquiry.PropertyId });
    }
}
