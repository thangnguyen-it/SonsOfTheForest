namespace SonsOfTheForest.Gameplay.Interaction
{
    public interface IInteractable
    {
        InteractionPrompt GetPrompt(in InteractionContext context);

        bool CanInteract(in InteractionContext context);

        InteractionResult Interact(in InteractionContext context);
    }
}
