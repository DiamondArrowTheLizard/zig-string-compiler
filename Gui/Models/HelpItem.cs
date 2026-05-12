namespace Gui.Models;

public class HelpItem
{
    public string Name { get; set; }
    public string Description { get; set; }
    public string Keys { get; set; }

    public HelpItem(string name, string description, string keys)
    {
        Name = name;
        Description = description;
        Keys = keys;
    }
}