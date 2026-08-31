using System.Net;
using System.Net.Http.Json;
using MemberManagement.Data;
using MemberManagement.Entity;
using MemberManagement.Enums;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace SampleAPI.Tests
{
    public class MemberControllerTest : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;

    public MemberControllerTest(WebApplicationFactory<Program> factory)
        {
            _factory = factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    // Remove existing database registration
                    var descriptor = services.SingleOrDefault(
                        d => d.ServiceType ==
                             typeof(DbContextOptions<ApplicationDbContext>));

                    if (descriptor != null)
                    {
                        services.Remove(descriptor);
                    }

                    // Use InMemory database for integration testing
                    services.AddDbContext<ApplicationDbContext>(options =>
                    {
                        options.UseInMemoryDatabase(
                            "MemberTestDb_" + Guid.NewGuid());
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

            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();

            context.members.RemoveRange(context.members);

            await context.SaveChangesAsync();
        }

        // 1. CREATE MEMBER

        [Fact]
        public async Task CreateMember_ShouldReturnCreated()
        {
            await ClearDatabaseAsync();

            var client = CreateClient();

            var member = new MemberEntity
            {
                MemberId = Guid.NewGuid(),
                RegistrationNumber = "(976111788V",
                FirstName = "John",
                LastName = "Smith",
                Email = "john.smith@test.com",
                DateOfBirth = new DateTime(1990, 5, 10),
                MemberType = MemberType.Major,
                IsActive = true
            };

            var response = await client.PostAsJsonAsync(
                "/api/Member",
                member);

            Assert.Equal(
                HttpStatusCode.Created,
                response.StatusCode);
        }


        // 2. GET ALL MEMBERS

        [Fact]
        public async Task GetAllMembers_ShouldReturnOk()
        {
            await ClearDatabaseAsync();

            var client = CreateClient();

            var response = await client.GetAsync(
                "/api/Member");

            Assert.Equal(
                HttpStatusCode.OK,
                response.StatusCode);
        }

      
    }

}
