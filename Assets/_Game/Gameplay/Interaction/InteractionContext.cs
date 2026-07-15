using UnityEngine;

namespace SonsOfTheForest.Gameplay.Interaction
{
    public readonly struct InteractionContext
    {
        public InteractionContext(
            GameObject actor,
            Transform actorTransform,
            Camera viewCamera,
            float deltaTime,
            InteractionInputKind inputKind)
        {
            Actor = actor;
            ActorTransform = actorTransform;
            ViewCamera = viewCamera;
            DeltaTime = deltaTime;
            InputKind = inputKind;
        }

        public GameObject Actor { get; }

        public Transform ActorTransform { get; }

        public Camera ViewCamera { get; }

        public float DeltaTime { get; }

        public InteractionInputKind InputKind { get; }
    }
}
