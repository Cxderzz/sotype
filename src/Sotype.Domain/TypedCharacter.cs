namespace Sotype.Domain;

/// <summary>
/// A single character as actually typed by the user, paired with the correctness it
/// was judged to have at the moment it was typed. Immutable value object.
/// </summary>
public readonly record struct TypedCharacter(char Character, CharacterState State);
