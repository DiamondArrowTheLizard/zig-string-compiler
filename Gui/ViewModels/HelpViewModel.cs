using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
using Gui.Models;

namespace Gui.ViewModels;


public partial class HelpViewModel : ViewModelBase
{
    public string HelpName => "Справочная система компилятора константной строки Zig";

    public List<HelpItem> ToolbarItems { get; } = new()
    {
        new("Создать", "Создание нового файла Zig", "Ctrl+N"),
        new("Открыть", "Загрузить исходный код", "Ctrl+O"),
        new("Сохранить", "Сохранить текущий текст", "Ctrl+S"),
        new("Отмена", "Отменить правку", "Ctrl+Z"),
        new("Повтор", "Вернуть правку", "Ctrl+Y"),
        new("Запустить", "Лексический и синт. анализ", "F5"),
        new("Справка", "Вызов справочного окна", "F1")
    };

    public List<HelpItem> FileMenuItems { get; } = new()
    {
        new("Новый", "Очистить рабочую область", "Ctrl+N"),
        new("Открыть", "Открыть файл через диалог", "Ctrl+O"),
        new("Сохранить", "Записать изменения", "Ctrl+S"),
        new("Сохранить как", "Выбрать путь сохранения", "—"),
        new("Выход", "Закрыть программу", "—")
    };

    public List<HelpItem> EditMenuItems { get; } = new()
    {
        new("Отменить", "Откат действия", "Ctrl+Z"),
        new("Повторить", "Повтор действия", "Ctrl+Y"),
        new("Вырезать", "В буфер с удалением", "Ctrl+X"),
        new("Копировать", "Копировать в буфер", "Ctrl+C"),
        new("Вставить", "Вставить из буфера", "Ctrl+V"),
        new("Удалить", "Удалить выделенное", "Del"),
        new("Выбрать всё", "Выделить весь код", "Ctrl+A")
    };
}