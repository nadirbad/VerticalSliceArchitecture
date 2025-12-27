# Healthcare Domain

## Overview

This template uses a **healthcare clinic** domain - a small medical practice where patients book appointments with doctors and receive prescriptions.

We chose healthcare over simpler domains (Todo lists, blogs) because it provides:

- Realistic business rules that can't be hand-waved away
- Time-sensitive operations with real consequences
- Multi-party workflows requiring approvals
- Audit and compliance requirements

## Domain Story

> A **patient** calls the clinic to **book an appointment** with their **doctor**. The system checks for scheduling conflicts - the doctor cannot be double-booked. If the patient needs to change the time, they can **reschedule** but only with 24-hour notice.
>
> During the visit, the doctor may **issue a prescription** for medication. The prescription has an expiration date and a maximum number of refills. When the patient needs more medication, they **request a refill**. A doctor must **approve or deny** the request.

## Key Requirements

From the domain story, we extracted these requirements:

| # | Requirement | Business Rule |
|---|-------------|---------------|
| 1 | Book appointment | Doctor cannot have overlapping appointments |
| 2 | Book appointment | Minimum 15 minutes advance booking |
| 3 | Book appointment | Duration: 10 min - 8 hours |
| 4 | Reschedule appointment | Requires 24-hour advance notice |
| 5 | Reschedule appointment | New time cannot conflict with doctor's schedule |
| 6 | Cancel appointment | Cannot cancel completed appointments |
| 7 | Complete appointment | Cannot complete cancelled appointments |
| 8 | Issue prescription | Must specify medication, dosage, directions |
| 9 | Issue prescription | Expiration = issue date + validity period |
| 10 | Issue prescription | Maximum 12 refills allowed |
| 11 | Request refill | Cannot refill expired prescriptions |
| 12 | Request refill | Cannot exceed maximum refill count |
| 13 | Approve/Deny refill | Only doctors can approve refills |

---

## Domain Discovery with EventStorming

We used [EventStorming](https://www.eventstorming.com/) to discover the domain. EventStorming is a collaborative workshop where you explore business processes by identifying **domain events** (things that happened) and working backwards to find **commands** (actions that trigger events) and **aggregates** (consistency boundaries).

> For the complete workshop results including Big Picture and Design Level EventStorming, see **[event-storming.md](event-storming.md)**.

### Domain Events Summary

| Event | Aggregate | Status |
|-------|-----------|--------|
| `AppointmentBooked` | Appointment | ✅ Implemented |
| `AppointmentRescheduled` | Appointment | ✅ Implemented |
| `AppointmentCancelled` | Appointment | ✅ Implemented |
| `AppointmentCompleted` | Appointment | ✅ Implemented |
| `PrescriptionIssued` | Prescription | ✅ Implemented |
| `RefillRequested` | RefillRequest | 📋 Planned |
| `RefillApproved` | RefillRequest | 📋 Planned |
| `RefillDenied` | RefillRequest | 📋 Planned |

---

## Identifying Subdomains

From EventStorming, we identified natural groupings by looking at:

1. **Which events cluster together?**
2. **Which actors own which processes?**
3. **Where are the consistency boundaries?**

### Subdomain Map

```
┌─────────────────────────────────────────────────────────────────┐
│                     HEALTHCARE DOMAIN                           │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  ┌─────────────────────┐      ┌─────────────────────┐          │
│  │   SCHEDULING        │      │   MEDICATIONS       │          │
│  │   ───────────       │      │   ───────────       │          │
│  │   • Book            │      │   • Issue Rx        │          │
│  │   • Reschedule      │      │   • Request refill  │          │
│  │   • Cancel          │      │   • Approve/Deny    │          │
│  │   • Complete        │      │                     │          │
│  │                     │      │                     │          │
│  │   [CORE]            │      │   [CORE]            │          │
│  └─────────────────────┘      └─────────────────────┘          │
│                                                                 │
│  ┌─────────────────────┐      ┌─────────────────────┐          │
│  │   PATIENT REGISTRY  │      │   PROVIDER DIRECTORY│          │
│  │   ────────────────  │      │   ─────────────────│          │
│  │   Patient identity  │      │   Doctor profiles   │          │
│  │   Contact info      │      │   Specialties       │          │
│  │                     │      │                     │          │
│  │   [SUPPORTING]      │      │   [SUPPORTING]      │          │
│  └─────────────────────┘      └─────────────────────┘          │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

### Why These Groupings?

| Subdomain | Events Owned | Rationale |
|-----------|--------------|-----------|
| **Scheduling** | Booked, Rescheduled, Cancelled, Completed | All about appointment lifecycle and time management |
| **Medications** | Issued, RefillRequested, Approved, Denied | All about prescription lifecycle and refill workflow |
| **Patient Registry** | (none in MVP) | Provides patient identity to other subdomains |
| **Provider Directory** | (none in MVP) | Provides doctor identity to other subdomains |

### Subdomain Classification

| Subdomain | Type | Why? |
|-----------|------|------|
| **Scheduling** | Core | Primary business value - patients come to book appointments |
| **Medications** | Core | Primary business value - prescriptions are key outcome |
| **Patient Registry** | Supporting | Necessary but not differentiating - any clinic has patients |
| **Provider Directory** | Supporting | Necessary but not differentiating - any clinic has doctors |

> **Naming Convention**: We name subdomains by capability (Scheduling, Medications) not by entity (Appointment, Prescription). This keeps focus on business processes, not data structures.

---

## Bounded Contexts

Each subdomain maps to its own bounded context. This is a pragmatic choice for this template.

### Why Separate Contexts?

Consider the term **"Schedule"**:

- In **Scheduling**: A specific time slot for a patient-doctor meeting
- In **Provider Directory**: A doctor's weekly availability pattern

Sharing a single "Schedule" model leads to conditional logic and tight coupling.

### Context Map

```
                    ┌─────────────────────┐
                    │     Scheduling      │
                    │  ─────────────────  │
                    │  Appointment        │
                    │  lifecycle          │
                    └──────────┬──────────┘
                               │
              ┌────────────────┼────────────────┐
              │ uses           │                │ uses
              ▼                │                ▼
┌─────────────────────┐        │     ┌─────────────────────┐
│  Patient Registry   │        │     │ Provider Directory  │
│  ─────────────────  │        │     │ ─────────────────   │
│  Patient identity   │        │     │ Doctor identity     │
└─────────────────────┘        │     └─────────────────────┘
              │                │                │
              │                │                │
              │                ▼                │
              │     ┌─────────────────────┐     │
              └────▶│    Medications      │◀────┘
                    │  ─────────────────  │
                    │  Prescription &     │
                    │  refill workflows   │
                    └─────────────────────┘
```

---

## Code Organization

Bounded contexts map to feature folders:

```text
src/Application/
├── Features/Healthcare/
│   ├── Appointments/           # Scheduling context
│   │   ├── BookAppointment.cs
│   │   ├── RescheduleAppointment.cs
│   │   ├── CancelAppointment.cs
│   │   ├── CompleteAppointment.cs
│   │   └── EventHandlers/
│   │
│   ├── Prescriptions/          # Medications context
│   │   └── IssuePrescription.cs
│   │
│   └── HealthcareEndpoints.cs
│
└── Domain/Healthcare/
    ├── Appointment.cs          # Aggregate root
    ├── Prescription.cs         # Aggregate root
    ├── Patient.cs              # Entity
    ├── Doctor.cs               # Entity
    └── Events/
        ├── AppointmentBookedEvent.cs
        └── PrescriptionIssuedEvent.cs
```

---

## Further Reading

- [EventStorming](https://www.eventstorming.com/) - Alberto Brandolini
- [Vertical Slice Architecture](https://jimmybogard.com/vertical-slice-architecture/) - Jimmy Bogard
- [Domain-Driven Design Distilled](https://www.oreilly.com/library/view/domain-driven-design-distilled/9780134434964/) - Vaughn Vernon
- [Bounded Context Canvas](https://github.com/ddd-crew/bounded-context-canvas) - DDD Crew
