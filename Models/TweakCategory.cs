using System.Collections.ObjectModel;

namespace PCOptimizerApp.Models;

public class TweakCategory
{
    public required string Id { get; init; }
    public required string Label { get; init; }
    public required ObservableCollection<TweakItem> Items { get; init; }
}
