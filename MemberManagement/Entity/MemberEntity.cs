using MemberManagement.Enums;
using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace MemberManagement.Entity
{
    public class MemberEntity
    {
        [Key]
        public Guid MemberId { get; set; }

        [Required]
        [MaxLength(50)]
        public string RegistrationNumber { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string LastName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [MaxLength(200)]
        public string Email { get; set; } = string.Empty;

        [Required]
        public DateTime DateOfBirth { get; set; }

        [Required]
        public MemberType MemberType { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
