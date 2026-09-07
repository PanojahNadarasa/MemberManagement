using System.Net;
using System.Net.Http.Json;
using MemberManagement.Data;
using MemberManagement.Entity;
using MemberManagement.Enums;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace MemberManagement.Tests
{
    public class MemberControllerTest : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;

        public MemberControllerTest(WebApplicationFactory<Program> factory)
        {
            var dbName = "MemberTestDb_" + Guid.NewGuid(); 

            _factory = factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    var descriptor = services.SingleOrDefault(
                        d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));

                    if (descriptor != null)
                    {
                        services.Remove(descriptor);
                    }

                    services.AddDbContext<ApplicationDbContext>(options =>
                    {
                        options.UseInMemoryDatabase(dbName); 
                    });
                });
            });
        }
        private HttpClient CreateClient()
        {
            return _factory.CreateClient();
        }

        private async Task ClearDatabaseAsync()
        {
            using var scope = _factory.Services.CreateScope();

            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            context.members.RemoveRange(context.members);

            await context.SaveChangesAsync();
        }

        // 1. CREATE MEMBER
        [Fact]
        public async Task CreateMember_ShouldReturnCreated()
        {
            // Arrange
            await ClearDatabaseAsync();

            var client = CreateClient();

            var member = new MemberEntity
            {
                RegistrationNumber = "976111788V",
                FirstName = "John",
                LastName = "Smith",
                Email = "johnsmith@test.com",
                DateOfBirth = new DateTime(1990, 5, 10),
                MemberType = MemberType.Major,
                IsActive = true
            };

            var response = await client.PostAsJsonAsync(
                "/api/members",
                member);

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        }

        // 2. GET ALL MEMBERS
        [Fact]
        public async Task GetAllMembers_ShouldReturnOk()
        {
            await ClearDatabaseAsync();

            var client = CreateClient();
            var response = await client.GetAsync("/api/members");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        // 3. GET MEMBER BY ID
        [Fact]
        public async Task GetMemberById_WhenMemberExists_ShouldReturnOk()
        {
            // Arrange
            await ClearDatabaseAsync();

            var id = Guid.NewGuid();

            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                var member = new MemberEntity
                {
                    MemberId = id,   // <-- this line was missing
                    RegistrationNumber = "976111789V",
                    FirstName = "John",
                    LastName = "Smith",
                    Email = "johnsmith@test.com",
                    DateOfBirth = new DateTime(1990, 5, 10),
                    MemberType = MemberType.Major,
                    IsActive = true
                };

                context.members.Add(member);

                await context.SaveChangesAsync();
               // var savedCount = context.members.Count(x => x.MemberId == id);

            }

            var client = CreateClient();

            var response = await client.GetAsync($"/api/members/{id}");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        // 4. GET MEMBER BY INVALID ID
        [Fact]
        public async Task GetMemberById_WhenMemberDoesNotExist_ShouldReturnNotFound()
        {
            await ClearDatabaseAsync();

            var client = CreateClient();

            var memberId = Guid.NewGuid();

            var response = await client.GetAsync($"/api/members/{memberId}");

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        // 5. UPDATE MEMBER
        [Fact]
        public async Task UpdateMember_WhenMemberExists_ShouldReturnOk()
        {
            // Arrange
            await ClearDatabaseAsync();

            var memberId = Guid.NewGuid();

            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                context.members.Add(
                    new MemberEntity
                    {
                        MemberId = memberId,
                        RegistrationNumber = "78945567V",
                        FirstName = "John",
                        LastName = "Smith",
                        Email = "john@test.com",
                        DateOfBirth = new DateTime(1990, 1, 1),
                        MemberType = MemberType.Major,
                        IsActive = true
                    });

                await context.SaveChangesAsync();
            }

            var client = CreateClient();

            var updatedMember = new MemberEntity
            {
                MemberId = memberId,
                RegistrationNumber = "REG003",
                FirstName = "John Updated",
                LastName = "Smith Updated",
                Email = "johnupdated@test.com",
                DateOfBirth = new DateTime(1990, 1, 1),
                MemberType = MemberType.Major,
                IsActive = true
            };

            // Act
            var response = await client.PutAsJsonAsync($"/api/members/{memberId}", updatedMember);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        // 6. ACTIVATE MEMBER
        [Fact]
        public async Task ActivateMember_ShouldChangeIsActiveToTrue()
        {
            // Arrange
            await ClearDatabaseAsync();

            var memberId = Guid.NewGuid();

            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                context.members.Add(
                    new MemberEntity
                    {
                        MemberId = memberId,
                        RegistrationNumber = "REG004",
                        FirstName = "David",
                        LastName = "Wilson",
                        Email = "david@test.com",
                        DateOfBirth = new DateTime(1992, 7, 20),
                        MemberType = MemberType.DependantAdult,
                        IsActive = false
                    });

                await context.SaveChangesAsync();
            }

            var client = CreateClient();

            // true
            var response = await client.PatchAsJsonAsync(
                $"/api/members/{memberId}/status",
                true);

            // Assert
            Assert.Equal(
                HttpStatusCode.OK, response.StatusCode);

            // Verify database status
            using var verificationScope = _factory.Services.CreateScope();

            var verificationContext = verificationScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var member = await verificationContext
                .members.FirstOrDefaultAsync(x => x.MemberId == memberId);

            Assert.NotNull(member);

            Assert.True(member.IsActive);
        }


        // 7. DEACTIVATE MEMBER
        [Fact]
        public async Task DeactivateMember_ShouldChangeIsActiveToFalse()
        {
            // Arrange
            await ClearDatabaseAsync();

            var memberId = Guid.NewGuid();

            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                context.members.Add(
                    new MemberEntity
                    {
                        MemberId = memberId,
                        RegistrationNumber = "REG005",
                        FirstName = "Michael",
                        LastName = "Brown",
                        Email = "michael@test.com",
                        DateOfBirth = new DateTime(1988, 4, 12),
                        MemberType = MemberType.Major,
                        IsActive = true
                    });

                await context.SaveChangesAsync();
            }

            var client = CreateClient();

            // Controller expects raw boolean:
            // false
            var response = await client.PatchAsJsonAsync(
                $"/api/members/{memberId}/status",
                false);

            // Assert
            Assert.Equal(
                HttpStatusCode.OK,
                response.StatusCode);

            // Verify database status
            using var verificationScope = _factory.Services.CreateScope();

            var verificationContext = verificationScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var member = await verificationContext
                .members
                .FirstOrDefaultAsync(
                    x => x.MemberId == memberId);

            Assert.NotNull(member);

            Assert.False(member.IsActive);
        }


        // 8. DELETE MEMBER
        // 8. DELETE MEMBER
        [Fact]
        public async Task DeleteMember_WhenMemberExists_ShouldReturnNoContent()
        {
            // Arrange
            await ClearDatabaseAsync();

            var client = CreateClient();

            var member = new MemberEntity
            {
                RegistrationNumber = "REG006",
                FirstName = "Delete",
                LastName = "Test",
                Email = "delete@test.com",
                DateOfBirth = new DateTime(1990, 1, 1),
                MemberType = MemberType.Major,
                IsActive = true
            };

            // Create member through API
            var createResponse = await client.PostAsJsonAsync(
                "/api/members",
                member);

            Assert.Equal(
                HttpStatusCode.Created,
                createResponse.StatusCode);

            // Get the created member from the API response
            var createdMember =
                await createResponse.Content.ReadFromJsonAsync<MemberEntity>();

            Assert.NotNull(createdMember);

            var memberId = createdMember.MemberId;

            // Act - Delete the member
            var deleteResponse = await client.DeleteAsync(
                $"/api/members/{memberId}");

            // Assert
            Assert.Equal(
                HttpStatusCode.NoContent,
                deleteResponse.StatusCode);

            // Verify member was deleted
            using var verificationScope =
                _factory.Services.CreateScope();

            var verificationContext =
                verificationScope.ServiceProvider
                    .GetRequiredService<ApplicationDbContext>();

            var deletedMember =
                await verificationContext.members
                    .FirstOrDefaultAsync(x => x.MemberId == memberId);

            Assert.Null(deletedMember);
        }
    }
}