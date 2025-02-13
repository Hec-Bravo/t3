using System;
using T3.Core;
using T3.Core.DataTypes;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;
using T3.Core.Resource;
using T3.Core.Utils;

namespace T3.Operators.Types.Id_7953f704_ebee_498b_8bdd_a2c201dfe278
{
    public class SetIntVariable : Instance<SetIntVariable>
    {
        [Output(Guid = "7d806685-4678-4dfc-9dbc-36fdfa0c7a59")]
        public readonly Slot<Command> Output = new();

        public SetIntVariable()
        {
            Output.UpdateAction = Update;
        }

        // private T GetEnumFromInput<T>(InputSlot<int> slot, EvaluationContext context) where T:Enum 
        // {
        //     return (T)slot.GetValue(context).Clamp(0, Enum.GetValues(typeof(T)).Length - 1);
        // }
        
        private int ClampForEnum<T>(int i) where T:Enum 
        {
            return i.Clamp(0, Enum.GetValues(typeof(T)).Length - 1);
        }
        
        private void Update(EvaluationContext context)
        {
            var name = VariableName.GetValue(context);
            var newValue = Value.GetValue(context);

            var logLevel = LogLevel.GetValue(context).ClampForEnum<LogLevels>();
            
            if (string.IsNullOrEmpty(name))
            {
                if(logLevel >= (int)LogLevels.Warnings) 
                    Log.Warning($"Can't set variable with invalid name {name}", this);
                
                return;
            }
            
            if (!SubGraph.IsConnected)
            {
                if(logLevel >= (int)LogLevels.Warnings) 
                    Log.Warning($"No subgraph defined for variable {name}", this);
                
                context.IntVariables[name] = newValue;
            }
            else
            {
                var hadPreviousValue = context.IntVariables.TryGetValue(name, out var previous);
                context.IntVariables[name] = newValue;
                
                SubGraph.GetValue(context);
                
                if (hadPreviousValue)
                {
                    if(logLevel >= (int)LogLevels.Changes) 
                        Log.Debug($"Changing {name} from {previous} -> {newValue}", this);
                    
                    context.IntVariables[name] = previous;
                }
                else
                {
                    if(logLevel >= (int)LogLevels.AllUpdates) 
                        Log.Debug($"Setting {name} to {newValue}", this);
                    
                    context.IntVariables.Remove(name);
                }
            }
        }

        
        [Input(Guid = "662b8a63-58db-4c9e-b53a-7ece1f118e12")]
        public readonly InputSlot<Command> SubGraph = new();
        
        [Input(Guid = "bfd87742-aaf5-4fa8-b714-fd275de1c60d")]
        public readonly InputSlot<string> VariableName = new();
        
        [Input(Guid = "72DD0C80-8E95-474B-9AA5-D8292D0FF0DD")]
        public readonly InputSlot<int> Value = new();

        [Input(Guid = "4AB2A742-7F3F-4D96-B67E-73E14B4A8F47", MappedType = typeof(LogLevels))]
        public readonly InputSlot<int> LogLevel = new();


        private enum LogLevels
        {
            None,
            Warnings,
            Changes,
            AllUpdates,
        }

        
    }
}

