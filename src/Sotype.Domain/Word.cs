namespace Sotype.Domain;

/// <summary>
/// One word within a <see cref="TypingSession"/>. Entity whose identity is its position in the
/// owning session's word sequence — it never exists or is referenced outside that aggregate.
/// All mutation is <c>internal</c>: only <see cref="TypingSession"/> may change a word's state,
/// which is what keeps the aggregate's invariants (extra-character cap, valid state transitions)
/// impossible to violate from outside.
/// </summary>
public sealed class Word
{
    private readonly List<TypedCharacter> _typed = new();

    internal Word(string target)
    {
        Target = target;
    }

    public string Target { get; }

    public IReadOnlyList<TypedCharacter> Typed => _typed;

    public bool IsCommitted { get; private set; }

    /// <summary>Typing past this many characters is rejected outright.</summary>
    public int MaxLength => Target.Length * 2;

    /// <summary>
    /// True once as many characters have been typed as the target word is long — win regardless
    /// of whether they were all correct. In <see cref="TestMode.Words"/>, reaching this on the
    /// final word ends the session even without a trailing space.
    /// </summary>
    public bool HasReachedTargetLength => _typed.Count >= Target.Length;

    internal bool TryTypeCharacter(char character)
    {
        if (_typed.Count >= MaxLength)
            return false;

        var state = _typed.Count < Target.Length
            ? (Target[_typed.Count] == character ? CharacterState.Correct : CharacterState.Incorrect)
            : CharacterState.Extra;

        _typed.Add(new TypedCharacter(character, state));
        return true;
    }

    internal bool TryRemoveLastCharacter()
    {
        if (_typed.Count == 0)
            return false;

        _typed.RemoveAt(_typed.Count - 1);
        return true;
    }

    internal void Commit() => IsCommitted = true;

    internal void Uncommit() => IsCommitted = false;
}
