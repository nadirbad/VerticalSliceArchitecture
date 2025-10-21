# Get Appointments - Feature Specification

**Feature:** Get Appointments Query
**Priority:** High
**Effort:** M (1 week)
**Status:** Draft
**Created:** 2025-10-21

## Overview

Provide a flexible query endpoint for retrieving appointments with filtering, pagination, and sorting capabilities. This is essential for building UIs, dashboards, and reports.

## Business Requirements

### User Stories

**As a** patient
**I want to** view my upcoming and past appointments
**So that** I can manage my healthcare schedule

**As a** doctor
**I want to** view my appointment schedule with patient details
**So that** I can prepare for appointments and manage my time

**As a** healthcare administrator
**I want to** query appointments with various filters
**So that** I can generate reports and manage the practice

### Query Capabilities

1. **Filter by Patient** - View all appointments for a specific patient
2. **Filter by Doctor** - View all appointments for a specific doctor
3. **Filter by Date Range** - View appointments between start and end dates
4. **Filter by Status** - View appointments in specific statuses (Scheduled, Rescheduled, Completed, Cancelled)
5. **Pagination** - Handle large result sets efficiently
6. **Sorting** - Order by appointment time (ascending/descending)
7. **Include Related Data** - Patient and doctor details in response

## Technical Specification

### API Endpoint

```http
GET /api/healthcare/appointments?patientId={guid}&doctorId={guid}&startDate={date}&endDate={date}&status={status}&pageNumber={int}&pageSize={int}&sortBy={field}&sortDescending={bool}
```

**Query Parameters (All Optional):**
- `patientId` (Guid) - Filter by patient
- `doctorId` (Guid) - Filter by doctor
- `startDate` (DateTime) - Filter appointments starting from this date (inclusive)
- `endDate` (DateTime) - Filter appointments until this date (inclusive)
- `status` (string) - Filter by status (Scheduled, Rescheduled, Completed, Cancelled)
- `pageNumber` (int, default: 1) - Page number (1-based)
- `pageSize` (int, default: 10, max: 100) - Items per page
- `sortBy` (string, default: "StartTime") - Sort field (StartTime, CreatedUtc)
- `sortDescending` (bool, default: false) - Sort direction

**Response (200 OK):**
```json
{
  "items": [
    {
      "id": "guid",
      "patientId": "guid",
      "patientName": "John Smith",
      "doctorId": "guid",
      "doctorName": "Dr. Sarah Wilson",
      "doctorSpecialty": "Family Medicine",
      "startUtc": "2025-10-28T10:00:00Z",
      "endUtc": "2025-10-28T10:30:00Z",
      "status": "Scheduled",
      "notes": "Annual checkup",
      "createdUtc": "2025-10-21T09:00:00Z",
      "rescheduledFromUtc": null,
      "completedUtc": null,
      "cancelledUtc": null,
      "cancellationReason": null
    }
  ],
  "pagination": {
    "pageNumber": 1,
    "pageSize": 10,
    "totalCount": 42,
    "totalPages": 5,
    "hasPreviousPage": false,
    "hasNextPage": true
  }
}
```

### Query Model

**GetAppointmentsQuery:**
```csharp
public record GetAppointmentsQuery(
    Guid? PatientId,
    Guid? DoctorId,
    DateTime? StartDate,
    DateTime? EndDate,
    AppointmentStatus? Status,
    int PageNumber = 1,
    int PageSize = 10,
    string SortBy = "StartTime",
    bool SortDescending = false) : IRequest<ErrorOr<PaginatedResult<AppointmentDto>>>;
```

**Validation Rules:**
- `PageNumber` - Must be >= 1
- `PageSize` - Must be between 1 and 100
- `SortBy` - Must be one of: "StartTime", "CreatedUtc"
- `StartDate` - If provided, must be <= EndDate
- `EndDate` - If provided, must be >= StartDate

### Response Models

**AppointmentDto:**
```csharp
public record AppointmentDto(
    Guid Id,
    Guid PatientId,
    string PatientName,
    Guid DoctorId,
    string DoctorName,
    string DoctorSpecialty,
    DateTime StartUtc,
    DateTime EndUtc,
    string Status,
    string? Notes,
    DateTime CreatedUtc,
    DateTime? RescheduledFromUtc,
    DateTime? CompletedUtc,
    DateTime? CancelledUtc,
    string? CancellationReason);
```

**PaginatedResult:**
```csharp
public record PaginatedResult<T>(
    List<T> Items,
    PaginationMetadata Pagination);

public record PaginationMetadata(
    int PageNumber,
    int PageSize,
    int TotalCount,
    int TotalPages,
    bool HasPreviousPage,
    bool HasNextPage);
```

### Handler Logic

```csharp
public async Task<ErrorOr<PaginatedResult<AppointmentDto>>> Handle(
    GetAppointmentsQuery request,
    CancellationToken cancellationToken)
{
    // Build query with filters
    var query = _context.Appointments
        .Include(a => a.Patient)
        .Include(a => a.Doctor)
        .AsNoTracking()
        .AsQueryable();

    // Apply filters
    if (request.PatientId.HasValue)
        query = query.Where(a => a.PatientId == request.PatientId.Value);

    if (request.DoctorId.HasValue)
        query = query.Where(a => a.DoctorId == request.DoctorId.Value);

    if (request.StartDate.HasValue)
        query = query.Where(a => a.StartUtc >= request.StartDate.Value);

    if (request.EndDate.HasValue)
        query = query.Where(a => a.StartUtc <= request.EndDate.Value);

    if (request.Status.HasValue)
        query = query.Where(a => a.Status == request.Status.Value);

    // Apply sorting
    query = request.SortBy switch
    {
        "CreatedUtc" => request.SortDescending
            ? query.OrderByDescending(a => a.CreatedUtc)
            : query.OrderBy(a => a.CreatedUtc),
        _ => request.SortDescending
            ? query.OrderByDescending(a => a.StartUtc)
            : query.OrderBy(a => a.StartUtc)
    };

    // Get total count before pagination
    var totalCount = await query.CountAsync(cancellationToken);

    // Apply pagination
    var items = await query
        .Skip((request.PageNumber - 1) * request.PageSize)
        .Take(request.PageSize)
        .Select(a => new AppointmentDto(
            a.Id,
            a.PatientId,
            a.Patient.FullName,
            a.DoctorId,
            a.Doctor.FullName,
            a.Doctor.Specialty,
            a.StartUtc,
            a.EndUtc,
            a.Status.ToString(),
            a.Notes,
            a.CreatedUtc,
            a.RescheduledFromUtc,
            a.CompletedUtc,
            a.CancelledUtc,
            a.CancellationReason))
        .ToListAsync(cancellationToken);

    // Build pagination metadata
    var totalPages = (int)Math.Ceiling(totalCount / (double)request.PageSize);
    var pagination = new PaginationMetadata(
        request.PageNumber,
        request.PageSize,
        totalCount,
        totalPages,
        request.PageNumber > 1,
        request.PageNumber < totalPages);

    return new PaginatedResult<AppointmentDto>(items, pagination);
}
```

### Database Considerations

**Recommended Indexes:**
```sql
CREATE INDEX IX_Appointments_PatientId ON Appointments(PatientId);
CREATE INDEX IX_Appointments_DoctorId ON Appointments(DoctorId);
CREATE INDEX IX_Appointments_StartUtc ON Appointments(StartUtc);
CREATE INDEX IX_Appointments_Status ON Appointments(Status);
CREATE INDEX IX_Appointments_PatientId_StartUtc ON Appointments(PatientId, StartUtc);
CREATE INDEX IX_Appointments_DoctorId_StartUtc ON Appointments(DoctorId, StartUtc);
```

## Testing Requirements

### Unit Tests

**Validator Tests:**
1. ✅ Valid query - passes validation
2. ✅ PageNumber = 0 - fails validation
3. ✅ PageSize = 0 - fails validation
4. ✅ PageSize = 101 - fails validation
5. ✅ StartDate > EndDate - fails validation
6. ✅ Invalid SortBy - fails validation
7. ✅ All optional parameters null - passes
8. ✅ PageSize = 100 - passes (boundary)

**Handler Tests:**
1. ✅ No filters - returns all appointments
2. ✅ Filter by patient - returns only patient's appointments
3. ✅ Filter by doctor - returns only doctor's appointments
4. ✅ Filter by date range - returns appointments in range
5. ✅ Filter by status - returns appointments with status
6. ✅ Multiple filters combined - applies all filters
7. ✅ Pagination - returns correct page
8. ✅ Sorting ascending - correct order
9. ✅ Sorting descending - correct order
10. ✅ Empty result set - returns empty list with correct pagination

### Integration Tests

1. ✅ **No Filters** - Returns all appointments with pagination
2. ✅ **Filter by Patient** - Returns only patient's appointments
3. ✅ **Filter by Doctor** - Returns only doctor's appointments
4. ✅ **Filter by Date Range** - Returns appointments in range
5. ✅ **Filter by Status Scheduled** - Returns only scheduled
6. ✅ **Filter by Status Cancelled** - Returns only cancelled
7. ✅ **Multiple Filters** - Patient + Status combination works
8. ✅ **Pagination First Page** - Returns first 10 items
9. ✅ **Pagination Second Page** - Returns next 10 items
10. ✅ **Pagination Last Page** - Returns remaining items
11. ✅ **Page Size Limit** - PageSize 100 works, 101 fails
12. ✅ **Sort by StartTime Ascending** - Correct order
13. ✅ **Sort by StartTime Descending** - Correct order
14. ✅ **Sort by CreatedUtc** - Works correctly
15. ✅ **Empty Result** - No matches returns empty list
16. ✅ **Patient Name Included** - Patient details in response
17. ✅ **Doctor Details Included** - Doctor name and specialty in response
18. ✅ **All Status Fields** - CompletedUtc, CancelledUtc populated correctly
19. ✅ **Invalid Page Number** - Returns 400
20. ✅ **Total Count Accurate** - Pagination metadata correct

**Estimated:** 15-20 integration tests

## Implementation Checklist

- [ ] Create `GetAppointmentsQuery` record
- [ ] Create `AppointmentDto` record
- [ ] Create `PaginatedResult<T>` and `PaginationMetadata` records
- [ ] Implement query validator
- [ ] Implement query handler with filtering logic
- [ ] Configure Minimal API endpoint (GET)
- [ ] Add database indexes for performance
- [ ] Write unit tests (validator + handler)
- [ ] Write integration tests
- [ ] Create HTTP request file with examples
- [ ] Update CLAUDE.md with query pattern example
- [ ] Performance test with large dataset
- [ ] Code review

## HTTP Request Examples

Create `requests/Healthcare/Appointments/GetAppointments.http`:

```http
@baseUrl = https://localhost:7098

### Get all appointments (default pagination)
GET {{baseUrl}}/api/healthcare/appointments

### Get appointments for specific patient
GET {{baseUrl}}/api/healthcare/appointments?patientId=11111111-1111-1111-1111-111111111111

### Get appointments for specific doctor
GET {{baseUrl}}/api/healthcare/appointments?doctorId=aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa

### Get appointments in date range
GET {{baseUrl}}/api/healthcare/appointments?startDate=2025-10-01&endDate=2025-10-31

### Get scheduled appointments only
GET {{baseUrl}}/api/healthcare/appointments?status=Scheduled

### Get cancelled appointments
GET {{baseUrl}}/api/healthcare/appointments?status=Cancelled

### Get completed appointments
GET {{baseUrl}}/api/healthcare/appointments?status=Completed

### Get patient's upcoming appointments
GET {{baseUrl}}/api/healthcare/appointments?patientId=11111111-1111-1111-1111-111111111111&startDate=2025-10-21&status=Scheduled

### Get doctor's schedule for today
GET {{baseUrl}}/api/healthcare/appointments?doctorId=aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa&startDate=2025-10-21&endDate=2025-10-21

### Pagination - Page 2 with 20 items per page
GET {{baseUrl}}/api/healthcare/appointments?pageNumber=2&pageSize=20

### Pagination - Page 1 with 5 items per page
GET {{baseUrl}}/api/healthcare/appointments?pageNumber=1&pageSize=5

### Sort by start time descending (most recent first)
GET {{baseUrl}}/api/healthcare/appointments?sortBy=StartTime&sortDescending=true

### Sort by created date descending (newest created first)
GET {{baseUrl}}/api/healthcare/appointments?sortBy=CreatedUtc&sortDescending=true

### Complex query - Patient's completed appointments in date range, sorted
GET {{baseUrl}}/api/healthcare/appointments?patientId=11111111-1111-1111-1111-111111111111&status=Completed&startDate=2025-01-01&endDate=2025-10-21&sortDescending=true

### Invalid page number (validation error)
GET {{baseUrl}}/api/healthcare/appointments?pageNumber=0

### Invalid page size (validation error)
GET {{baseUrl}}/api/healthcare/appointments?pageSize=101

### Invalid date range (validation error)
GET {{baseUrl}}/api/healthcare/appointments?startDate=2025-12-01&endDate=2025-01-01
```

## Performance Considerations

### Query Optimization

1. **Use AsNoTracking()** - Read-only queries don't need change tracking
2. **Select Only Needed Fields** - Project to DTO in query
3. **Indexes on Filter Fields** - PatientId, DoctorId, StartUtc, Status
4. **Composite Indexes** - PatientId+StartUtc, DoctorId+StartUtc for common queries
5. **Limit Include Depth** - Only include Patient and Doctor, not nested relations

### Expected Performance

- **< 100ms** for filtered queries with indexes
- **< 200ms** for complex multi-filter queries
- **< 50ms** for queries with specific IDs (indexed)

### Caching Considerations (Future)

- Cache patient's upcoming appointments (5 min TTL)
- Cache doctor's daily schedule (15 min TTL)
- Invalidate on appointment create/update/delete

## Documentation Updates

### CLAUDE.md Addition

Add section showing query pattern in VSA:

```markdown
### Querying with Filtering and Pagination

```csharp
// Query with optional filters and pagination
public record GetAppointmentsQuery(
    Guid? PatientId,
    Guid? DoctorId,
    DateTime? StartDate,
    DateTime? EndDate,
    AppointmentStatus? Status,
    int PageNumber = 1,
    int PageSize = 10) : IRequest<ErrorOr<PaginatedResult<AppointmentDto>>>;

// Build query dynamically based on filters
var query = _context.Appointments.AsNoTracking();

if (request.PatientId.HasValue)
    query = query.Where(a => a.PatientId == request.PatientId.Value);

// Apply pagination
var items = await query
    .Skip((request.PageNumber - 1) * request.PageSize)
    .Take(request.PageSize)
    .ToListAsync();
```

### Query Pattern Best Practices

- Use `AsNoTracking()` for read-only queries
- Project to DTOs to avoid over-fetching
- Add indexes for commonly filtered fields
- Validate pagination parameters
- Return metadata (total count, page info)
- Consider caching for frequently accessed data
```

## Acceptance Criteria

- [x] Can filter by patient ID
- [x] Can filter by doctor ID
- [x] Can filter by date range (start and/or end)
- [x] Can filter by appointment status
- [x] Multiple filters can be combined
- [x] Pagination works correctly with all filters
- [x] Sorting by StartTime ascending/descending works
- [x] Sorting by CreatedUtc works
- [x] Patient name included in response
- [x] Doctor name and specialty included in response
- [x] Pagination metadata accurate (total count, pages, has next/previous)
- [x] Empty result sets handled gracefully
- [x] Performance acceptable with indexes (< 200ms for complex queries)
- [x] All unit tests passing (15+ tests)
- [x] All integration tests passing (15-20 tests)
- [x] HTTP request file with 10+ examples
- [x] Documentation updated with query patterns

## Timeline

- **Day 1-2:** Domain models + handler scaffolding
- **Day 3:** Filtering logic + unit tests
- **Day 4:** Pagination + sorting + unit tests
- **Day 5:** Integration tests
- **Day 6:** HTTP requests + performance testing
- **Day 7:** Documentation + code review

## Next Steps

After completing this feature:
1. Use same pattern for Get Prescriptions
2. Consider adding search capabilities (patient name, etc.)
3. Add caching layer if performance requires
4. Begin Request Medication Refill feature
