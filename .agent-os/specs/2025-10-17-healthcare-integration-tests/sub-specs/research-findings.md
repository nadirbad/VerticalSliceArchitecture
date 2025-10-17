# Research Findings: .NET 9 Integration Testing Best Practices

**Research Date:** 2025-10-17
**Focus:** Modern integration testing patterns for ASP.NET Core 9 Minimal APIs with Vertical Slice Architecture

---

## 1. WebApplicationFactory Patterns for .NET 9 and Minimal APIs

### Overview

`WebApplicationFactory<TEntryPoint>` creates an in-memory TestServer for integration tests, where `TEntryPoint` is typically the `Program` class. This approach handles application bootstrapping and provides an `HttpClient` for making test requests.

### Best Practices

#### 1.1 Custom WebApplicationFactory

Create a custom factory by inheriting from `WebApplicationFactory<Program>`:

```csharp
public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remove real database registration
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));

            if (descriptor != null)
            {
                services.Remove(descriptor);
            }

            // Add in-memory database for testing
            services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseInMemoryDatabase("InMemoryTestDb");
            });

            // Build service provider and seed database
            var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            db.Database.EnsureCreated();
            SeedTestData(db);
        });
    }

    private static void SeedTestData(ApplicationDbContext db)
    {
        // Add seed data for tests
    }
}
```

#### 1.2 Use IClassFixture for Shared Resources

Test classes implement `IClassFixture<T>` to share factory instances across all tests in the class:

```csharp
public class BookAppointmentTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public BookAppointmentTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }
}
```

#### 1.3 WithWebHostBuilder for Test-Specific Configuration

Use `WithWebHostBuilder()` for tests requiring specific setup:

```csharp
var client = _factory
    .WithWebHostBuilder(builder =>
    {
        builder.ConfigureServices(services =>
        {
            // Test-specific service configuration
        });
    })
    .CreateClient();
```

### Key Takeaways

✅ **Separate test projects** - Keep integration tests in dedicated project
✅ **Use class fixtures** - Share resources efficiently across test classes
✅ **Override ConfigureWebHost** - Configure test-specific services
✅ **Mock external dependencies** - Replace real services with test doubles
✅ **In-memory database** - Fast, isolated database for tests

---

## 2. Test Isolation Strategies

### Database Reset Approaches

#### 2.1 Respawn Library (Recommended)

**Respawn** intelligently resets databases to initial state by deleting data from tables while respecting foreign key constraints.

```csharp
// In test base class
private static Respawner _respawner = null!;

public async Task InitializeAsync()
{
    _respawner = await Respawner.CreateAsync(_connectionString, new RespawnerOptions
    {
        TablesToIgnore = new[] { "__EFMigrationsHistory" },
        DbAdapter = DbAdapter.SqlServer
    });
}

public async Task ResetDatabaseAsync()
{
    await _respawner.ResetAsync(_connectionString);
}
```

**Pros:**
- Fast - significantly faster than recreating database
- Reliable - respects referential integrity
- Configurable - can ignore specific tables

**Cons:**
- Requires SQL Server or similar (not for in-memory)
- Additional dependency

#### 2.2 EnsureDeleted + EnsureCreated Pattern

For in-memory databases, use EF Core methods:

```csharp
public async Task ResetDatabaseAsync()
{
    await _dbContext.Database.EnsureDeletedAsync();
    await _dbContext.Database.EnsureCreatedAsync();
    await SeedTestDataAsync();
}
```

**Pros:**
- Works with in-memory database
- Simple and straightforward
- No additional dependencies

**Cons:**
- Slower than Respawn for real databases
- Recreates schema every time

#### 2.3 Transaction Rollback

Wrap each test in a transaction and rollback after completion:

```csharp
public async Task InitializeAsync()
{
    _transaction = await _dbContext.Database.BeginTransactionAsync();
}

public async Task DisposeAsync()
{
    await _transaction.RollbackAsync();
    await _transaction.DisposeAsync();
}
```

**Pros:**
- Fast rollback
- No database cleanup needed

**Cons:**
- Test data can't be debugged after test runs
- Won't work if multiple transactions are needed
- Can conflict with some EF Core operations

### xUnit Collection Management

#### 2.4 Collection Fixtures for Shared Context

By default, each xUnit test class is a unique collection. Tests within a collection run sequentially, but collections run in parallel.

```csharp
// Define a collection
[CollectionDefinition("Database collection")]
public class DatabaseCollection : ICollectionFixture<CustomWebApplicationFactory>
{
}

// Use the collection
[Collection("Database collection")]
public class BookAppointmentTests
{
    // All tests here share the same factory instance
}
```

#### 2.5 Disable Parallel Execution (If Needed)

Force all tests to run sequentially:

```csharp
// In AssemblyInfo.cs or any test file
[assembly: CollectionBehavior(CollectionBehavior.CollectionPerAssembly)]
```

### Recommended Strategy for This Project

✅ **Use EnsureDeleted + EnsureCreated** - Works perfectly with in-memory database
✅ **Implement IAsyncLifetime** - Reset database before each test
✅ **Deterministic seed data** - Use known GUIDs matching HTTP request files
✅ **Independent tests** - Each test creates its own test data

---

## 3. Vertical Slice Architecture Integration Testing

### Key Principles

Vertical Slice Architecture organizes code by features (slices) rather than technical layers. Each slice includes all layers needed for a complete feature (HTTP endpoint, validation, business logic, data access).

### Testing Approach: Subcutaneous Tests

**Subcutaneous tests** execute just below the UI layer, typically at the HTTP API level. For vertical slices with MediatR + CQRS:

```csharp
// Test sends HTTP request → Controller → MediatR Command → Handler → Database
// Then verifies: Response status code, body, and database state
```

### Benefits for Vertical Slice Testing

✅ **Tests match actual usage flow** - HTTP request → response cycle
✅ **No coupling to implementation** - Tests only know how to send requests
✅ **High cohesion** - Test entire feature slice end-to-end
✅ **CQRS validation** - Commands and queries tested separately

### Testing Pattern for MediatR + CQRS

```csharp
[Fact]
public async Task BookAppointment_WithValidData_Returns201Created()
{
    // Arrange - Build test data
    var command = new BookAppointmentTestDataBuilder()
        .WithPatientId(_testPatientId)
        .WithDoctorId(_testDoctorId)
        .WithStartTime(DateTime.UtcNow.AddDays(1))
        .Build();

    // Act - Send HTTP request (triggers MediatR command)
    var response = await _client.PostAsJsonAsync("/api/healthcare/appointments", command);

    // Assert - Verify response AND database state
    response.StatusCode.Should().Be(HttpStatusCode.Created);

    var result = await response.Content.ReadFromJsonAsync<BookAppointmentResult>();
    result.Should().NotBeNull();
    result!.Id.Should().NotBeEmpty();

    // Verify database state
    var appointment = await _dbContext.Appointments.FindAsync(result.Id);
    appointment.Should().NotBeNull();
    appointment!.PatientId.Should().Be(command.PatientId);
}
```

### Best Practices

✅ **Test through HTTP layer** - Don't call handlers directly
✅ **Verify both response and database** - Ensure complete behavior
✅ **Test entire slice** - Validation → Handler → Persistence → Events
✅ **Use realistic data** - Match production scenarios

---

## 4. Test Data Management Patterns

### Test Data Builder Pattern

The builder pattern provides a fluent API for creating test data with sensible defaults and explicit overrides.

#### 4.1 Implementation Pattern

```csharp
public class BookAppointmentTestDataBuilder
{
    private Guid _patientId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private Guid _doctorId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private DateTimeOffset _start = DateTimeOffset.UtcNow.AddHours(24);
    private DateTimeOffset _end = DateTimeOffset.UtcNow.AddHours(25);
    private string? _notes = null;

    public BookAppointmentTestDataBuilder WithPatientId(Guid patientId)
    {
        _patientId = patientId;
        return this;
    }

    public BookAppointmentTestDataBuilder WithDoctorId(Guid doctorId)
    {
        _doctorId = doctorId;
        return this;
    }

    public BookAppointmentTestDataBuilder WithStartTime(DateTimeOffset start)
    {
        _start = start;
        _end = start.AddHours(1); // Auto-adjust end time
        return this;
    }

    public BookAppointmentTestDataBuilder WithDuration(TimeSpan duration)
    {
        _end = _start.Add(duration);
        return this;
    }

    public BookAppointmentTestDataBuilder WithNotes(string notes)
    {
        _notes = notes;
        return this;
    }

    public BookAppointmentCommand Build()
    {
        return new BookAppointmentCommand(_patientId, _doctorId, _start, _end, _notes);
    }
}
```

#### 4.2 Usage in Tests

```csharp
// Happy path - use all defaults
var command = new BookAppointmentTestDataBuilder().Build();

// Override specific values
var command = new BookAppointmentTestDataBuilder()
    .WithDoctorId(specificDoctorId)
    .WithStartTime(tomorrow)
    .Build();

// Test edge cases
var command = new BookAppointmentTestDataBuilder()
    .WithDuration(TimeSpan.FromMinutes(9)) // Too short
    .Build();
```

### Key Benefits

✅ **Readable tests** - Intent is clear from method names
✅ **Reusable** - Builders used across many tests
✅ **Maintainable** - Changes to commands don't break all tests
✅ **Default values** - Tests only specify what they care about
✅ **Immutable** - Each `With` method returns new instance

### Object Mother Pattern

Combine builders with "object mother" factories for common scenarios:

```csharp
public static class TestAppointments
{
    public static BookAppointmentCommand ValidCommand() =>
        new BookAppointmentTestDataBuilder().Build();

    public static BookAppointmentCommand ShortDurationCommand() =>
        new BookAppointmentTestDataBuilder()
            .WithDuration(TimeSpan.FromMinutes(5))
            .Build();

    public static BookAppointmentCommand PastAppointmentCommand() =>
        new BookAppointmentTestDataBuilder()
            .WithStartTime(DateTimeOffset.UtcNow.AddHours(-1))
            .Build();
}
```

### Seed Data Strategy

Use deterministic GUIDs for seed data that match HTTP request files:

```csharp
private static readonly Guid TestPatient1 = Guid.Parse("11111111-1111-1111-1111-111111111111");
private static readonly Guid TestDoctor1 = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

private static void SeedTestData(ApplicationDbContext context)
{
    context.Patients.Add(new Patient { Id = TestPatient1, Name = "John Smith" });
    context.Doctors.Add(new Doctor { Id = TestDoctor1, Name = "Dr. Sarah Wilson" });
    context.SaveChanges();
}
```

---

## 5. FluentAssertions for HTTP Response Validation

### Recommended Package: FluentAssertions.Web

**FluentAssertions.Web** is the recommended extension for HTTP response assertions (as of FluentAssertions 8.0+).

```bash
dotnet add package FluentAssertions.Web
```

### Status Code Assertions

```csharp
// Standard FluentAssertions
response.StatusCode.Should().Be(HttpStatusCode.Created);

// FluentAssertions.Web - more readable
response.Should().Be201Created();
response.Should().Be200Ok();
response.Should().Be400BadRequest();
response.Should().Be404NotFound();
response.Should().Be409Conflict();
response.Should().Be422UnprocessableEntity();
```

### Content Assertions

```csharp
// Deserialize and assert
var result = await response.Content.ReadFromJsonAsync<BookAppointmentResult>();
result.Should().NotBeNull();
result!.Id.Should().NotBeEmpty();
result.StartUtc.Should().BeCloseTo(expectedStart, TimeSpan.FromSeconds(1));

// FluentAssertions.Web - assert content matches model
await response.Should().Satisfy<BookAppointmentResult>(result =>
{
    result.Id.Should().NotBeEmpty();
    result.StartUtc.Should().BeCloseTo(expectedStart, TimeSpan.FromSeconds(1));
});
```

### Header Assertions

```csharp
// Assert Location header for 201 Created
response.Headers.Location.Should().NotBeNull();
response.Headers.Location!.ToString().Should().Contain("/api/healthcare/appointments/");

// FluentAssertions.Web
response.Should().HaveHeader("Location");
```

### ProblemDetails Assertions

```csharp
// For error responses (400, 404, 409, 422)
var problemDetails = await response.Content.ReadFromJsonAsync<ProblemDetails>();
problemDetails.Should().NotBeNull();
problemDetails!.Status.Should().Be(400);
problemDetails.Title.Should().Be("One or more validation errors occurred.");
problemDetails.Extensions.Should().ContainKey("errors");
```

### Complete Example

```csharp
[Fact]
public async Task BookAppointment_WithInvalidDuration_Returns400WithValidationErrors()
{
    // Arrange
    var command = new BookAppointmentTestDataBuilder()
        .WithDuration(TimeSpan.FromMinutes(5)) // Too short
        .Build();

    // Act
    var response = await _client.PostAsJsonAsync("/api/healthcare/appointments", command);

    // Assert status code
    response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

    // Assert ProblemDetails
    var problemDetails = await response.Content.ReadFromJsonAsync<ProblemDetails>();
    problemDetails.Should().NotBeNull();
    problemDetails!.Status.Should().Be(400);
    problemDetails.Title.Should().Contain("validation");

    // Assert validation errors
    var errors = problemDetails.Extensions["errors"] as JsonElement?;
    errors.Should().NotBeNull();
    errors.Value.GetProperty("End").GetArrayLength().Should().BeGreaterThan(0);
}
```

### Best Practices

✅ **Use FluentAssertions.Web** - More readable status code assertions
✅ **Deserialize responses** - Validate strongly-typed response bodies
✅ **Check ProblemDetails** - Verify error responses follow RFC 7807
✅ **Assert error messages** - Ensure validation errors are meaningful
✅ **Verify headers** - Check Location, Content-Type, etc.

---

## Recommended Patterns for This Project

### Test Project Structure

```
tests/
  Application.IntegrationTests/
    Infrastructure/
      CustomWebApplicationFactory.cs       # In-memory database setup
      IntegrationTestBase.cs               # Base class with IAsyncLifetime
    Helpers/
      TestDataBuilders/
        BookAppointmentTestDataBuilder.cs
        RescheduleAppointmentTestDataBuilder.cs
        IssuePrescriptionTestDataBuilder.cs
      ResponseHelper.cs                    # ProblemDetails parsing
      HttpClientExtensions.cs              # Typed POST/GET helpers
    Healthcare/
      Appointments/
        BookAppointmentTests.cs
        RescheduleAppointmentTests.cs
      Prescriptions/
        IssuePrescriptionTests.cs
```

### Technology Stack

- ✅ **xUnit** - Test framework (already used in unit tests)
- ✅ **WebApplicationFactory** - In-memory test server
- ✅ **InMemory Database** - Fast, isolated database (already configured)
- ✅ **FluentAssertions** - Readable assertions (already used)
- ✅ **FluentAssertions.Web** - HTTP-specific assertions
- ✅ **Test Data Builders** - Fluent test data creation

### Testing Workflow

1. **Arrange** - Use test data builders to create command/query
2. **Act** - Send HTTP request via HttpClient
3. **Assert** - Verify:
   - HTTP status code (using FluentAssertions.Web)
   - Response body (deserialize and assert properties)
   - Database state (query DbContext to verify persistence)
   - Error messages (for validation failures)

### Example Test Class Structure

```csharp
public class BookAppointmentTests : IntegrationTestBase
{
    public BookAppointmentTests(CustomWebApplicationFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task BookAppointment_HappyPath_Returns201Created()
    {
        // Arrange
        var command = new BookAppointmentTestDataBuilder().Build();

        // Act
        var response = await Client.PostAsJsonAsync("/api/healthcare/appointments", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        // ... more assertions
    }

    [Fact]
    public async Task BookAppointment_InvalidDuration_Returns400BadRequest()
    {
        // Test validation error
    }

    [Fact]
    public async Task BookAppointment_DoctorConflict_Returns409Conflict()
    {
        // Test business rule violation
    }
}
```

---

## Key Takeaways

### DO ✅

- Use `WebApplicationFactory` with in-memory database for fast, isolated tests
- Implement `IAsyncLifetime` to reset database before each test
- Create test data builders with fluent API for readable, maintainable tests
- Test through HTTP layer (subcutaneous tests) to validate entire vertical slice
- Use FluentAssertions.Web for readable HTTP response assertions
- Verify both response and database state in each test
- Use deterministic GUIDs for seed data matching HTTP request files

### DON'T ❌

- Don't call MediatR handlers directly - test through HTTP layer
- Don't share mutable state between tests - reset database before each test
- Don't hardcode test data in every test - use builders with defaults
- Don't skip database state verification - ensure persistence works correctly
- Don't test only happy paths - cover validation errors, conflicts, edge cases
- Don't couple tests to implementation details - test behavior, not internals

---

## References

- [Microsoft: Integration tests in ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests?view=aspnetcore-9.0)
- [Microsoft: Unit and integration tests in Minimal API apps](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/minimal-apis/test-min-api?view=aspnetcore-9.0)
- [Jimmy Bogard: Vertical Slice Architecture](https://www.jimmybogard.com/vertical-slice-architecture/)
- [Jimmy Bogard: Vertical Slice Test Fixtures](https://lostechies.com/jimmybogard/2016/10/24/vertical-slice-test-fixtures-for-mediatr-and-asp-net-core/)
- [Steve Smith: Builder Pattern for Test Data](https://ardalis.com/improve-tests-with-the-builder-pattern-for-test-data/)
- [FluentAssertions.Web GitHub](https://github.com/adrianiftode/FluentAssertions.Web)
- [Respawn Library for Database Reset](https://github.com/jbogard/Respawn)
