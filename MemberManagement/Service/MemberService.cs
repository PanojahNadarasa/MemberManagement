using MemberManagement.Data;
using MemberManagement.DTOs;
using MemberManagement.Entity;
using MemberManagement.Enums;
using Microsoft.EntityFrameworkCore;

namespace MemberManagement.Services
{
    public class MemberService : IMemberService
    {
        private readonly ApplicationDbContext _context;

        public MemberService(ApplicationDbContext context)
        {
            _context = context;
        }

        // CREATE
        public async Task<(bool Success, string? ErrorMessage, MemberEntity? Member)> CreateMemberAsync(CreateMemberRequest request)
        {
            try
            {
                // Check duplicate registration number
                var registrationExists = await _context.members
                    .AnyAsync(x =>x.RegistrationNumber == request.RegistrationNumber);

                if (registrationExists)
                {
                    return (false,"Registration number already exists.",null);
                }
                if (request.DateOfBirth.Date > DateTime.UtcNow.Date)
                {
                    return (false,"Date of birth cannot be in the future.",null);
                }
                var validationError = ValidateMemberType(request.DateOfBirth,request.MemberType);

                if (validationError != null)
                {
                    return (false,validationError,null);
                }

                var member = new MemberEntity
                {
                    MemberId = Guid.NewGuid(),
                    RegistrationNumber = request.RegistrationNumber,
                    FirstName = request.FirstName,
                    LastName = request.LastName,
                    Email = request.Email,
                    DateOfBirth = request.DateOfBirth,
                    MemberType = request.MemberType,
                    IsActive = request.IsActive
                };

                _context.members.Add(member);

                await _context.SaveChangesAsync();

                return (true,null, member);
            }
            catch (DbUpdateException)
            {
                return (
                    false,
                    "A database error occurred while creating the member.",
                    null
                );
            }
            catch (Exception)
            {
                return (
                    false,
                    "An unexpected error occurred while creating the member.",
                    null
                );
            }
        }

        // GET ALL
        public async Task<IEnumerable<MemberEntity>> GetMembersAsync()
        {
            try
            {
                var members = await _context.members.AsNoTracking().ToListAsync();
                return members;
            }
            catch (Exception)
            {
                throw new Exception(
                    "An unexpected error occurred while retrieving members.");
            }
        }

        // GET BY ID
        public async Task<MemberEntity?> GetMemberAsync(Guid id)
        {
            try
            {
                var member = await _context.members.AsNoTracking().FirstOrDefaultAsync(x => x.MemberId == id);
                return member;
            }
            catch (Exception)
            {
                throw new Exception(
                    "An unexpected error occurred while retrieving the member.");
            }
        }


        // UPDATE
        public async Task<(bool Success, string? ErrorMessage, MemberEntity? Member)> UpdateMemberAsync(
                Guid id,UpdateMemberRequest request)
        {
            try
            {
                var member = await _context.members.FirstOrDefaultAsync(x => x.MemberId == id);

                if (member == null)
                {
                    return (false,"Member not found.",null);
                }

                if (request.DateOfBirth.Date > DateTime.UtcNow.Date)
                {
                    return (
                        false,"Date of birth cannot be in the future.",null
                    );
                }

                // Member type validation
                var validationError = ValidateMemberType(request.DateOfBirth,request.MemberType);

                if (validationError != null)
                {
                    return (false,validationError,null );
                }
                member.FirstName =request.FirstName;
                member.LastName =request.LastName;
                member.Email = request.Email;
                member.DateOfBirth =request.DateOfBirth;
                member.MemberType =request.MemberType;
                member.IsActive =request.IsActive;

                await _context.SaveChangesAsync();

                return (
                    true,null,member
                );
            }
            catch (DbUpdateException)
            {
                return (
                    false,
                    "A database error occurred while updating the member.",
                    null
                );
            }
            catch (Exception)
            {
                return (
                    false,
                    "An unexpected error occurred while updating the member.",
                    null
                );
            }
        }

        // DELETE
        public async Task<(bool Success, string? ErrorMessage)> DeleteMemberAsync(Guid id)
        {
            var member = await _context.members
                .FirstOrDefaultAsync(x => x.MemberId == id);

            if (member == null)
            {
                return (false, "Member not found.");
            }

            // Only active members can be deleted
            if (!member.IsActive)
            {
                return (false, "Only active members can be deleted.");
            }

            _context.members.Remove(member);
            await _context.SaveChangesAsync();

            return (true, null);
        }        // UPDATE STATUS
        public async Task<(bool Success, string? ErrorMessage, MemberEntity? Member)>UpdateStatusAsync(Guid id,bool isActive)
        {
            try
            {
                var member = await _context.members.FirstOrDefaultAsync(x => x.MemberId == id);

                if (member == null)
                {
                    return (
                        false, "Member not found.", null
                    );
                }

                member.IsActive = isActive;
                await _context.SaveChangesAsync();

                return (true,null,member);
            }
            catch (DbUpdateException)
            {
                return (
                    false,"A database error occurred while updating member status.",null
                );
            }
            catch (Exception)
            {
                return (
                    false,"An unexpected error occurred while updating member status.",null
                );
            }
        }


        private static string? ValidateMemberType(DateTime dateOfBirth,MemberType memberType)
        {
            var age = CalculateAge(dateOfBirth);

            switch (memberType)
            {
                case MemberType.Minor
                    when age >= 18:
                    return
                        "Minor member must be under 18 years old.";

                case MemberType.Major
                    when age < 18:
                    return
                        "Major member must be 18 years or older.";

                case MemberType.DependantAdult
                    when age < 18:
                    return
                        "Dependant Adult must be 18 years or older.";
            }
            return null;
        }

        private static int CalculateAge(DateTime dateOfBirth)
        {
            var today = DateTime.UtcNow.Date;
            var age = today.Year - dateOfBirth.Year;

            if (dateOfBirth.Date > today.AddYears(-age))
            {
                age--;
            }
            return age;
        }
    }
}