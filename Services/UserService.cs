using System.Security.Cryptography;
using System.Text;
using FluentResults;
using learning_core_api.Models.DTOs;
using learning_core_api.Models.Entities;
using learning_core_api.Repositories;

namespace learning_core_api.Services;

public class UserService(IUserRepository userRepository, ILogger<UserService> logger) : IUserService
{
    public const string NotFoundMetadataKey = "NotFound";

    public async Task<Result<IReadOnlyList<UserDto>>> GetUsersAsync(CancellationToken ct = default)
    {
        try
        {
            if (logger.IsEnabled(LogLevel.Debug))
                logger.LogDebug("Fetching all users");

            var users = await userRepository.GetAllAsync(ct);
            var userDtos = users.Select(MapToDto).ToList();
         
            return Result.Ok((IReadOnlyList<UserDto>)userDtos);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to fetch users");
            return Result.Fail<IReadOnlyList<UserDto>>(new Error("Failed to fetch users").CausedBy(ex));
        }
    }

    public async Task<Result<UserDto>> GetUserByIdAsync(Guid id, CancellationToken ct = default)
    {
        try
        {
            if (logger.IsEnabled(LogLevel.Debug))
                logger.LogDebug("Fetching user {UserId}", id);
                
            var user = await userRepository.GetByIdAsync(id, ct);
            if (user is null)
            {
                return Result.Fail<UserDto>(new Error($"User {id} not found").WithMetadata(NotFoundMetadataKey, true));
            }

            return Result.Ok(MapToDto(user));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to fetch user {UserId}", id);
            return Result.Fail<UserDto>(new Error("Failed to fetch user").CausedBy(ex));
        }
    }

    public async Task<Result<UserDto>> CreateUserAsync(CreateUserRequest request, CancellationToken ct = default)
    {
        try
        {
            if (logger.IsEnabled(LogLevel.Debug))
                logger.LogDebug("Creating user {Username}", request.Username);

            var user = new User
            {
                Id = Guid.NewGuid(),
                Username = request.Username,
                Email = request.Email,
                PasswordHash = HashPassword(request.Password),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var createdUser = await userRepository.CreateAsync(user, ct);
            return Result.Ok(MapToDto(createdUser));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to create user {Username}", request.Username);
            return Result.Fail<UserDto>(new Error("Failed to create user").CausedBy(ex));
        }
    }

    public async Task<Result<UserDto>> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        try
        {
            if (logger.IsEnabled(LogLevel.Debug))
                logger.LogDebug("Attempting login for user {Username}", request.Username);

            var user = await userRepository.GetByUsernameAsync(request.Username, ct);
            if (user is null)
            {
                return Result.Fail<UserDto>(new Error("Invalid username or password").WithMetadata(NotFoundMetadataKey, true));
            }

            var passwordHash = HashPassword(request.Password);
            if (user.PasswordHash != passwordHash)
            {
                return Result.Fail<UserDto>(new Error("Invalid username or password").WithMetadata(NotFoundMetadataKey, true));
            }

            return Result.Ok(MapToDto(user));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to authenticate user {Username}", request.Username);
            return Result.Fail<UserDto>(new Error("Authentication failed").CausedBy(ex));
        }
    }

    public async Task<Result<bool>> DeleteUserAsync(Guid id, CancellationToken ct = default)
    {
        try
        {
            if (logger.IsEnabled(LogLevel.Debug))
                logger.LogDebug("Deleting user {UserId}", id);
            await userRepository.DeleteAsync(id, ct);
            return Result.Ok(true);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to delete user {UserId}", id);
            return Result.Fail<bool>(new Error("Failed to delete user").CausedBy(ex));
        }
    }

    private static UserDto MapToDto(User user) =>
        new(
            user.Id,
            user.Username,
            user.Email,
            user.Name,
        user.UserRoles.Select(ur => ur.Role.Name).ToList());

    private static string HashPassword(string password)
    {
        var hashedBytes = SHA256.HashData(Encoding.UTF8.GetBytes(password));
        return Convert.ToBase64String(hashedBytes);
    }
}