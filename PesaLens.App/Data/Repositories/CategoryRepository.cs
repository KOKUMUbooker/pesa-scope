using PesaLens.App.Data.Repositories.Interfaces;
using SQLite;

namespace PesaLens.App.Repositories;

public class CategoryRepository(SQLiteAsyncConnection db)
    : BaseRepository<Category>(db), ICategoryRepository
{
    public Task<List<Category>> GetAllActiveAsync() =>
        _db.Table<Category>()
           .Where(c => !c.IsSystemCategory || c.Name != "__deleted__")
           .OrderBy(c => c.Name)
           .ToListAsync();

    public Task<List<Category>> GetUserCategoriesAsync() =>
        _db.Table<Category>()
           .Where(c => !c.IsSystemCategory)
           .OrderBy(c => c.Name)
           .ToListAsync();

    public async Task<Category?> GetByNameAsync(string name) =>
        await _db.Table<Category>()
                 .Where(c => c.Name.ToLower() == name.ToLower())
                 .FirstOrDefaultAsync();

    public async Task<Category> GetUncategorizedAsync()
    {
        var category = await _db.Table<Category>()
                                .Where(c => c.Name == "Uncategorized")
                                .FirstOrDefaultAsync();

        // Should always exist after seeding but guard just in case
        if (category is null)
        {
            category = new Category
            {
                Name = "Uncategorized",
                Icon = "help",
                Color = "#90A4AE",
                IsSystemCategory = true,
                CreatedAt = DateTime.UtcNow
            };
            await _db.InsertAsync(category);
        }

        return category;
    }

    public async Task SeedDefaultsAsync()
    {
        var defaultCategories = new List<Category>
        {
            new() { Name = "Food & Groceries",  Icon = "shopping-cart",  Color = "#D4522A", IsSystemCategory = true },
            new() { Name = "Transport",         Icon = "car",            Color = "#1A8C62", IsSystemCategory = true },
            new() { Name = "Utilities",         Icon = "bolt",           Color = "#C98A00", IsSystemCategory = true },
            new() { Name = "Airtime & Data",    Icon = "device-mobile",  Color = "#5C6BC0", IsSystemCategory = true },
            new() { Name = "Entertainment",     Icon = "movie",          Color = "#AB47BC", IsSystemCategory = true },
            new() { Name = "Health",            Icon = "heart",          Color = "#EF5350", IsSystemCategory = true },
            new() { Name = "Education",         Icon = "school",         Color = "#42A5F5", IsSystemCategory = true },
            new() { Name = "Shopping",          Icon = "tag",            Color = "#EC407A", IsSystemCategory = true },
            new() { Name = "Rent & Housing",    Icon = "home",           Color = "#8D6E63", IsSystemCategory = true },
            new() { Name = "Savings & Goals",   Icon = "piggy-bank",     Color = "#26A69A", IsSystemCategory = true },
            new() { Name = "Income",            Icon = "arrow-down",     Color = "#1A8C62", IsSystemCategory = true },
            new() { Name = "Uncategorized",     Icon = "help",           Color = "#90A4AE", IsSystemCategory = true },
        };

        foreach (var category in defaultCategories)
        {
            var existing = await GetByNameAsync(category.Name);
            if (existing is null)
            {
                category.CreatedAt = DateTime.UtcNow;
                await _db.InsertAsync(category);
            }
        }
    }

    public async Task DeleteAndReassignAsync(int categoryId)
    {
        var category = await _db.FindAsync<Category>(categoryId)
            ?? throw new InvalidOperationException($"Category {categoryId} not found.");

        if (category.IsSystemCategory)
            throw new InvalidOperationException("System categories cannot be deleted.");

        var uncategorized = await GetUncategorizedAsync();

        // Reassign all transactions from the deleted category
        await _db.ExecuteAsync(
            "UPDATE Transactions SET category_id = ? WHERE category_id = ?",
            uncategorized.Id, categoryId);

        // Delete the category's rules
        await _db.ExecuteAsync(
            "DELETE FROM AutoCategorizationRules WHERE category_id = ?",
            categoryId);

        // Delete associated budget if any
        await _db.ExecuteAsync(
            "DELETE FROM Budgets WHERE category_id = ?",
            categoryId);

        await _db.DeleteAsync(category);
    }
}