using CapTap.Application.DependencyInjection;
using CapTap.Shared.Responses;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace CapTap.Application.Tests;

public class ApplicationFoundationTests
{
    [Fact]
    public void AddApplicationServices_Should_Register_Without_Throwing()
    {
        var services = new ServiceCollection();

        var act = () => services.AddApplicationServices();

        act.Should().NotThrow();
        services.Should().NotBeNull();
    }

    [Fact]
    public void ApiResponse_Ok_Should_Set_Success_True()
    {
        var response = ApiResponse<object>.Ok(new { ready = true }, "loaded");

        response.Success.Should().BeTrue();
        response.Data.Should().NotBeNull();
        response.Message.Should().Be("loaded");
        response.Error.Should().BeNull();
    }

    [Fact]
    public void ApiResponse_Fail_Should_Set_Error_Payload()
    {
        var response = ApiResponse.Fail("SERVER_ERROR", "An unexpected error occurred.");

        response.Success.Should().BeFalse();
        response.Error.Should().NotBeNull();
        response.Error!.Code.Should().Be("SERVER_ERROR");
        response.Error.Message.Should().Be("An unexpected error occurred.");
    }
}
