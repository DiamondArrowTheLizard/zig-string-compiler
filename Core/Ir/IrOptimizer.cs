using System.Collections.Generic;
using System.Linq;

namespace Core.Ir;

public class IrOptimizer
{
    public List<IrInstruction> Optimize(List<IrInstruction> input)
    {
        var optimized = input.Select(i => i.Clone()).ToList();

        optimized = ApplyConstantFolding(optimized);
        optimized = ApplyConstantPropagation(optimized);

        return optimized;
    }

    private List<IrInstruction> ApplyConstantFolding(List<IrInstruction> instructions)
    {
        var stringValues = new Dictionary<string, string>();

        foreach (var inst in instructions)
        {
            if (inst.Operation == "ASSIGN" && inst.Argument1.StartsWith("\""))
            {
                stringValues[inst.Result] = inst.Argument1.Trim('"');
            }
            else if (inst.Operation == "LEN" && stringValues.ContainsKey(inst.Argument1))
            {
                int length = stringValues[inst.Argument1].Length;
                inst.Operation = "CONST";
                inst.Argument1 = length.ToString();
            }
        }

        return instructions;
    }

    private List<IrInstruction> ApplyConstantPropagation(List<IrInstruction> instructions)
    {
        var constants = new Dictionary<string, string>();
        var toRemove = new List<IrInstruction>();

        foreach (var inst in instructions)
        {
            if (inst.Operation == "CONST")
            {
                constants[inst.Result] = inst.Argument1;
                toRemove.Add(inst);
            }
            else
            {
                if (constants.ContainsKey(inst.Argument1))
                {
                    inst.Argument1 = constants[inst.Argument1];
                }
                if (constants.ContainsKey(inst.Argument2))
                {
                    inst.Argument2 = constants[inst.Argument2];
                }
            }
        }

        foreach (var inst in toRemove)
        {
            instructions.Remove(inst);
        }

        return instructions;
    }
}