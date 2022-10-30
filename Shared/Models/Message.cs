namespace Domain.Models;

public record Message
{
    private int priorityLevel;

    public string TextMessage { get; init; }

    public int PriorityLevel
    {
        get => priorityLevel;
        init => priorityLevel = value switch
        {
            < 0 => 0,
            > 9 => 9,
            _ => value
        };
    }
}
