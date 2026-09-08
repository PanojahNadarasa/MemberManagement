using MemberManagement.DTOs;
using MemberManagement.Services;
using Microsoft.AspNetCore.Mvc;

namespace MemberManagement.Controllers
{
    [ApiController]
    [Route("api/members")]
    public class MemberController : ControllerBase
    {
        private readonly IMemberService _memberService;

    public MemberController(IMemberService memberService)
        {
            _memberService = memberService;
        }

        // POST: api/members
        [HttpPost]
        public async Task<IActionResult> CreateMember([FromBody] CreateMemberRequest request)
        {
            var result = await _memberService.CreateMemberAsync(request);

            if (!result.Success)
            {
                return BadRequest(new
                {
                    message = result.ErrorMessage
                });
            }

            return CreatedAtAction(
                nameof(GetMember),
                new { id = result.Member!.MemberId },
                result.Member);
        }

        // GET: api/members
        [HttpGet]
        public async Task<IActionResult> GetMembers()
        {
            var members =
                await _memberService.GetMembersAsync();

            return Ok(members);
        }

        // GET: api/members/{id}
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetMember(Guid id)
        {
            var member =
                await _memberService.GetMemberAsync(id);

            if (member == null)
            {
                return NotFound(new
                {
                    message = "Member not found."
                });
            }

            return Ok(member);
        }

        // PUT: api/members/{id}
        [HttpPut("{id:guid}")]
        public async Task<IActionResult> UpdateMember(Guid id,[FromBody] UpdateMemberRequest request)
        {
            var result = await _memberService.UpdateMemberAsync(id,request);

            if (!result.Success)
            {
                if (result.ErrorMessage == "Member not found.")
                {
                    return NotFound(new
                    {
                        message = result.ErrorMessage
                    });
                }

                return BadRequest(new
                {
                    message = result.ErrorMessage
                });
            }

            return Ok(result.Member);
        }
        // DELETE: api/members/{id}
        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> DeleteMember(Guid id)
        {
            var result = await _memberService.DeleteMemberAsync(id);

            if (!result.Success)
            {
                if (result.ErrorMessage == "Member not found.")
                    return NotFound(new { message = result.ErrorMessage });

                return BadRequest(new { message = result.ErrorMessage });
            }

            return NoContent();
        }

        // PATCH: api/members/{id}/status
        [HttpPatch("{id:guid}/status")]
        public async Task<IActionResult> UpdateStatus(Guid id,[FromBody] bool isActive)
        {
            var result = await _memberService.UpdateStatusAsync(id,isActive);

            if (!result.Success)
            {
                return NotFound(new
                {
                    message = result.ErrorMessage
                });
            }

            return Ok(result.Member);
        }
    }

}
