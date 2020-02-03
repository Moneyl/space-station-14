using System;
using System.Collections.Generic;
using System.Text;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Shared.GameObjects.Components.Chemistry
{
    public class SharedInjectorComponent : Component
    {
        public override string Name => "Injector";
        public sealed override uint? NetID => ContentNetIDs.REAGENT_INJECTOR;

        [Serializable, NetSerializable]
        protected sealed class InjectorComponentState : ComponentState
        {
            public int CurrentVolume { get; }
            public int TotalVolume { get; }
            public InjectorToggleMode CurrentMode { get; }

            public InjectorComponentState(int currentVolume, int totalVolume, InjectorToggleMode currentMode) : base(ContentNetIDs.REAGENT_INJECTOR)
            {
                CurrentVolume = currentVolume;
                TotalVolume = totalVolume;
                CurrentMode = currentMode;
            }
        }

        public enum InjectorToggleMode
        {
            Inject,
            Draw
        }
    }
}
