using Avalonia.Controls;
using Gui.Models;

namespace Gui.Views;

public partial class SaveBeforeCloseDialog : Window
{
    public SaveBeforeCloseDialog()
    {
        InitializeComponent();
        SaveBtn.Click += (s, e) => Close(SaveBeforeCloseResult.Save);
        DontSaveBtn.Click += (s, e) => Close(SaveBeforeCloseResult.Discard);
        CancelBtn.Click += (s, e) => Close(SaveBeforeCloseResult.Cancel);
    }
}