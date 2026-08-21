using DeepSeekPet.Core.Snap;

namespace DeepSeekPet.Core.Character;

public enum PetMood
{
    Sleepy,
    Loading,
    Happy,
    Worry,
    Sad,
    Confused
}

public interface ICharacterView
{
    void SetMood(PetMood mood);

    /// <summary>
    /// Screen coordinates in device-independent pixels, or null to reset.
    /// Live2D can use this for head/eye tracking; the sprite view offsets pupils.
    /// </summary>
    void SetLookAt(double? screenX, double? screenY);

    void SetPeek(bool peek, DockEdge? edge = null);

    void SetFlip(bool horizontal, bool vertical);
}
