# Product Roadmap

## Phase 0: Already Completed ✓

The following features have been implemented and demonstrate the Vertical Slice Architecture approach:

### Todo Domain (Reference Implementation)

- [x] **Create Todo List** - Create new todo lists with title and color selection `S`
- [x] **Update Todo List** - Update existing todo list properties `XS`
- [x] **Delete Todo List** - Delete todo lists with cascade to items `XS`
- [x] **Get Todos** - Retrieve all todo lists with items in hierarchical structure `XS`
- [x] **Create Todo Item** - Add new todo items to lists with validation `S`
- [x] **Update Todo Item** - Update todo item properties (title, done status) `XS`
- [x] **Update Todo Item Detail** - Update detailed properties of todo items `XS`
- [x] **Delete Todo Item** - Remove todo items from lists `XS`
- [x] **Get Todo Items with Pagination** - Query todo items with pagination support `S`
- [x] **Export Todos** - Export todo lists to CSV format `M`
- [x] **Domain Events** - TodoItemCreated, TodoItemCompleted, TodoItemDeleted events with handlers `M`

### Healthcare Domain - Appointments

- [x] **Book Appointment** - Schedule appointments with doctor availability conflict detection `M`
  - Patient and doctor existence validation
  - Overlapping time slot conflict detection
  - Time window validation (min 10 minutes, max 8 hours)
  - 15-minute advance booking requirement
  - AppointmentBooked domain event with handler
  - Proper UTC datetime handling
  - Concurrency control with RowVersion

### Core Infrastructure

- [x] **Domain Entity Base Classes** - AuditableEntity, IHasDomainEvent interfaces `S`
- [x] **Rich Domain Models** - Patient, Doctor, Appointment with private setters and business methods `M`
- [x] **Entity Framework Configuration** - DbContext, entity configurations, migrations `M`
- [x] **Pipeline Behaviors** - Validation, Logging, Performance Monitoring, Authorization behaviors `M`
- [x] **Error Handling** - ErrorOr pattern integration with ApiControllerBase `S`
- [x] **In-Memory Database** - Toggle between in-memory and SQL Server via configuration `S`

### Testing & Quality

- [x] **Test Projects Structure** - Application.UnitTests and Application.IntegrationTests projects `S`
- [x] **Code Style Enforcement** - EditorConfig, StyleCop analyzers, TreatWarningsAsErrors `S`
- [x] **HTTP Request Files** - Manual testing examples for implemented endpoints `S`

**Goal:** Establish foundational VSA patterns and demonstrate with both simple (Todo) and complex (Healthcare) domains

**Success Criteria:** ✓ Working API with Swagger documentation, all tests passing, code style enforced

---

## Phase 1: Core Healthcare Management

**Goal:** Complete the essential healthcare appointment and prescription management features

**Success Criteria:**
- Healthcare domain has feature parity with Todo domain in terms of completeness
- All CRUD operations for appointments and prescriptions work end-to-end
- Realistic business rules enforced in domain models
- Full test coverage for healthcare features

### Features

- [ ] **Reschedule Appointment** - Allow rescheduling existing appointments with conflict detection `M`
  - Check appointment status (cannot reschedule cancelled/completed)
  - Verify new time slot doesn't conflict with doctor's schedule
  - Update appointment with rescheduling reason
  - Maintain audit trail via notes field
  - Raise AppointmentRescheduled domain event
  - Spec: `.github/specs/Healthcare/Appointments/RescheduleAppointment.md`

- [ ] **Complete Appointment** - Mark appointments as completed with completion notes `S`
  - Status validation (cannot complete cancelled appointments)
  - Idempotency check (already completed)
  - Optional completion notes
  - Status transition to Completed

- [ ] **Cancel Appointment** - Cancel scheduled or rescheduled appointments `S`
  - Status validation (cannot cancel completed appointments)
  - Cancellation reason capture
  - Status transition to Cancelled
  - Optional notification via domain event

- [ ] **Get Appointments** - Query appointments with filtering and pagination `M`
  - Filter by patient, doctor, date range, status
  - Include patient and doctor details
  - Pagination support
  - Sort by appointment time

- [ ] **Issue Prescription** - Doctors issue prescriptions for patients `M`
  - Patient and doctor existence validation
  - Medication, dosage, and directions capture
  - Expiration date calculation (IssuedUtc + daysValid)
  - Maximum refills configuration (0-12)
  - PrescriptionIssued domain event
  - Audit log entry
  - Spec: `.github/specs/Healthcare/Prescriptions/IssuePrescription.md`

- [ ] **Request Medication Refill** - Patients request prescription refills `L`
  - Prescription existence and expiration validation
  - Check refills remaining (RefillsUsed < MaxRefills)
  - Create RefillRequest with pending status
  - MedicationRefillRequested domain event
  - Link to prescription and patient
  - Spec: `.github/specs/Healthcare/Prescriptions/RequestRefill.md`

- [ ] **Approve/Deny Refill Request** - Doctors review and process refill requests `M`
  - Verify request is in pending status (prevent double-processing)
  - Approve: increment RefillsUsed on prescription, set ApprovedUtc
  - Deny: set DeniedUtc and capture reason
  - MedicationRefillApproved or MedicationRefillDenied events
  - Status validation and audit trail

### Dependencies

- Entity Framework migrations for Prescription and RefillRequest entities
- Domain entity implementations for Prescription and RefillRequest
- Event handlers for prescription-related events
- HTTP request files for manual testing

---

## Phase 2: Enhanced Healthcare Features

**Goal:** Add supporting features that demonstrate cross-feature concerns and realistic production requirements

**Success Criteria:**
- Comprehensive querying capabilities across healthcare entities
- Audit trail provides compliance-ready tracking
- Event handlers demonstrate cross-feature communication patterns

### Features

- [ ] **Get Prescriptions** - Query prescriptions with filtering `M`
  - Filter by patient, doctor, medication, active/expired status
  - Include refill history
  - Pagination support
  - Calculate refills remaining

- [ ] **Get Refill Requests** - Query refill requests with filtering `M`
  - Filter by patient, doctor, prescription, status
  - Include prescription details
  - Pagination and sorting

- [ ] **Patient Management** - CRUD operations for patients `M`
  - Create patient with full name, email, phone validation
  - Update patient information
  - Soft delete with cascade considerations
  - View patient with appointment and prescription history

- [ ] **Doctor Management** - CRUD operations for doctors `M`
  - Create doctor with specialty
  - Update doctor information
  - Soft delete with cascade considerations
  - View doctor with appointment schedule

- [ ] **Audit Log Querying** - Query audit logs for compliance `M`
  - Filter by actor, action, date range
  - Support for various actor types (Patient, Doctor, Admin)
  - Pagination for large result sets
  - Export capabilities

- [ ] **Notification Event Handlers** - Send notifications via domain events `L`
  - Email/SMS notifications for appointment booked/rescheduled/cancelled
  - Prescription issued and refill approved notifications
  - Reminder notifications for upcoming appointments
  - Configurable notification preferences

- [ ] **Appointment Conflict Detection Enhancement** - Improve scheduling logic `M`
  - Check patient availability (prevent double-booking for patients)
  - Buffer time between appointments for doctors
  - Business hours validation
  - Holiday/doctor availability calendar

### Dependencies

- Email/SMS service abstractions (out-of-process via domain events)
- Audit log entity configuration and persistence
- Enhanced business rules in domain entities

---

## Phase 3: Documentation & Polish

**Goal:** Ensure the template is production-quality, well-documented, and ready for community adoption

**Success Criteria:**
- Comprehensive documentation for all features
- Clear examples in HTTP request files
- Blog post and presentation materials prepared
- Community feedback incorporated

### Features

- [ ] **Integration Test Coverage** - Full test coverage for all healthcare features `L`
  - End-to-end scenarios for each feature
  - Validation error scenarios
  - Conflict and error handling
  - Domain event verification

- [ ] **Unit Test Coverage** - Domain logic and validation testing `M`
  - Appointment business methods (Reschedule, Complete, Cancel)
  - Prescription business methods
  - Validator classes
  - Event handler logic

- [ ] **HTTP Request Examples** - Complete manual testing files `S`
  - Happy path scenarios for all endpoints
  - Error scenarios (validation, conflicts, not found)
  - Organized by feature area
  - Located in `requests/Healthcare/**/*.http`

- [ ] **Architecture Documentation** - Enhanced CLAUDE.md and README `M`
  - Healthcare domain modeling explanation
  - When to use factory methods vs constructors
  - Domain event patterns and best practices
  - Testing strategies for vertical slices
  - Migration guide from Clean Architecture

- [ ] **Performance Benchmarks** - Demonstrate scalability `M`
  - Benchmark common operations
  - Database query optimization
  - Index recommendations
  - Performance comparison with layered approach

- [ ] **Deployment Guide** - Production deployment instructions `S`
  - Docker containerization
  - Azure deployment with App Service
  - SQL Server configuration
  - Environment-specific configuration

- [ ] **Video Walkthrough** - Recorded code walkthrough `L`
  - Architecture overview
  - Implementing a feature from scratch
  - Testing approaches
  - Comparison with Clean Architecture

### Dependencies

- Community feedback on Phase 1 and 2 implementations
- Performance testing tools setup
- Documentation review and editing

---

## Future Considerations (Phase 4+)

Features to consider based on community interest and feedback:

- **Authentication & Authorization** - Add Identity/JWT with role-based access control
- **API Versioning** - Demonstrate versioning strategies for vertical slices
- **Multi-Tenancy** - Show how VSA handles tenant isolation
- **Advanced Querying** - GraphQL or OData for flexible client queries
- **Background Jobs** - Hangfire/Quartz integration for scheduled tasks
- **Real-Time Updates** - SignalR for appointment notifications
- **Mobile BFF** - Backend-for-frontend pattern for mobile clients
- **Microservices Extraction** - Show evolution path from monolith to services

---

## Effort Scale Reference

- **XS**: 1 day - Simple CRUD, straightforward validation
- **S**: 2-3 days - Single feature with business rules, tests, documentation
- **M**: 1 week - Complex feature with multiple validations, domain events, cross-feature concerns
- **L**: 2 weeks - Feature with multiple sub-features, extensive testing, complex domain logic
- **XL**: 3+ weeks - Major architectural changes, new infrastructure, comprehensive documentation