using learning_core_api.Data;
using learning_core_api.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace learning_core_api.Repositories;

public class RoleRepository(AppDbContext context, ILogger<RoleRepository> logger) : IRoleRepository
{
    public async Task<IReadOnlyList<Role>> GetAllAsync(CancellationToken ct = default)
    {
        if (logger.IsEnabled(LogLevel.Debug))
            logger.LogDebug("Fetching all roles");
        return await context.Roles.ToListAsync(ct);
    }

    public async Task<Role?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        if (logger.IsEnabled(LogLevel.Debug))
            logger.LogDebug("Fetching role {RoleId}", id);
        return await context.Roles.FirstOrDefaultAsync(r => r.Id == id, ct);
    }

    public async Task<Role?> GetByNameAsync(string name, CancellationToken ct = default)
    {
        if (logger.IsEnabled(LogLevel.Debug))
            logger.LogDebug("Fetching role by name {RoleName}", name);
        return await context.Roles.FirstOrDefaultAsync(r => r.Name == name, ct);
    }

    public async Task<Role> CreateAsync(Role role, CancellationToken ct = default)
    {
        if (logger.IsEnabled(LogLevel.Debug))
            logger.LogDebug("Creating role {RoleName}", role.Name);
        context.Roles.Add(role);
        await context.SaveChangesAsync(ct);
        return role;
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        if (logger.IsEnabled(LogLevel.Debug))
            logger.LogDebug("Deleting role {RoleId}", id);
        var role = await context.Roles.FirstOrDefaultAsync(r => r.Id == id, ct);
        if (role != null)
        {
            context.Roles.Remove(role);
            await context.SaveChangesAsync(ct);
        }
    }
}
