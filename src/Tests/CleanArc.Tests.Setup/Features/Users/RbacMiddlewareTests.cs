using System.Security.Claims;
using CleanArc.Application.Models.ApiResult;
using CleanArc.WebFramework.Middlewares;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace CleanArc.Tests.Setup.Features.Users;

public class RbacMiddlewareTests
{
    private readonly ILogger<RbacMiddleware> _loggerMock = Substitute.For<ILogger<RbacMiddleware>>();

    private HttpContext CreateHttpContext(string method, string path, string routeTemplate, ClaimsPrincipal user, bool hasAuthorizeMetadata = true)
    {
        var context = new DefaultHttpContext();
        context.Request.Method = method;
        context.Request.Path = path;
        context.User = user;

        var metadata = new List<object>();
        if (hasAuthorizeMetadata)
        {
            metadata.Add(Substitute.For<IAuthorizeData>());
        }

        var endpoint = new RouteEndpoint(
            c => Task.CompletedTask,
            RoutePatternFactory.Parse(routeTemplate),
            0,
            new EndpointMetadataCollection(metadata),
            "TestEndpoint"
        );

        context.SetEndpoint(endpoint);
        return context;
    }

    [Fact]
    public async Task InvokeAsync_AnonymousOrNoAuthMetadata_AllowsRequest()
    {
        var nextCalled = false;
        RequestDelegate next = ctx =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        };

        var middleware = new RbacMiddleware(next, _loggerMock);
        var user = new ClaimsPrincipal(new ClaimsIdentity());
        var context = CreateHttpContext("GET", "/api/v1.1/Badges", "api/v{version}/Badges", user, hasAuthorizeMetadata: false);

        await middleware.InvokeAsync(context);

        Assert.True(nextCalled);
        Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_StudentEndpoint_StudentUser_AllowsRequest()
    {
        var nextCalled = false;
        RequestDelegate next = ctx =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        };

        var middleware = new RbacMiddleware(next, _loggerMock);
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.Role, "student")
        }, "TestAuth"));
        var context = CreateHttpContext("GET", "/api/v1.1/Badges", "api/v{version}/Badges", user, hasAuthorizeMetadata: true);

        await middleware.InvokeAsync(context);

        Assert.True(nextCalled);
        Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_StudentEndpoint_TeacherUser_DeniesRequest()
    {
        var nextCalled = false;
        RequestDelegate next = ctx =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        };

        var middleware = new RbacMiddleware(next, _loggerMock);
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.Role, "teacher")
        }, "TestAuth"));
        var context = CreateHttpContext("GET", "/api/v1.1/Badges", "api/v{version}/Badges", user, hasAuthorizeMetadata: true);

        await middleware.InvokeAsync(context);

        Assert.False(nextCalled);
        Assert.Equal(StatusCodes.Status403Forbidden, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_EducatorEndpoint_StudentUser_DeniesRequest()
    {
        var nextCalled = false;
        RequestDelegate next = ctx =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        };

        var middleware = new RbacMiddleware(next, _loggerMock);
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.Role, "student")
        }, "TestAuth"));
        var context = CreateHttpContext("GET", "/api/v1/educator/Classrooms/dashboard", "api/v{version}/educator/Classrooms/dashboard", user, hasAuthorizeMetadata: true);

        await middleware.InvokeAsync(context);

        Assert.False(nextCalled);
        Assert.Equal(StatusCodes.Status403Forbidden, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_EducatorEndpoint_TeacherUser_AllowsRequest()
    {
        var nextCalled = false;
        RequestDelegate next = ctx =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        };

        var middleware = new RbacMiddleware(next, _loggerMock);
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.Role, "teacher")
        }, "TestAuth"));
        var context = CreateHttpContext("GET", "/api/v1/educator/Classrooms/dashboard", "api/v{version}/educator/Classrooms/dashboard", user, hasAuthorizeMetadata: true);

        await middleware.InvokeAsync(context);

        Assert.True(nextCalled);
        Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
    }
}
