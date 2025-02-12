using T3.Core;
using T3.Core.DataTypes;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;
using T3.Core.Resource;

namespace T3.Operators.Types.Id_8b285708_3f20_4957_9eb2_bb40e0d320ee
{
    public class IterateList : Instance<IterateList>
    {
        [Output(Guid = "6FD6E8AB-7F90-4693-BE5C-391DE9027362")]
        public readonly Slot<Command> Result = new Slot<Command>();

        public IterateList()
        {
            Result.UpdateAction = Update;
        }

        private void Update(EvaluationContext context)
        {
            if (context.IteratedList != null)
            {
                Log.Warning("Nesting multiple [IterateList] Operators is not possible yet", this);
                return;
            }
            
            var list = List.GetValue(context);
            if (list == null || list.NumElements == 0)
                return;

            context.IteratedList = list;

            foreach (var initCommand in SetupCommands.CollectedInputs)
            {
                initCommand.Invalidate();
                initCommand.GetValue(context);
            }
            
            for (int index = 0; index < list.NumElements; index++)
            {
                context.IteratedListIndex = index;
                context.FloatVariables["iterator"] = index;
                DirtyFlag.InvalidationRefFrame++;
                foreach (var c in IterateCommands.CollectedInputs)
                {
                    c.Invalidate();
                    c.GetValue(context);
                    
                }
            }

            context.IteratedList = null;
        }

        [Input(Guid = "A5E1551B-697E-4316-A95E-315A39B69C76")]
        public readonly InputSlot<StructuredList> List = new InputSlot<StructuredList>();

        [Input(Guid = "452B30A4-D0B8-43CC-B5D2-B6D338CA4DEB")]
        public readonly MultiInputSlot<Command> SetupCommands = new MultiInputSlot<Command>();

        [Input(Guid = "1D09C5E2-19D1-4622-A06D-4FFD3124343C")]
        public readonly MultiInputSlot<Command> IterateCommands = new MultiInputSlot<Command>();
    }
}