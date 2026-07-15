namespace SonsOfTheForest.Gameplay.Interaction
{
    public interface IFocusableInteractable
    {
        void OnFocus(in InteractionContext context);

        void OnFocusLost(in InteractionContext context);
    }
}
