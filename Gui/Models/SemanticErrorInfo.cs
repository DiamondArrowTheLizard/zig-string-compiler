namespace Gui.Models;

public class SemanticErrorInfo
{
    public string Location { get; }
    public string Description { get; }

    public SemanticErrorInfo(string location, string description)
    {
        Location = location;
        Description = description;
    }
}