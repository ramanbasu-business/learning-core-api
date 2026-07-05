using learning_user_service.Models.Entities;

namespace learning_user_service.Repositories;

public interface IRoleRepository
{
    Task<IReadOnlyList<Role>> GetAllAsync(CancellationToken ct = default);
    Task<Role?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Role?> GetByNameAsync(string name, CancellationToken ct = default);
    Task<Role> CreateAsync(Role role, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}
