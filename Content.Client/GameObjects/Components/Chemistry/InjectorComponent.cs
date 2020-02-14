using Content.Client.UserInterface;
using Content.Client.Utility;
using Content.Shared.GameObjects.Components.Chemistry;
using Robust.Shared.Timing;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.ViewVariables;

namespace Content.Client.GameObjects.Components.Chemistry
{
    [RegisterComponent]
    public class InjectorComponent : SharedInjectorComponent, IItemStatus
    {
        [ViewVariables] public int CurrentVolume { get; private set; }
        [ViewVariables] public int TotalVolume { get; private set; }
        [ViewVariables] public InjectorToggleMode CurrentMode { get; private set; }

        [ViewVariables(VVAccess.ReadWrite)] private bool _uiUpdateNeeded;

        public Control MakeControl() => new StatusControl(this);
        public void DestroyControl(Control control) { }

        public override void HandleComponentState(ComponentState curState, ComponentState nextState)
        {
            var cast = (InjectorComponentState)curState;

            CurrentVolume = cast.CurrentVolume;
            TotalVolume = cast.TotalVolume;
            CurrentMode = cast.CurrentMode;
            _uiUpdateNeeded = true;
        }

        private sealed class StatusControl : Control
        {
            private readonly InjectorComponent _parent;
            private readonly RichTextLabel _label;

            public StatusControl(InjectorComponent parent)
            {
                _parent = parent;
                _label = new RichTextLabel { StyleClasses = { NanoStyle.StyleClassItemStatus } };
                AddChild(_label);

                parent._uiUpdateNeeded = true;
            }

            protected override void Update(FrameEventArgs args)
            {
                base.Update(args);

                if (!_parent._uiUpdateNeeded)
                {
                    return;
                }

                _parent._uiUpdateNeeded = false;

                _label.SetMarkup(Loc.GetString("Volume: [color=white]{0}/{1}[/color] | [color=white]{2}[/color]",
                    _parent.CurrentVolume, _parent.TotalVolume, _parent.CurrentMode.ToString()));
            }
        }
    }
}
