using CommunityToolkit.Mvvm.ComponentModel;
using Gui.Models;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Gui.ViewModels;

public partial class RegexSearchViewModel : ObservableObject
{
    public class PatternPreset
    {
        public string Name { get; set; } = string.Empty;
        public string RegexPattern { get; set; } = string.Empty;
    }

    [ObservableProperty]
    private List<PatternPreset> _presets;

    [ObservableProperty]
    private PatternPreset? _selectedPreset;

    [ObservableProperty]
    private ObservableCollection<RegexMatchInfo> _matches = new();

    [ObservableProperty]
    private string _summaryText = "Совпадений не найдено";

    public RegexSearchViewModel()
    {
        _presets = new List<PatternPreset>
        {
            new() { Name = "Канадский почтовый индекс (A1A 1A1)", RegexPattern = @"\b[A-Z]\d[A-Z]\s?\d[A-Z]\d\b" },
            new() { Name = "Шестнадцатеричный (HEX) цвет", RegexPattern = @"\b#?[0-9A-Fa-f]{6}\b" },
            new() { Name = "Паспорт РФ (Серия и номер)", RegexPattern = @"\b\d{4}\s?\d{6}\b|\b\d{2}\s?\d{2}\s?\d{6}\b" }
        };

        SelectedPreset = _presets[0];
    }
}