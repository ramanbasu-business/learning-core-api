using FluentResults;
using learning_core_api.Models.DTOs;

namespace learning_core_api.Services;

public interface IRoleService
{
    Task<Result<IReadOnlyList<RoleDto>>> GetAllAsync(CancellationToken ct = default);
    Task<Result<RoleDto>> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Result<RoleDto>> CreateAsync(string name, CancellationToken ct = default);
    Task<Result<bool>> DeleteAsync(Guid id, CancellationToken ct = default);
    Task<Result<bool>> AssignRoleToUserAsync(AssignRoleRequest request, CancellationToken ct = default);
}
