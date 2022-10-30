namespace Domain.Models;

/// <summary>
/// Message model.
/// </summary>
public record Message
{
    private int priorityLevel;

    public string TextMessage { get; init; }

    /// <summary>
    /// Priority level from 0 to 9.
    /// </summary>
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
