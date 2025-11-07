# Request Medication Refill - Feature Specification

**Feature:** Request Medication Refill
**Priority:** Medium
**Effort:** L (2 weeks)
**Status:** Draft
**Created:** 2025-10-21

## Overview

Allow patients to request refills for active prescriptions that have remaining refills available. Creates a workflow for doctor approval.

## Business Requirements

- Patient can request refill if prescription is active and has refills remaining
- Prescription must not be expired
- RefillRequest entity tracks request status (Pending, Approved, Denied)
- Domain event raised for doctor notification

## API Endpoint

```http
POST /api/healthcare/prescriptions/{prescriptionId}/request-refill
Content-Type: application/json

{
  "requestNotes": "Running low on medication, need refill"
}
```

## New Entity: RefillRequest

```csharp
public class RefillRequest
{
    public Guid Id { get; private set; }
    public Guid PrescriptionId { get; private set; }
    public Guid PatientId { get; private set; }
    public RefillRequestStatus Status { get; private set; } // Pending, Approved, Denied
    public string? RequestNotes { get; private set; }
    public DateTime RequestedUtc { get; private set; }
    public DateTime? ProcessedUtc { get; private set; }
    public Guid? ProcessedByDoctorId { get; private set; }
    public string? ProcessingNotes { get; private set; }
    
    // Navigation properties
    public Prescription Prescription { get; private set; }
    public Patient Patient { get; private set; }
    public Doctor? ProcessedByDoctor { get; private set; }
}
```

## Validation Rules

- Prescription must exist
- Prescription must not be expired (IssuedUtc + DurationInDays > Now)
- Prescription must have remaining refills (RemainingRefills > 0)
- No pending refill request already exists for this prescription
- Request notes optional, max 500 characters

## Testing Requirements

**Integration Tests:** 15-18 tests
- Happy path
- Prescription expired
- No refills remaining
- Prescription not found
- Duplicate pending request
- Notes too long

## Timeline

**Day 1-3:** Entity + migration + domain method
**Day 4-7:** Handler + validation + unit tests  
**Day 8-11:** Integration tests
**Day 12-14:** HTTP requests + documentation + review
