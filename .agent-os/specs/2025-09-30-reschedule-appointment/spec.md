# Spec Requirements Document

> Spec: Reschedule Appointment
> Created: 2025-09-30

## Overview

Implement appointment rescheduling functionality that allows patients, doctors, and administrators to modify existing appointment times while maintaining data integrity through conflict detection and business rule enforcement. This feature completes the core appointment management capabilities by enabling flexibility for both healthcare providers and patients when schedules need to change.

## User Stories

### Patient Reschedules Appointment

As a **patient**, I want to **reschedule my upcoming appointment to a different time**, so that **I can accommodate changes in my personal schedule without losing my appointment slot**.

**Workflow:**
1. Patient views their upcoming appointments
2. Patient selects an appointment they want to reschedule
3. Patient chooses a new date and time that works for them
4. System validates the new time slot (at least 24 hours before original appointment, no conflicts with doctor's schedule)
5. System updates the appointment and notifies both patient and doctor of the change
6. Patient receives confirmation of the rescheduled appointment

**Problem Solved:** Patients can manage their healthcare appointments flexibly without needing to cancel and rebook, maintaining continuity of care.

### Doctor Reschedules on Behalf of Patient

As a **doctor or medical staff**, I want to **reschedule a patient's appointment**, so that **I can accommodate urgent cases, handle scheduling conflicts, or respond to patient requests**.

**Workflow:**
1. Doctor or staff views patient's appointment
2. Doctor initiates reschedule with new time slot
3. System validates the change and checks for conflicts
4. Optional: Doctor provides a reason for the reschedule
5. System updates appointment and notifies patient
6. Audit trail records who made the change and when

**Problem Solved:** Healthcare providers have flexibility to optimize their schedules and respond to changing medical priorities.

### Administrator Handles Scheduling Conflicts

As an **administrator**, I want to **reschedule appointments when doctors are unavailable**, so that **I can manage unforeseen circumstances like doctor illness, emergency surgeries, or facility issues**.

**Workflow:**
1. Administrator identifies appointments that need rescheduling
2. Administrator proposes new time slots
3. System prevents conflicts and validates business rules
4. Patients are notified with reason for reschedule
5. Audit trail maintains accountability

**Problem Solved:** Medical facilities can handle operational disruptions while maintaining patient care continuity.

## Spec Scope

1. **Appointment Time Modification** - Update start and end times for existing appointments with validation of business rules (minimum duration, maximum duration, advance notice requirements)

2. **Conflict Detection** - Check for overlapping appointments in doctor's schedule, excluding the current appointment being rescheduled, and prevent double-booking

3. **24-Hour Reschedule Window** - Enforce business rule that appointments cannot be rescheduled within 24 hours of the original start time to ensure adequate notice for both parties

4. **Audit Trail via Notes** - Append reschedule reasons to appointment notes field to maintain history of changes, supporting accountability and patient communication

5. **Domain Events** - Raise AppointmentRescheduledEvent containing old and new times for triggering notifications to patient and doctor

6. **Concurrency Control** - Support optimistic concurrency via RowVersion to handle simultaneous reschedule attempts and prevent lost updates

## Out of Scope

- **Authentication and Authorization** - Role-based access control (Patient can only reschedule own appointments) will be implemented in Phase 2
- **Notification Implementation** - Email/SMS notifications to patients and doctors will be handled by event handlers in Phase 2
- **Reschedule History Tracking** - Dedicated audit log table for tracking all changes (will use notes field for MVP)
- **Reschedule Limits** - Business rules limiting how many times an appointment can be rescheduled
- **Cancellation Penalties** - Late reschedule fees or penalties for frequent rescheduling
- **Calendar Integration** - Syncing with external calendars (Google Calendar, Outlook)
- **Bulk Rescheduling** - Administrative tools for rescheduling multiple appointments at once
- **Automated Rescheduling** - AI-powered suggestions for alternative time slots

## Expected Deliverable

1. **API Endpoint Operational** - POST `/api/healthcare/appointments/{appointmentId}/reschedule` accepts requests and returns 200 OK with old and new times, or appropriate error codes (404, 409, 422) with descriptive error messages

2. **Business Rules Enforced** - System prevents rescheduling within 24 hours of appointment (422 Unprocessable Entity), detects doctor schedule conflicts (409 Conflict), validates time window constraints (400 Bad Request for duration < 10 min or > 8 hours)

3. **Data Integrity Maintained** - Appointment entity correctly updates StartUtc, EndUtc, Status (to Rescheduled), and Notes fields; domain event AppointmentRescheduledEvent is raised and can be verified in logs; concurrency conflicts handled gracefully via RowVersion

4. **Comprehensive Test Coverage** - Unit tests for domain model Reschedule() method, validator rules, and handler logic; integration tests covering happy path, 24-hour rule, conflict detection, and concurrency scenarios; HTTP request file with manual test examples