﻿using System.Collections.Generic;
using Content.Server.GameObjects.Components.Chemistry;
using Content.Server.GameObjects.EntitySystems;
using Content.Shared.Chemistry;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Metabolism
{
    public class LiverComponent : Component
    {
#pragma warning disable 649
        [Dependency] private readonly IPrototypeManager _prototypeManager;
#pragma warning restore 649

        public override string Name => "Liver";

        [ViewVariables(VVAccess.ReadOnly)]
        private SolutionComponent _internalSolution;
        private int _initialMaxVolume;
        //Used to track changes to reagent amounts during metabolism
        private readonly Dictionary<string, int> _reagentDeltas = new Dictionary<string, int>();

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            serializer.DataField(ref _initialMaxVolume, "max_volume", 20);
        }

        public override void Initialize()
        {
            base.Initialize();
            //Doesn't use Owner.AddComponent<>() to avoid cross-contamination (e.g. with blood or whatever they holds other solutions)
            _internalSolution = new SolutionComponent();
            _internalSolution.InitializeFromPrototype();
            _internalSolution.MaxVolume = _initialMaxVolume;
            _internalSolution.Owner = Owner; //Manually set owner to avoid crash when VV'ing this
        }

        public bool TryTransferSolution(Solution solution)
        {
            // TODO: For now no partial transfers. Potentially change by design
            if (solution.TotalVolume + _internalSolution.CurrentVolume > _internalSolution.MaxVolume)
                return false;

            _internalSolution.TryAddSolution(solution, false, true);
            return true;
        }

        /// <summary>
        /// Loops through each reagent in _internalSolution, and calls the IMetabolizable for each of them./>
        /// </summary>
        /// <param name="tickTime">The time since the last metabolism tick in seconds.</param>
        public void Metabolize(float tickTime)
        {
            if (_internalSolution.CurrentVolume == 0)
                return;

            //Run metabolism for each reagent, track quantity changes
            _reagentDeltas.Clear();
            foreach (var reagent in _internalSolution.ReagentList)
            {
                if (!_prototypeManager.TryIndex(reagent.ReagentId, out ReagentPrototype proto))
                    continue;

                foreach (var metabolizable in proto.Metabolism)
                {
                    _reagentDeltas[reagent.ReagentId] = metabolizable.Metabolize(Owner, reagent.ReagentId, tickTime);
                }
            }

            //Apply changes to quantity afterwards. Can't change the reagent quantities while the iterating the
            //list of reagents, because that would invalidate the iterator and throw an exception.
            foreach (var reagentDelta in _reagentDeltas)
            {
                _internalSolution.TryRemoveReagent(reagentDelta.Key, reagentDelta.Value);
            }
        }

        /// <summary>
        /// Triggers metabolism of the reagents inside _internalSolution. Called by <see cref="StomachSystem"/>
        /// </summary>
        /// <param name="tickTime">The time since the last metabolism tick in seconds.</param>
        public void OnUpdate(float tickTime)
        {
            Metabolize(tickTime);
        }
    }
}
