# Approve/Deny Refill Request - Feature Specification

**Feature:** Approve/Deny Refill Request
**Priority:** Medium  
**Effort:** M (1 week)
**Status:** Draft
**Created:** 2025-10-21

## Overview

Doctors review pending refill requests and either approve (increments prescription refills used) or deny (with reason).

## Business Requirements

- Only process Pending requests (idempotency)
- Approve: Increment RefillsUsed on prescription, mark request Approved
- Deny: Capture denial reason, mark request Denied
- Domain events for patient notification

## API Endpoints

```http
POST /api/healthcare/refill-requests/{requestId}/approve
Content-Type: application/json

{
  "doctorId": "guid",
  "notes": "Approved - patient following treatment plan"
}
```

```http
POST /api/healthcare/refill-requests/{requestId}/deny
Content-Type: application/json

{
  "doctorId": "guid",
  "reason": "Need in-person consultation before additional refills"
}
```

## Domain Methods

```csharp
public void Approve(Guid doctorId, string? notes = null)
{
    if (Status != RefillRequestStatus.Pending)
        throw new InvalidOperationException("Only pending requests can be approved");
        
    Status = RefillRequestStatus.Approved;
    ProcessedUtc = DateTime.UtcNow;
    ProcessedByDoctorId = doctorId;
    ProcessingNotes = notes;
}

public void Deny(Guid doctorId, string reason)
{
    if (string.IsNullOrWhiteSpace(reason))
        throw new ArgumentException("Denial reason is required");
        
    if (Status != RefillRequestStatus.Pending)
        throw new InvalidOperationException("Only pending requests can be denied");
        
    Status = RefillRequestStatus.Denied;
    ProcessedUtc = DateTime.UtcNow;
    ProcessedByDoctorId = doctorId;
    ProcessingNotes = reason;
}
```

## Testing Requirements

**Integration Tests:** 12-15 tests
- Approve pending request
- Deny pending request  
- Cannot approve already processed
- Cannot deny without reason
- Prescription refills updated on approval
- Domain events raised

## Timeline

**Day 1-2:** Domain methods + unit tests
**Day 3-4:** Handlers + integration tests
**Day 5-6:** HTTP requests + documentation
**Day 7:** Review + cleanup
