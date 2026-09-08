using MemberManagement.DTOs;
using MemberManagement.Entity;

namespace MemberManagement.Services
{
    public interface IMemberService
    {
        Task<(bool Success, string? ErrorMessage, MemberEntity? Member)> CreateMemberAsync(CreateMemberRequest request);
        Task<IEnumerable<MemberEntity>> GetMembersAsync();
        Task<MemberEntity?> GetMemberAsync(Guid id);
        Task<(bool Success, string? ErrorMessage, MemberEntity? Member)> UpdateMemberAsync(Guid id,UpdateMemberRequest request);
        Task<(bool Success, string? ErrorMessage)> DeleteMemberAsync(Guid id);
        Task<(bool Success, string? ErrorMessage, MemberEntity? Member)> UpdateStatusAsync(Guid id,bool isActive);
    }
}