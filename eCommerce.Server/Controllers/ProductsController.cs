using eCommerce.Server.Application.Common.Results;
using eCommerce.Server.Application.DTOs.Common;
using eCommerce.Server.Application.DTOs.Products;
using eCommerce.Server.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace eCommerce.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController(IProductService productService) : ControllerBase
{
    // GET api/products
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] ProductQueryParams query)
    {
        var result = await productService.GetAllAsync(query);
        return Ok(result);
    }

    // GET api/products/5
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await productService.GetByIdAsync(id);
        return ToActionResult(result);
    }

    // GET api/products/slug/{slug}
    [HttpGet("slug/{slug}")]
    public async Task<IActionResult> GetBySlug(string slug)
    {
        var result = await productService.GetBySlugAsync(slug);
        return ToActionResult(result);
    }

    // POST api/products  [Admin]
    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] UpsertProductRequest request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var result = await productService.CreateAsync(request);
        if (!result.Success)
            return ToActionResult(result);

        return CreatedAtAction(nameof(GetById), new { id = result.Data!.Id }, result.Data);
    }

    // PUT api/products/5  [Admin]
    [Authorize(Roles = "Admin")]
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpsertProductRequest request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var result = await productService.UpdateAsync(id, request);
        return ToActionResult(result);
    }

    // DELETE api/products/5  [Admin]
    [Authorize(Roles = "Admin")]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await productService.DeleteAsync(id);
        return ToActionResult(result);
    }

    // POST api/products/5/images  [Admin]
    [Authorize(Roles = "Admin")]
    [HttpPost("{id:int}/images")]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<IActionResult> UploadImage(int id, IFormFile file)
    {
        var result = await productService.UploadImageAsync(id, file);
        return ToActionResult(result);
    }

    // DELETE api/products/5/images/3  [Admin]
    [Authorize(Roles = "Admin")]
    [HttpDelete("{productId:int}/images/{imageId:int}")]
    public async Task<IActionResult> DeleteImage(int productId, int imageId)
    {
        var result = await productService.DeleteImageAsync(productId, imageId);
        return ToActionResult(result);
    }

    private IActionResult ToActionResult(Result result) =>
        result.Success
            ? result.StatusCode switch
            {
                StatusCodes.Status204NoContent => NoContent(),
                _ => Ok()
            }
            : result.StatusCode switch
            {
                StatusCodes.Status404NotFound => NotFound(result.Error),
                StatusCodes.Status400BadRequest => BadRequest(result.Error),
                _ => StatusCode(result.StatusCode, result.Error)
            };

    private IActionResult ToActionResult<T>(Result<T> result) =>
        result.Success
            ? result.StatusCode switch
            {
                StatusCodes.Status201Created => StatusCode(StatusCodes.Status201Created, result.Data),
                _ => Ok(result.Data)
            }
            : result.StatusCode switch
            {
                StatusCodes.Status404NotFound => NotFound(result.Error),
                StatusCodes.Status400BadRequest => BadRequest(result.Error),
                _ => StatusCode(result.StatusCode, result.Error)
            };
}
