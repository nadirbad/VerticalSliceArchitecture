# Spec Tasks

## Tasks

- [x] 1. Create Prescription Domain Entity and Events
  - [x] 1.1 Write unit tests for Prescription entity factory method and business rules
  - [x] 1.2 Implement Prescription entity with private setters and validation
  - [x] 1.3 Implement PrescriptionStatus enum (Active, Expired, Depleted)
  - [x] 1.4 Implement Issue() factory method with business rule enforcement
  - [x] 1.5 Implement computed properties (IsExpired, IsDepleted)
  - [x] 1.6 Create PrescriptionIssuedEvent domain event
  - [x] 1.7 Create PrescriptionConfiguration for EF Core mapping
  - [x] 1.8 Verify all tests pass

- [x] 2. Add Database Schema and Migration
  - [x] 2.1 Add Prescriptions DbSet to ApplicationDbContext
  - [x] 2.2 Generate EF Core migration for Prescriptions table (N/A - using in-memory database)
  - [x] 2.3 Review migration SQL for correctness (N/A - using in-memory database)
  - [x] 2.4 Apply migration to database (if using SQL Server) or verify in-memory setup
  - [x] 2.5 Add optional seed data for development/testing

- [ ] 3. Implement Issue Prescription Feature (Vertical Slice)
  - [x] 3.1 Write unit tests for IssuePrescriptionCommandValidator
  - [ ] 3.2 Write integration tests for IssuePrescription endpoint (happy path and error scenarios) [SKIPPED]
  - [x] 3.3 Create IssuePrescriptionCommand record
  - [x] 3.4 Implement IssuePrescriptionCommandValidator with FluentValidation
  - [x] 3.5 Create PrescriptionResponse DTO record
  - [x] 3.6 Implement IssuePrescriptionCommandHandler with error handling
  - [x] 3.7 Create Minimal API endpoint and register in Program.cs
  - [x] 3.8 Verify all tests pass

- [ ] 4. Create HTTP Request Scenarios and Manual Testing
  - [ ] 4.1 Create requests/healthcare/issue-prescription.http file
  - [ ] 4.2 Add success scenarios (standard prescription, long-term medication, no refills)
  - [ ] 4.3 Add validation error scenarios (invalid quantity, refills, duration)
  - [ ] 4.4 Add not found scenarios (invalid patient/doctor IDs)
  - [ ] 4.5 Add missing field scenarios
  - [ ] 4.6 Test all scenarios manually and verify responses
  - [ ] 4.7 Run full test suite and verify all tests pass
  - [ ] 4.8 Verify domain events are raised correctly
