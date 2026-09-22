namespace Game1.Gameplay
{
    /// <summary>Target that can be used by the local first-person player.</summary>
    public interface IInteractable
    {
        string PromptKey { get; }
        bool CanInteract(LocalPlayerController player);
        void Interact(LocalPlayerController player);
    }
}
