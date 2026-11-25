using AutoTaller.Business.Models;

namespace AutoTaller.Business.Abstractions;

public interface IUserAdminService
{
    Task<IReadOnlyList<UserListItem>> ListUsersAsync();
    Task<OperationResult> SetRoleAsync(string actingUserId, string targetUserId, string role);
    Task<OperationResult> SetActiveAsync(string actingUserId, string targetUserId, bool isActive);
}
