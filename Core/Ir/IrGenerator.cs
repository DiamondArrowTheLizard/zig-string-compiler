using System.Collections.Generic;
using Core.Ast;

namespace Core.Ir;

public class IrGenerator
{
    private int _tempCounter = 0;

    private string NextTemp()
    {
        return $"%t{++_tempCounter}";
    }

    public List<IrInstruction> Generate(IEnumerable<ConstDeclNode> declarations)
    {
        var instructions = new List<IrInstruction>();
        _tempCounter = 0;

        foreach (var decl in declarations)
        {
            if (string.IsNullOrEmpty(decl.Value)) continue;

            string tempValue = NextTemp();
            instructions.Add(new IrInstruction
            {
                Operation = "ASSIGN",
                Argument1 = $"\"{decl.Value}\"",
                Result = tempValue
            });

            string tempLen = NextTemp();
            instructions.Add(new IrInstruction
            {
                Operation = "LEN",
                Argument1 = tempValue,
                Result = tempLen
            });

            string tempAlloc = NextTemp();
            instructions.Add(new IrInstruction
            {
                Operation = "ALLOC",
                Argument1 = decl.Type,
                Argument2 = tempLen,
                Result = tempAlloc
            });

            instructions.Add(new IrInstruction
            {
                Operation = "STORE",
                Argument1 = tempValue,
                Argument2 = tempAlloc
            });

            instructions.Add(new IrInstruction
            {
                Operation = "GLOBAL",
                Argument1 = tempAlloc,
                Result = $"@{decl.Name}"
            });
        }

        return instructions;
    }
}