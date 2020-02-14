using System;
using Content.Server.GameObjects.Components.Metabolism;
using Content.Server.GameObjects.EntitySystems;
using Content.Server.Interfaces;
using Content.Shared.Chemistry;
using Content.Shared.GameObjects.Components.Chemistry;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Chemistry
{
    //Todo: Write description
    /// <summary>
    /// 
    /// </summary>
    [RegisterComponent]
    public class InjectorComponent : SharedInjectorComponent, IAfterAttack, IUse
    {
#pragma warning disable 649
        [Dependency] private readonly IServerNotifyManager _notifyManager;
        [Dependency] private readonly ILocalizationManager _localizationManager;
#pragma warning restore 649

        /// <summary>
        /// Whether or not the injector is able to draw from containers
        /// of if it's a single use device that can only inject.
        /// </summary>
        [ViewVariables]
        private bool _injectOnly;

        /// <summary>
        /// Amount to inject or draw on each usage. If the injector is inject only,
        /// it will attempt to inject it's entire contents upon use.
        /// </summary>
        [ViewVariables]
        private int _transferAmount;

        /// <summary>
        /// The state of the injector. Determines it's attack behavior. If set to draw, attempts to draw
        /// solutions from containers into itself. If set to inject, attempts to inject it's contents into
        /// containers. Containers must have the right SolutionCaps to support injection. For InjectOnly
        /// injectors this should only ever be set to Inject.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        private InjectorToggleMode _toggleState;
        [ViewVariables]
        private SolutionComponent _internalContents;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            serializer.DataField(ref _injectOnly, "injectOnly", true);
            serializer.DataField(ref _transferAmount, "transferAmount", 5);
            serializer.DataField(ref _toggleState, "toggleState", InjectorToggleMode.Draw);
        }

        public override void Initialize()
        {
            base.Initialize();

            Owner.TryGetComponent<SolutionComponent>(out _internalContents);
            _internalContents.Capabilities |= SolutionCaps.Injector;

            if (_injectOnly)
                _toggleState = InjectorToggleMode.Inject;
            else
                _toggleState = InjectorToggleMode.Draw;
        }

        private void Toggle()
        {
            if (_injectOnly)
                return;

            _toggleState = _toggleState switch
            {
                InjectorToggleMode.Inject => InjectorToggleMode.Draw,
                InjectorToggleMode.Draw => InjectorToggleMode.Inject,
                _ => throw new ArgumentOutOfRangeException()
            };

            Dirty();
        }

        void IAfterAttack.AfterAttack(AfterAttackEventArgs eventArgs)
        {
            if (eventArgs.Attacked == null)
                return;
            //Todo: Injection behavior
            //Check if attack entity has solutionComp
            //Check if has right SolutionCaps for injection
            //Inject or draw if space available in injector / target
            var targetEntity = eventArgs.Attacked;
            if (_internalContents == null)
                return;
            if (!_internalContents.Injector) //Todo: Why even have SolutionCaps if they're all gonna be hid behind bools? Network perf?
                return;
            if (targetEntity.TryGetComponent<SolutionComponent>(out var targetSolution) && targetSolution.Injectable)
            {
                if (_toggleState == InjectorToggleMode.Inject)
                    TryInject(targetSolution, eventArgs.User);
                else if (_toggleState == InjectorToggleMode.Draw)
                    TryDraw(targetSolution, eventArgs.User);
            }
            else
            {
                if (!targetEntity.TryGetComponent<BloodstreamComponent>(out var bloodstream))
                    return;
                if (_toggleState == InjectorToggleMode.Inject)
                    TryInjectIntoLiver(bloodstream, eventArgs.User); //Todo: Check liver sol caps
            }
            //Todo: Add injection into LiverComponent/Bloodstream

        }

        //Todo: Make injector base prototype since there will be autoinjectors
        //Todo: Add autoinjectors with basic med chems - maybe add these with separate PR with healing chem effects
        //Todo: Have sprites change based on fullness and inject state
        bool IUse.UseEntity(UseEntityEventArgs eventArgs)
        {
            Toggle();
            //Todo: Maybe have popup saying what state it is
            _notifyManager.PopupMessage(Owner.Transform.GridPosition, eventArgs.User,
                _localizationManager.GetString(_toggleState == InjectorToggleMode.Draw ? "Draw mode" : "Inject mode"));

            return true;
        }

        private void TryInjectIntoLiver(BloodstreamComponent targetBloodstream, IEntity user)
        {
            //Todo: Maybe have popup saying what state it is
            //if(!targetSolution.Injectable)
            //    return;
            if (_internalContents.CurrentVolume == 0)
                return;

            //Get transfer amount. May be smaller than _transferAmount if not enough room
            int realTransferAmount = Math.Min(_transferAmount, targetBloodstream.EmptyVolume);
            if (realTransferAmount <= 0) //Todo: Special message if container is full
            {
                _notifyManager.PopupMessage(Owner.Transform.GridPosition, user,
                    _localizationManager.GetString("Container full."));
                return;
            }

            //Move units from attackSolution to targetSolution
            var removedSolution = _internalContents.SplitSolution(realTransferAmount);
            //var removedSolution = targetSolution.SplitSolution(realTransferAmount);
            if (!targetBloodstream.TryTransferSolution(removedSolution))
                return;

            _notifyManager.PopupMessage(Owner.Transform.GridPosition, user,
                _localizationManager.GetString("Injected {0}u", removedSolution.TotalVolume));
            Dirty();
        }

        private void TryInject(SolutionComponent targetSolution, IEntity user)
        {
            //Todo: Maybe have popup saying what state it is
            //if(!targetSolution.Injectable)
            //    return;
            if (_internalContents.CurrentVolume == 0)
                return;

            //Get transfer amount. May be smaller than _transferAmount if not enough room
            int realTransferAmount = Math.Min(_transferAmount, targetSolution.EmptyVolume);
            if (realTransferAmount <= 0) //Todo: Special message if container is full
            {
                _notifyManager.PopupMessage(Owner.Transform.GridPosition, user,
                    _localizationManager.GetString("Container full."));
                return;
            }

            //Move units from attackSolution to targetSolution
            var removedSolution = _internalContents.SplitSolution(realTransferAmount);
            //var removedSolution = targetSolution.SplitSolution(realTransferAmount);
            if (!targetSolution.TryAddSolution(removedSolution))
                return;

            _notifyManager.PopupMessage(Owner.Transform.GridPosition, user,
                _localizationManager.GetString("Injected {0}u", removedSolution.TotalVolume));
            Dirty();
        }

        private void TryDraw(SolutionComponent targetSolution, IEntity user)
        {
            //Todo: Maybe have popup saying what state it is
            //if(!targetSolution.Injectable) //Todo: Figure out what flags should be used for drawing. Maybe another one
            //    return;
            if (_internalContents.EmptyVolume == 0)
                return;

            //Get transfer amount. May be smaller than _transferAmount if not enough room
            int realTransferAmount = Math.Min(_transferAmount, targetSolution.CurrentVolume);
            if (realTransferAmount <= 0) //Special message if container is full
            {
                _notifyManager.PopupMessage(Owner.Transform.GridPosition, user,
                    _localizationManager.GetString("Container empty")); //Todo: Remove for PR. only for debug
                return;
            }

            //Move units from attackSolution to targetSolution
            var removedSolution = targetSolution.SplitSolution(realTransferAmount);
            if (!_internalContents.TryAddSolution(removedSolution))
                return;

            _notifyManager.PopupMessage(Owner.Transform.GridPosition, user,
                _localizationManager.GetString("Drew {0}u", removedSolution.TotalVolume));
            Dirty();
        }

        public override ComponentState GetComponentState()
        {
            return new InjectorComponentState(_internalContents.CurrentVolume, _internalContents.MaxVolume, _toggleState);
        }
    }
}
