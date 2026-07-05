using learning_user_service.Models.DTOs;
using learning_user_service.Services;
using Microsoft.AspNetCore.Mvc;

namespace learning_user_service.Controllers;

[ApiController]
[Route("api/users")]
public class UsersController(IUserService userService, ILogger<UsersController> logger) : ControllerBase
{


    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<UserDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetUsers(CancellationToken ct)
    {
        if (logger.IsEnabled(LogLevel.Information))
            logger.LogInformation("Received request to fetch all users");

        var result = await userService.GetUsersAsync(ct);

        return result.IsSuccess
            ? Ok(result.Value)
            : Problem(
                detail: string.Join("; ", result.Errors.Select(e => e.Message)),
            statusCode: StatusCodes.Status500InternalServerError);
    }



    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetUserById(Guid id, CancellationToken ct)
    {
        if (logger.IsEnabled(LogLevel.Information))
            logger.LogInformation("Received request to fetch user {UserId}", id);

        var result = await userService.GetUserByIdAsync(id, ct);

        if (result.IsSuccess)
        {
            return Ok(result.Value);
        }

        if (result.Errors.Any(e => e.Metadata.ContainsKey(UserService.NotFoundMetadataKey)))
        {
            return NotFound();
        }

        return Problem(detail: string.Join("; ", result.Errors.Select(e => e.Message)), statusCode: StatusCodes.Status500InternalServerError);
    }

    [HttpPost("auth")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Login(LoginRequest request, CancellationToken ct)
    {
        if (logger.IsEnabled(LogLevel.Information))
            logger.LogInformation(
                "Received login request {@Request}",
                new
                {
                    request.Username,
                    Password = "[REDACTED]"
                });

        var result = await userService.LoginAsync(request, ct);

        if (result.IsSuccess)
        {
            return Ok(result.Value);
        }

        if (result.Errors.Any(e => e.Metadata.ContainsKey(UserService.NotFoundMetadataKey)))
        {
            return Unauthorized();
        }

        return Problem(
            detail: string.Join("; ", result.Errors.Select(e => e.Message)),
            statusCode: StatusCodes.Status500InternalServerError
        );
    }

    [HttpPost]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreateUser(CreateUserRequest request, CancellationToken ct)
    {
        if (logger.IsEnabled(LogLevel.Information))
            logger.LogInformation(
                "Received request to create user {@Request}",
                new
                {
                    request.Username,
                    request.Email,
                    request.Name,
                    Password = "[REDACTED]"
                });

        var result = await userService.CreateUserAsync(request, ct);

        if (result.IsSuccess)
        {
            if (logger.IsEnabled(LogLevel.Information))
                logger.LogInformation("Created user {UserId} for {Username}", result.Value.Id, request.Username);

            return CreatedAtAction(nameof(GetUserById), new { id = result.Value.Id }, result.Value);
        }

        return Problem(detail: string.Join("; ", result.Errors.Select(e => e.Message)), statusCode: StatusCodes.Status500InternalServerError);
    }

    [HttpGet("{id:guid}/roles")]
    [ProducesResponseType(typeof(IReadOnlyList<RoleDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetUserRoles(Guid id, CancellationToken ct)
    {
        if (logger.IsEnabled(LogLevel.Information))
            logger.LogInformation("Received request to fetch roles for user {UserId}", id);

        var result = await userService.GetUserByIdAsync(id, ct);

        if (!result.IsSuccess)
        {
            if (result.Errors.Any(e => e.Metadata.ContainsKey(UserService.NotFoundMetadataKey)))
            {
                return NotFound();
            }

            return Problem(detail: string.Join("; ", result.Errors.Select(e => e.Message)), statusCode: StatusCodes.Status500InternalServerError);
        }

        var roles = result.Value.Roles.Select(r => new RoleDto(Guid.Empty, r)).ToList();
        return Ok((IReadOnlyList<RoleDto>)roles);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DeleteUser(Guid id, CancellationToken ct)
    {
        if (logger.IsEnabled(LogLevel.Information))
            logger.LogInformation("Received request to delete user {UserId}", id);

        var result = await userService.DeleteUserAsync(id, ct);

        if (result.IsSuccess)
        {
            if (logger.IsEnabled(LogLevel.Information))
                logger.LogInformation("Deleted user {UserId}", id);

            return NoContent();
        }

        return Problem(detail: string.Join("; ", result.Errors.Select(e => e.Message)), statusCode: StatusCodes.Status500InternalServerError);
    }
}
