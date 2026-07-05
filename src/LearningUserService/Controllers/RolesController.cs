using learning_user_service.Models.DTOs;
using learning_user_service.Services;
using Microsoft.AspNetCore.Mvc;

namespace learning_user_service.Controllers;

[ApiController]
[Route("api/roles")]
public class RolesController(IRoleService roleService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<RoleDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetRoles(CancellationToken ct)
    {
        var result = await roleService.GetAllAsync(ct);

        return result.IsSuccess
            ? Ok(result.Value)
            : Problem(detail: string.Join("; ", result.Errors.Select(e => e.Message)), statusCode: StatusCodes.Status500InternalServerError);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(RoleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetRoleById(Guid id, CancellationToken ct)
    {
        var result = await roleService.GetByIdAsync(id, ct);

        if (result.IsSuccess)
        {
            return Ok(result.Value);
        }

        if (result.Errors.Any(e => e.Metadata.ContainsKey(RoleService.NotFoundMetadataKey)))
        {
            return NotFound();
        }

        return Problem(detail: string.Join("; ", result.Errors.Select(e => e.Message)), statusCode: StatusCodes.Status500InternalServerError);
    }

    [HttpPost]
    [ProducesResponseType(typeof(RoleDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreateRole([FromBody] string name, CancellationToken ct)
    {
        var result = await roleService.CreateAsync(name, ct);

        if (result.IsSuccess)
        {
            return CreatedAtAction(nameof(GetRoleById), new { id = result.Value.Id }, result.Value);
        }

        return Problem(detail: string.Join("; ", result.Errors.Select(e => e.Message)), statusCode: StatusCodes.Status500InternalServerError);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DeleteRole(Guid id, CancellationToken ct)
    {
        var result = await roleService.DeleteAsync(id, ct);

        if (result.IsSuccess)
        {
            return NoContent();
        }

        return Problem(detail: string.Join("; ", result.Errors.Select(e => e.Message)), statusCode: StatusCodes.Status500InternalServerError);
    }

    [HttpPost("assign")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> AssignRoleToUser([FromBody] AssignRoleRequest request, CancellationToken ct)
    {
        var result = await roleService.AssignRoleToUserAsync(request, ct);

        if (result.IsSuccess)
        {
            return NoContent();
        }

        if (result.Errors.Any(e => e.Metadata.ContainsKey(RoleService.NotFoundMetadataKey)))
        {
            return NotFound();
        }

        return Problem(detail: string.Join("; ", result.Errors.Select(e => e.Message)), statusCode: StatusCodes.Status500InternalServerError);
    }
}
