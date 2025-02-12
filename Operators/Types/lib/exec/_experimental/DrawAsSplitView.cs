using T3.Core;
using T3.Core.DataTypes;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;
using T3.Core.Resource;

namespace T3.Operators.Types.Id_f19a9234_cd23_4229_a794_aa9d97ad8027
{
    public class DrawAsSplitView : Instance<DrawAsSplitView>
    {
        [Output(Guid = "65456554-355b-41a3-893e-960d28113f53")]
        public readonly Slot<Command> Output = new Slot<Command>();


        [Input(Guid = "a3929303-170b-496a-b8e0-fc5f604a0ec7")]
        public readonly MultiInputSlot<Command> Commands = new MultiInputSlot<Command>();

        [Input(Guid = "987bda72-6a6b-4216-9ecf-d87b7299553d")]
        public readonly InputSlot<string> Labels = new InputSlot<string>();
    }
}

