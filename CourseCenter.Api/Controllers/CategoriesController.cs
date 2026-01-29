using CourseCenter.Api;
using CourseCenter.Api.Categories;
using CourseCenter.Api.Common.DTOs;
using CourseCenter.Api.Common;
using CourseCenter.Api.Migrations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;


[ApiController]
[Route("api/categories")]
public class CategoriesController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public CategoriesController(ApplicationDbContext context)
    {
        _context = context;
    }

    // =========================
    // GET: api/categories
    // =========================
    [HttpGet]
    [Authorize(Policy = "Categories.View")]
    public IActionResult GetAll([FromQuery] PagedRequest request)
    {
        // defaults
        if (request == null)
            request = new PagedRequest { PageNumber = 1, PageSize = 10 };

        if (request.PageNumber <= 0)
            request.PageNumber = 1;

        if (request.PageSize <= 0)
            request.PageSize = 10;

        request.PageSize = Math.Min(request.PageSize, 100);

        var query = _context.Categories
            .Where(c => c.IsActive)
            .AsQueryable();

        // total before pagination
        var totalCount = query.Count();

        var dataAnon = query
            .OrderBy(c => c.Name)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(c => new
            {
                c.Id,
                c.Name
            })
            .ToList();

        var data = dataAnon.Cast<object>().ToList();

        var response = new PagedResponse<object>
        {
            Data = data,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize,
            TotalCount = totalCount
        };

        return Ok(response);
    }

    // =========================
    // POST: api/categories
    // =========================
    [HttpPost]
    [Authorize(Policy = "Categories.Create")]
    public IActionResult Create([FromBody] CategoryRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest("Category name is required");

        var name = request.Name.Trim();

        if (_context.Categories.Any(c => c.Name == name))
            return BadRequest("Category already exists");

        var category = new Category
        {
            Name = name,
            IsActive = true
        };

        _context.Categories.Add(category);
        _context.SaveChanges();

        return Ok("Category created successfully");
    }

    // =========================
    // PUT: api/categories/{id}
    // =========================
    [HttpPut("{id}")]
    [Authorize(Policy = "Categories.Edit")]
    public IActionResult Update(int id, [FromBody] CategoryRequest request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.Name))
            return BadRequest("Category name is required");

        var category = _context.Categories.FirstOrDefault(c => c.Id == id);
        if (category == null)
            return NotFound("Category not found");

        var newName = request.Name.Trim();

        // لو الاسم نفسه بدون تغيير
        if (category.Name.Trim().Equals(newName, StringComparison.OrdinalIgnoreCase))
        {
            return Ok(new
            {
                category.Id,
                category.Name,
                category.IsActive
            });
        }

        var nameExists = _context.Categories.Any(c =>
            c.Id != id &&
            c.Name.Trim().ToLower() == newName.ToLower()
        );

        if (nameExists)
            return BadRequest("Another category with the same name already exists");

        category.Name = newName;
        _context.SaveChanges();

        return Ok(new
        {
            category.Id,
            category.Name,
            category.IsActive
        });
    }



    // =========================
    // PUT: api/categories/{id}/disable
    // =========================
    [HttpPut("{id}/disable")]
    [Authorize(Policy = "Categories.Delete")]
    public IActionResult Disable(int id)
    {
        var category = _context.Categories.FirstOrDefault(c => c.Id == id);
        if (category == null)
            return NotFound("Category not found");

        category.IsActive = false;
        _context.SaveChanges();

        return Ok("Category disabled successfully");
    }

   
    // =========================
    // DELETE: api/categories/{id}
    // =========================
    [HttpDelete("{id}")]
    [Authorize(Policy = "Categories.Delete")]
    public IActionResult Delete(int id)
    {
        var category = _context.Categories.FirstOrDefault(c => c.Id == id);
        if (category == null)
            return NotFound("Category not found");

        if (!category.IsActive)
            return BadRequest("Category already deleted");

        category.IsActive = false;
        _context.SaveChanges();

        return Ok("Category deleted successfully");
    }
    // =========================
    // PUT: api/categories/{id}/enable
    // =========================
    [HttpPut("{id}/enable")]
    [Authorize(Policy = "Categories.Edit")]
    public IActionResult Enable(int id)
    {
        var category = _context.Categories.FirstOrDefault(c => c.Id == id);
        if (category == null)
            return NotFound("Category not found");

        if (category.IsActive)
            return BadRequest("Category already active");

        category.IsActive = true;
        _context.SaveChanges();

        return Ok("Category enabled successfully");
    }
    // =========================
    // GET: api/categories/{id}
    // =========================
    [HttpGet("{id}")]
    [Authorize(Policy = "Categories.View")]
    public IActionResult GetById(int id)
    {
        var category = _context.Categories
            .Where(c => c.Id == id)
            .Select(c => new
            {
                c.Id,
                c.Name,
                c.IsActive
            })
            .FirstOrDefault();

        if (category == null)
            return NotFound("Category not found");

        return Ok(category);
    }
    // =========================
    // PUT: api/categories/{id}/toggle
    // =========================
    [HttpPut("{id}/toggle")]
    [Authorize(Policy = "Categories.Edit")]
    public IActionResult Toggle(int id)
    {
        var category = _context.Categories.FirstOrDefault(c => c.Id == id);
        if (category == null)
            return NotFound("Category not found");

        category.IsActive = !category.IsActive;
        _context.SaveChanges();

        return Ok(new
        {
            category.Id,
            category.IsActive
        });
    }

}
