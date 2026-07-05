namespace learning_user_service.Models.DTOs;

public record UserDto(Guid Id, string Username, string Email, string? Name, IReadOnlyList<string> Roles);
public record CreateUserRequest(string Username, string Email, string Password, string? Name = null);
public record LoginRequest(string Username, string Password);
public record UpdateUserRequest(string? Name, string? Email);
public record RoleDto(Guid Id, string Name);
public record AssignRoleRequest(Guid UserId, Guid RoleId);
