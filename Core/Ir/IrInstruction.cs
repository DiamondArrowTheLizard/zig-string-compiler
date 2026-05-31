namespace Core.Ir;

public class IrInstruction
{
    public string Operation { get; set; } = string.Empty;
    public string Argument1 { get; set; } = string.Empty;
    public string Argument2 { get; set; } = string.Empty;
    public string Result { get; set; } = string.Empty;

    public IrInstruction Clone()
    {
        return new IrInstruction
        {
            Operation = Operation,
            Argument1 = Argument1,
            Argument2 = Argument2,
            Result = Result
        };
    }

    public override string ToString()
    {
        if (string.IsNullOrEmpty(Result))
        {
            return $"{Operation} {Argument1} {Argument2}".Trim();
        }
        return $"{Result} = {Operation} {Argument1} {Argument2}".Trim();
    }
}