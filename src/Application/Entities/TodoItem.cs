using VerticalSliceArchitecture.Application.Common;

namespace VerticalSliceArchitecture.Application.Entities;

public class TodoItem : AuditableEntity, IHasDomainEvent
{
    public int Id { get; set; }

    public int ListId { get; set; }

    public string? Title { get; set; }

    public string? Note { get; set; }

    public PriorityLevel Priority { get; set; }

    public DateTime? Reminder { get; set; }

    private bool _done;
    public bool Done
    {
        get => _done;
        set
        {
            if (value == true && _done == false)
            {
                DomainEvents.Add(new TodoItemCompletedEvent(this));
            }

            _done = value;
        }
    }

    public TodoList List { get; set; } = null!;

    public List<DomainEvent> DomainEvents { get; set; } = new List<DomainEvent>();
}

public class TodoItemCompletedEvent : DomainEvent
{
    public TodoItemCompletedEvent(TodoItem item)
    {
        Item = item;
    }

    public TodoItem Item { get; }
}

public enum PriorityLevel
{
    None = 0,
    Low = 1,
    Medium = 2,
    High = 3
}
