using VerticalSliceArchitecture.Domain.Common;

namespace VerticalSliceArchitecture.Application.Common.Interfaces;

public interface IDomainEventService
{
    Task Publish(DomainEvent domainEvent);
}
