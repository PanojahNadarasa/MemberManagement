using MemberManagement.Data;
using MemberManagement.Entity;
using MemberManagement.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MemberManagement.Controllers
{
    [ApiController]
    [Route("api/members")]
    public class MemberController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public MemberController(ApplicationDbContext context)
        {
            _context = context;
        }

        // POST: api/members
        [HttpPost]
        public async Task<ActionResult<MemberEntity>> CreateMember(MemberEntity member)
        {
            // duplicate check 
            var registrationExists = await _context.members
                .AnyAsync(x =>
                    x.RegistrationNumber == member.RegistrationNumber);

            if (registrationExists)
            {
                return BadRequest(new
                {
                    message = "Registration number already exists."
                });
            }

            // Date of birth cannot be future
            if (member.DateOfBirth.Date > DateTime.UtcNow.Date)
            {
                return BadRequest(new
                {
                    message = "Date of birth cannot be in the future."
                });
            }

          //MemberType validation checking
          var validationError = ValidateMemberType(
           member.DateOfBirth,
           member.MemberType);

            if (validationError != null)
            {
                return BadRequest(new
                {
                    message = validationError
                });
            }

            member.MemberId = Guid.NewGuid();

            _context.members.Add(member);

            await _context.SaveChangesAsync();

            return CreatedAtAction(
                nameof(GetMember),
                new { id = member.MemberId },
                member);
        }

        // GET: api/members
        [HttpGet]
        public async Task<ActionResult<IEnumerable<MemberEntity>>> GetMembers()
        {
            var members = await _context.members
                .AsNoTracking()
                .ToListAsync();

            return Ok(members);
        }

        // GET: api/members/{id}
        [HttpGet("{id:guid}")]
        public async Task<ActionResult<MemberEntity>> GetMember(Guid id)
        {
            var member = await _context.members
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.MemberId == id);

            if (member == null)
            {
                return NotFound(new
                {
                    message = "Member not found."
                });
            }

            return Ok(member);
        }

        //Validation checking
        private static string? ValidateMemberType(DateTime dateOfBirth, MemberType memberType)
        {
            var age = CalculateAge(dateOfBirth);

            switch (memberType)
            {
                case MemberType.Minor when age >= 18:
                    return "Minor member must be under 18 years old.";

                case MemberType.Major when age < 18:
                    return "Major member must be 18 years or older.";

                case MemberType.DependantAdult when age < 18:
                    return "Dependant Adult must be 18 years or older.";
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
