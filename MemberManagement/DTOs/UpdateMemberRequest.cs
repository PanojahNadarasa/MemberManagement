using MemberManagement.Enums;

namespace MemberManagement.DTOs
{
    public class UpdateMemberRequest
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public DateTime DateOfBirth { get; set; }
        public MemberType MemberType { get; set; }
        public bool IsActive { get; set; }
    }
}
