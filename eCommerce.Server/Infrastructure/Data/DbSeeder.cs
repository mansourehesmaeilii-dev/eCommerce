using eCommerce.Server.Application.Helpers;
using eCommerce.Server.Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace eCommerce.Server.Infrastructure.Data;

public static class DbSeeder
{
    private static readonly string[] Roles = ["Admin", "Customer"];

    public static async Task SeedAsync(IServiceProvider services)
    {
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = services.GetRequiredService<UserManager<AppUser>>();
        var db = services.GetRequiredService<AppDbContext>();

        // ── Roles ──────────────────────────────────────────────────────────
        foreach (var role in Roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));
        }

        // ── Admin user ────────────────────────────────────────────────────
        const string adminEmail = "admin@ecommerce.dev";
        if (await userManager.FindByEmailAsync(adminEmail) is null)
        {
            var admin = new AppUser
            {
                FirstName = "Admin",
                LastName = "User",
                Email = adminEmail,
                UserName = adminEmail,
                EmailConfirmed = true
            };
            var result = await userManager.CreateAsync(admin, "Admin@123456");
            if (result.Succeeded)
                await userManager.AddToRoleAsync(admin, "Admin");
        }

        // ── Demo customer ─────────────────────────────────────────────────
        const string customerEmail = "customer@ecommerce.dev";
        if (await userManager.FindByEmailAsync(customerEmail) is null)
        {
            var customer = new AppUser
            {
                FirstName = "Jane",
                LastName = "Doe",
                Email = customerEmail,
                UserName = customerEmail,
                EmailConfirmed = true
            };
            var result = await userManager.CreateAsync(customer, "Customer@123456");
            if (result.Succeeded)
                await userManager.AddToRoleAsync(customer, "Customer");
        }

        // ── Categories ────────────────────────────────────────────────────
        if (!db.Categories.Any())
        {
            var categories = new List<Category>
            {
                new() { Name = "Electronics",   Slug = "electronics",   Description = "Gadgets and devices",       IsActive = true },
                new() { Name = "Clothing",       Slug = "clothing",       Description = "Apparel for all seasons", IsActive = true },
                new() { Name = "Books",          Slug = "books",          Description = "Fiction and non-fiction",  IsActive = true },
                new() { Name = "Home & Garden",  Slug = "home-garden",    Description = "Everything for the home", IsActive = true },
            };
            db.Categories.AddRange(categories);
            await db.SaveChangesAsync();
        }

        // ── Products ──────────────────────────────────────────────────────
        if (!db.Products.Any())
        {
            var electronics = db.Categories.First(c => c.Slug == "electronics").Id;
            var clothing    = db.Categories.First(c => c.Slug == "clothing").Id;
            var books       = db.Categories.First(c => c.Slug == "books").Id;

            var products = new List<Product>
            {
                new() { Name = "Wireless Noise-Cancelling Headphones", Slug = "wireless-noise-cancelling-headphones",
                        Description = "Premium over-ear headphones with 30h battery life.", Price = 149.99m,
                        CompareAtPrice = 199.99m, StockQuantity = 50, Brand = "SoundMax",
                        Sku = "EL-001", IsFeatured = true, IsActive = true, CategoryId = electronics },

                new() { Name = "Mechanical Keyboard TKL", Slug = "mechanical-keyboard-tkl",
                        Description = "Tenkeyless mechanical keyboard with RGB backlighting.", Price = 89.99m,
                        StockQuantity = 30, Brand = "KeyCraft", Sku = "EL-002",
                        IsFeatured = false, IsActive = true, CategoryId = electronics },

                new() { Name = "4K Webcam Pro", Slug = "4k-webcam-pro",
                        Description = "4K streaming webcam with auto-focus and built-in ring light.", Price = 119.99m,
                        CompareAtPrice = 149.99m, StockQuantity = 20, Brand = "VisionTech",
                        Sku = "EL-003", IsFeatured = true, IsActive = true, CategoryId = electronics },

                new() { Name = "Classic Fit Oxford Shirt", Slug = "classic-fit-oxford-shirt",
                        Description = "100% cotton Oxford shirt, available in multiple colours.", Price = 49.99m,
                        StockQuantity = 100, Brand = "StyleCo", Sku = "CL-001",
                        IsFeatured = false, IsActive = true, CategoryId = clothing },

                new() { Name = "Slim-Fit Chino Trousers", Slug = "slim-fit-chino-trousers",
                        Description = "Versatile slim-fit chinos for everyday wear.", Price = 59.99m,
                        CompareAtPrice = 79.99m, StockQuantity = 80, Brand = "StyleCo",
                        Sku = "CL-002", IsFeatured = true, IsActive = true, CategoryId = clothing },

                new() { Name = "Clean Code", Slug = "clean-code",
                        Description = "A handbook of agile software craftsmanship by Robert C. Martin.", Price = 34.99m,
                        StockQuantity = 200, Brand = "Prentice Hall", Sku = "BK-001",
                        IsFeatured = true, IsActive = true, CategoryId = books },

                new() { Name = "The Pragmatic Programmer", Slug = "the-pragmatic-programmer",
                        Description = "Your journey to mastery, 20th anniversary edition.", Price = 39.99m,
                        StockQuantity = 150, Brand = "Addison-Wesley", Sku = "BK-002",
                        IsFeatured = false, IsActive = true, CategoryId = books },
            };

            db.Products.AddRange(products);
            await db.SaveChangesAsync();
        }
    }
}
