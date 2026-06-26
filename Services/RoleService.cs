using FluentResults;
using learning_core_api.Data;
using learning_core_api.Models.DTOs;
using learning_core_api.Models.Entities;
using learning_core_api.Repositories;
using Microsoft.EntityFrameworkCore;

namespace learning_core_api.Services;

public class RoleService(
    IRoleRepository roleRepository,
    IUserRepository userRepository,
    AppDbContext context,
    ILogger<RoleService> logger) : IRoleService
{
    public const string NotFoundMetadataKey = "NotFound";

    public async Task<Result<IReadOnlyList<RoleDto>>> GetAllAsync(CancellationToken ct = default)
    {
        try
        {
            if (logger.IsEnabled(LogLevel.Debug))
                logger.LogDebug("Fetching all roles");
            var roles = await roleRepository.GetAllAsync(ct);
            var roleDtos = roles.Select(MapToDto).ToList();
            return Result.Ok((IReadOnlyList<RoleDto>)roleDtos);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to fetch roles");
            return Result.Fail<IReadOnlyList<RoleDto>>(new Error("Failed to fetch roles").CausedBy(ex));
        }
    }

    public async Task<Result<RoleDto>> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        try
        {
            if (logger.IsEnabled(LogLevel.Debug))
                logger.LogDebug("Fetching role {RoleId}", id);
            var role = await roleRepository.GetByIdAsync(id, ct);
            if (role is null)
            {
                return Result.Fail<RoleDto>(new Error($"Role {id} not found").WithMetadata(NotFoundMetadataKey, true));
            }

            return Result.Ok(MapToDto(role));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to fetch role {RoleId}", id);
            return Result.Fail<RoleDto>(new Error("Failed to fetch role").CausedBy(ex));
        }
    }

    public async Task<Result<RoleDto>> CreateAsync(string name, CancellationToken ct = default)
    {
        try
        {
            if (logger.IsEnabled(LogLevel.Debug))
                logger.LogDebug("Creating role {RoleName}", name);

            var role = new Role { Id = Guid.NewGuid(), Name = name };
            var createdRole = await roleRepository.CreateAsync(role, ct);
            return Result.Ok(MapToDto(createdRole));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to create role {RoleName}", name);
            return Result.Fail<RoleDto>(new Error("Failed to create role").CausedBy(ex));
        }
    }

    public async Task<Result<bool>> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        try
        {
            if (logger.IsEnabled(LogLevel.Debug))
                logger.LogDebug("Deleting role {RoleId}", id);
            await roleRepository.DeleteAsync(id, ct);
            return Result.Ok(true);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to delete role {RoleId}", id);
            return Result.Fail<bool>(new Error("Failed to delete role").CausedBy(ex));
        }
    }

    public async Task<Result<bool>> AssignRoleToUserAsync(AssignRoleRequest request, CancellationToken ct = default)
    {
        try
        {
            if (logger.IsEnabled(LogLevel.Debug))
                logger.LogDebug("Assigning role {RoleId} to user {UserId}", request.RoleId, request.UserId);

            var user = await userRepository.GetByIdAsync(request.UserId, ct);
            if (user is null)
            {
                return Result.Fail<bool>(new Error($"User {request.UserId} not found").WithMetadata(NotFoundMetadataKey, true));
            }

            var role = await roleRepository.GetByIdAsync(request.RoleId, ct);
            if (role is null)
            {
                return Result.Fail<bool>(new Error($"Role {request.RoleId} not found").WithMetadata(NotFoundMetadataKey, true));
            }

            var userRole = new UserRole { Id = Guid.NewGuid(), UserId = request.UserId, RoleId = request.RoleId };
            context.UserRoles.Add(userRole);
            await context.SaveChangesAsync(ct);

            return Result.Ok(true);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to assign role {RoleId} to user {UserId}", request.RoleId, request.UserId);
            return Result.Fail<bool>(new Error("Failed to assign role").CausedBy(ex));
        }
    }

    private static RoleDto MapToDto(Role role) => new(role.Id, role.Name);
}
