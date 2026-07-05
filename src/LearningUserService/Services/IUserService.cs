using FluentResults;
using learning_user_service.Models.DTOs;

namespace learning_user_service.Services;

public interface IUserService
{
    Task<Result<IReadOnlyList<UserDto>>> GetUsersAsync(CancellationToken ct = default);
    Task<Result<UserDto>> GetUserByIdAsync(Guid id, CancellationToken ct = default);
    Task<Result<UserDto>> CreateUserAsync(CreateUserRequest request, CancellationToken ct = default);
    Task<Result<UserDto>> LoginAsync(LoginRequest request, CancellationToken ct = default);
    Task<Result<bool>> DeleteUserAsync(Guid id, CancellationToken ct = default);
}