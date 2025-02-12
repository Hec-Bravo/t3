using System.Numerics;
using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_1b69d98a_0b38_4563_aa43_aac5b8395c2b
{
    public class SlidingHistory : Instance<SlidingHistory>
    {
        [Output(Guid = "724ecde4-fd33-4a59-a8df-51cb21e70bd3")]
        public readonly Slot<Texture2D> Output = new Slot<Texture2D>();

        [Input(Guid = "48699e66-a59b-4cbb-b131-171ce9fcade3")]
        public readonly InputSlot<SharpDX.Direct3D11.Texture2D> Texture2d = new InputSlot<SharpDX.Direct3D11.Texture2D>();

        [Input(Guid = "72db9342-9c07-4557-8b67-e3dc6ee55271")]
        public readonly InputSlot<bool> ResetTrigger = new InputSlot<bool>();

        [Input(Guid = "41748add-f957-4a48-b7a5-43ff868bc814")]
        public readonly InputSlot<int> HistoryLength = new InputSlot<int>();

        [Input(Guid = "5d42ff17-a552-4437-877a-a27a369866d7")]
        public readonly InputSlot<bool> IsEnabled = new InputSlot<bool>();

    }
}