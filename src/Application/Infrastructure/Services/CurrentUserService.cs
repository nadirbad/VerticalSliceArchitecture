using System.Security.Claims;

using Microsoft.AspNetCore.Http;

using VerticalSliceArchitecture.Application.Common.Interfaces;

namespace VerticalSliceArchitecture.Application.Infrastructure.Services;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string UserId => _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier)!;
}