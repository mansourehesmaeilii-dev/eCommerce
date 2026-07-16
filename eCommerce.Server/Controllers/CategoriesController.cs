using eCommerce.Server.Application.DTOs.Products;
using eCommerce.Server.Application.Helpers;
using eCommerce.Server.Domain.Entities;
using eCommerce.Server.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace eCommerce.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CategoriesController(AppDbContext db) : ControllerBase
{
    // GET api/categories
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var categories = await db.Categories
            .Where(c => c.IsActive)
            .Select(c => new CategoryDto
            {
                Id = c.Id,
                Name = c.Name,
                Slug = c.Slug,
                Description = c.Description,
                ImageUrl = c.ImageUrl,
                ProductCount = c.Products.Count(p => p.IsActive)
            })
            .OrderBy(c => c.Name)
            .ToListAsync();

        return Ok(categories);
    }

    // GET api/categories/5
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var category = await db.Categories
            .Where(c => c.Id == id && c.IsActive)
            .Select(c => new CategoryDto
            {
                Id = c.Id,
                Name = c.Name,
                Slug = c.Slug,
                Description = c.Description,
                ImageUrl = c.ImageUrl,
                ProductCount = c.Products.Count(p => p.IsActive)
            })
            .FirstOrDefaultAsync();

        return category is null ? NotFound() : Ok(category);
    }

    // POST api/categories  [Admin]
    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] UpsertCategoryRequest request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var slug = await UniqueSlugAsync(SlugHelper.Generate(request.Name));

        var category = new Category
        {
            Name = request.Name,
            Slug = slug,
            Description = request.Description,
            ImageUrl = request.ImageUrl,
            IsActive = request.IsActive
        };

        db.Categories.Add(category);
        await db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = category.Id },
            new CategoryDto { Id = category.Id, Name = category.Name, Slug = category.Slug });
    }

    // PUT api/categories/5  [Admin]
    [Authorize(Roles = "Admin")]
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpsertCategoryRequest request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var category = await db.Categories.FindAsync(id);
        if (category is null) return NotFound();

        category.Name = request.Name;
        category.Slug = await UniqueSlugAsync(SlugHelper.Generate(request.Name), id);
        category.Description = request.Description;
        category.ImageUrl = request.ImageUrl;
        category.IsActive = request.IsActive;

        await db.SaveChangesAsync();
        return NoContent();
    }

    // DELETE api/categories/5  [Admin]
    [Authorize(Roles = "Admin")]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var category = await db.Categories.FindAsync(id);
        if (category is null) return NotFound();

        var hasProducts = await db.Products.AnyAsync(p => p.CategoryId == id);
        if (hasProducts) return Conflict("Cannot delete a category that has products.");

        db.Categories.Remove(category);
        await db.SaveChangesAsync();
        return NoContent();
    }

    private async Task<string> UniqueSlugAsync(string baseSlug, int? excludeId = null)
    {
        var slug = baseSlug;
        var counter = 1;
        while (await db.Categories.AnyAsync(c => c.Slug == slug && c.Id != excludeId))
            slug = $"{baseSlug}-{counter++}";
        return slug;
    }
}
