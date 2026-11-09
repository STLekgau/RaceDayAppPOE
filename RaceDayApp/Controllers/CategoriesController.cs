using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDayApp.Data;
using RaceDayApp.Models;

namespace RaceDayApp.Controllers
{
    [Route("api/categories")]
    [ApiController]
    public class CategoriesController : ControllerBase
    {
        private readonly RaceDayDbContext _context;
        public CategoriesController(RaceDayDbContext context) => _context = context;

        [HttpGet]
        public async Task<IActionResult> GetAllCategories() =>
            Ok(await _context.Categories.ToListAsync());

        [HttpPost]
        [Authorize(Policy = "OrganiserOnly")]
        public async Task<IActionResult> CreateCategory([FromBody] Category category)
        {
            _context.Categories.Add(category);
            await _context.SaveChangesAsync();
            return StatusCode(201, category);
        }
    }
}