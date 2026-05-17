using System.Collections.Generic;

namespace Core.Semantic;

public class SemanticResult
{
    public bool Success { get; set; }
    public List<SemanticError> Errors { get; set; } = new();
}