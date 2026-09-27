namespace Sotype.Domain;

/// <summary>
/// The correctness of one typed character relative to its target word, at the point
/// it was typed. Only <see cref="TypingSession"/> ever transitions a character between states.
/// </summary>
public enum CharacterState
{
    /// <summary>Not yet typed.</summary>
    Pending,

    /// <summary>Typed and matches the target character at this position.</summary>
    Correct,

    /// <summary>Typed and does not match the target character at this position.</summary>
    Incorrect,

    /// <summary>Typed beyond the end of the target word.</summary>
    Extra
}
