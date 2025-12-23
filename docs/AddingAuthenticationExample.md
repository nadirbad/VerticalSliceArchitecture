# Adding Authentication to Vertical Slice Architecture

This document provides practical examples of how to add authentication features to this Vertical Slice Architecture, following the established patterns.

## Example: JWT Authentication Implementation

This example shows how you could add JWT-based authentication while maintaining the vertical slice pattern.

### Step 1: Add Authentication Dependencies

Add to your `src/Api/Api.csproj`:

```xml
<PackageReference Include="Microsoft.AspNetCore.Authentication.JwtBearer" Version="9.0.0" />
<PackageReference Include="System.IdentityModel.Tokens.Jwt" Version="8.2.1" />
```

### Step 2: Create User Entity

**Location**: `src/Application/Domain/Users/User.cs`

```csharp
using VerticalSliceArchitecture.Application.Common;

namespace VerticalSliceArchitecture.Application.Domain.Users;

public class User : AuditableEntity
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public List<string> Roles { get; set; } = new();
}
```

### Step 3: Update Database Context

**Location**: `src/Application/Infrastructure/Persistence/ApplicationDbContext.cs`

Add to the existing class:

```csharp
using VerticalSliceArchitecture.Application.Domain.Users;

public class ApplicationDbContext : DbContext
{
    // ... existing code

    public DbSet<User> Users => Set<User>();

    // ... rest of existing code
}
```

### Step 4: Create Authentication Service Interface

**Location**: `src/Application/Common/Interfaces/IAuthenticationService.cs`

```csharp
using ErrorOr;

namespace VerticalSliceArchitecture.Application.Common.Interfaces;

public interface IAuthenticationService
{
    Task<ErrorOr<string>> AuthenticateAsync(string email, string password);
    string GenerateJwtToken(int userId, string email, List<string> roles);
    string HashPassword(string password);
    bool VerifyPassword(string password, string hash);
}
```

### Step 5: Implement Authentication Service

**Location**: `src/Application/Infrastructure/Services/AuthenticationService.cs`

```csharp
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using ErrorOr;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using VerticalSliceArchitecture.Application.Common.Interfaces;
using VerticalSliceArchitecture.Application.Infrastructure.Persistence;

namespace VerticalSliceArchitecture.Application.Infrastructure.Services;

public class AuthenticationService : IAuthenticationService
{
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _configuration;

    public AuthenticationService(ApplicationDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    public async Task<ErrorOr<string>> AuthenticateAsync(string email, string password)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == email && u.IsActive);

        if (user == null)
        {
            return Error.NotFound("User.NotFound", "Invalid email or password");
        }

        if (!VerifyPassword(password, user.PasswordHash))
        {
            return Error.Validation("User.InvalidPassword", "Invalid email or password");
        }

        var token = GenerateJwtToken(user.Id, user.Email, user.Roles);
        return token;
    }

    public string GenerateJwtToken(int userId, string email, List<string> roles)
    {
        var jwtSettings = _configuration.GetSection("JwtSettings");
        var secretKey = jwtSettings["SecretKey"];
        var issuer = jwtSettings["Issuer"];
        var audience = jwtSettings["Audience"];
        var expiryInHours = int.Parse(jwtSettings["ExpiryInHours"] ?? "24");

        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey!));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(ClaimTypes.Email, email),
        };

        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(expiryInHours),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string HashPassword(string password)
    {
        return BCrypt.Net.BCrypt.HashPassword(password);
    }

    public bool VerifyPassword(string password, string hash)
    {
        return BCrypt.Net.BCrypt.Verify(password, hash);
    }
}
```

### Step 6: Configure JWT Authentication

**Location**: `src/Api/Program.cs`

Add after `builder.Services.AddControllers();`:

```csharp
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

// JWT Authentication
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings["SecretKey"];

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey!))
    };
});
```

And add before `app.UseAuthorization();`:

```csharp
app.UseAuthentication();
```

### Step 7: Register Authentication Service

**Location**: `src/Application/ConfigureServices.cs`

Add to the `AddInfrastructure` method:

```csharp
services.AddTransient<IAuthenticationService, AuthenticationService>();
```

### Step 8: Create Login Feature (Vertical Slice)

**Location**: `src/Application/Features/Authentication/Login.cs`

```csharp
using ErrorOr;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using VerticalSliceArchitecture.Application.Common;
using VerticalSliceArchitecture.Application.Common.Interfaces;

namespace VerticalSliceArchitecture.Application.Features.Authentication;

public class LoginController : ApiControllerBase
{
    [HttpPost("/api/auth/login")]
    public async Task<IActionResult> Login(LoginCommand command)
    {
        var result = await Mediator.Send(command);

        return result.Match(
            response => Ok(response),
            Problem);
    }
}

public record LoginCommand(string Email, string Password) : IRequest<ErrorOr<LoginResponse>>;

public record LoginResponse(string Token, string Email);

internal sealed class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress();

        RuleFor(x => x.Password)
            .NotEmpty()
            .MinimumLength(6);
    }
}

internal sealed class LoginCommandHandler : IRequestHandler<LoginCommand, ErrorOr<LoginResponse>>
{
    private readonly IAuthenticationService _authenticationService;

    public LoginCommandHandler(IAuthenticationService authenticationService)
    {
        _authenticationService = authenticationService;
    }

    public async Task<ErrorOr<LoginResponse>> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var result = await _authenticationService.AuthenticateAsync(request.Email, request.Password);

        if (result.IsError)
        {
            return result.Errors;
        }

        return new LoginResponse(result.Value, request.Email);
    }
}
```

### Step 9: Create User Registration Feature

**Location**: `src/Application/Features/Authentication/Register.cs`

```csharp
using ErrorOr;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VerticalSliceArchitecture.Application.Common;
using VerticalSliceArchitecture.Application.Common.Interfaces;
using VerticalSliceArchitecture.Application.Domain.Users;
using VerticalSliceArchitecture.Application.Infrastructure.Persistence;

namespace VerticalSliceArchitecture.Application.Features.Authentication;

public class RegisterController : ApiControllerBase
{
    [HttpPost("/api/auth/register")]
    public async Task<IActionResult> Register(RegisterCommand command)
    {
        var result = await Mediator.Send(command);

        return result.Match(
            response => Ok(response),
            Problem);
    }
}

public record RegisterCommand(
    string Email,
    string Password,
    string FirstName,
    string LastName) : IRequest<ErrorOr<RegisterResponse>>;

public record RegisterResponse(int UserId, string Email, string Token);

internal sealed class RegisterCommandValidator : AbstractValidator<RegisterCommand>
{
    public RegisterCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress();

        RuleFor(x => x.Password)
            .NotEmpty()
            .MinimumLength(8)
            .Matches("[A-Z]").WithMessage("Password must contain at least one uppercase letter")
            .Matches("[a-z]").WithMessage("Password must contain at least one lowercase letter")
            .Matches("[0-9]").WithMessage("Password must contain at least one number");

        RuleFor(x => x.FirstName)
            .NotEmpty()
            .MaximumLength(50);

        RuleFor(x => x.LastName)
            .NotEmpty()
            .MaximumLength(50);
    }
}

internal sealed class RegisterCommandHandler : IRequestHandler<RegisterCommand, ErrorOr<RegisterResponse>>
{
    private readonly ApplicationDbContext _context;
    private readonly IAuthenticationService _authenticationService;

    public RegisterCommandHandler(
        ApplicationDbContext context,
        IAuthenticationService authenticationService)
    {
        _context = context;
        _authenticationService = authenticationService;
    }

    public async Task<ErrorOr<RegisterResponse>> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        // Check if user already exists
        var existingUser = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == request.Email, cancellationToken);

        if (existingUser != null)
        {
            return Error.Conflict("User.EmailTaken", "A user with this email already exists");
        }

        // Create new user
        var user = new User
        {
            Email = request.Email,
            PasswordHash = _authenticationService.HashPassword(request.Password),
            FirstName = request.FirstName,
            LastName = request.LastName,
            Roles = new List<string> { "User" } // Default role
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync(cancellationToken);

        // Generate token
        var token = _authenticationService.GenerateJwtToken(user.Id, user.Email, user.Roles);

        return new RegisterResponse(user.Id, user.Email, token);
    }
}
```

### Step 10: Configuration

**Location**: `src/Api/appsettings.json`

Add JWT settings:

```json
{
  "JwtSettings": {
    "SecretKey": "your-super-secret-key-that-is-at-least-32-characters-long",
    "Issuer": "VerticalSliceApp",
    "Audience": "VerticalSliceApp",
    "ExpiryInHours": 24
  }
}
```

### Step 11: Example of Protected Endpoint

**Location**: `src/Application/Features/TodoItems/CreateTodoItem.cs`

Modify the existing command to require authentication:

```csharp
using VerticalSliceArchitecture.Application.Common.Security;

[Authorize] // Add this line
public record CreateTodoItemCommand(int ListId, string? Title) : IRequest<ErrorOr<int>>;
```

### Step 12: Update Current User Service

The existing `CurrentUserService` will now work automatically with JWT authentication, as it extracts the user ID from the `NameIdentifier` claim that we set in the JWT token.

## Usage Examples

### Client Authentication Flow

```javascript
// Login
const loginResponse = await fetch('/api/auth/login', {
  method: 'POST',
  headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify({
    email: 'user@example.com',
    password: 'password123'
  })
});

const { token } = await loginResponse.json();

// Use token for subsequent requests
const todoResponse = await fetch('/api/todo-items', {
  method: 'POST',
  headers: {
    'Content-Type': 'application/json',
    'Authorization': `Bearer ${token}`
  },
  body: JSON.stringify({
    listId: 1,
    title: 'New Todo Item'
  })
});
```

### Testing with Authentication

**Location**: `tests/Application.IntegrationTests/Authentication/LoginTests.cs`

```csharp
using VerticalSliceArchitecture.Application.Features.Authentication;

[TestFixture]
public class LoginTests : BaseTestFixture
{
    [Test]
    public async Task ShouldAuthenticateValidUser()
    {
        // Arrange
        var registerCommand = new RegisterCommand(
            "test@example.com",
            "Password123",
            "Test",
            "User");

        await SendAsync(registerCommand);

        var loginCommand = new LoginCommand("test@example.com", "Password123");

        // Act
        var result = await SendAsync(loginCommand);

        // Assert
        result.Token.Should().NotBeNullOrEmpty();
        result.Email.Should().Be("test@example.com");
    }
}
```

This implementation follows the Vertical Slice Architecture principles by:

1. **Keeping related functionality together** - Each feature (login, register) is self-contained
2. **Using the existing infrastructure** - Leverages the current user service, authorization behavior, and database context
3. **Following established patterns** - Uses ErrorOr, MediatR, FluentValidation, and the controller pattern
4. **Maintaining separation of concerns** - Authentication logic is separate from business logic
5. **Supporting the existing audit trail** - CreatedBy/ModifiedBy fields will now be populated automatically

The beauty of this approach is that once you add authentication, all existing features with `[Authorize]` attributes will automatically be protected, and the audit trail will start working without any additional changes to the existing codebase.