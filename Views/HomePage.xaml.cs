using AmplitudeMauiPoc.Models;
using AmplitudeMauiPoc.Services;

namespace AmplitudeMauiPoc.Views;

public partial class HomePage : ContentPage
{
    private readonly IAmplitudeService _amp;
    private readonly List<DemoItem> _items;

    public HomePage(IAmplitudeService amp)
    {
        InitializeComponent();
        _amp = amp;

        _items = new List<DemoItem>
        {
            new()
            {
                ItemCode = "DEMO-1001",
                Name = "Sample Item A",
                Status = "Active",
                Category = "Standard",
                LastUpdated = DateTime.Today,
                Activity = new()
                {
                    new() { Timestamp = DateTime.Today.AddHours(9), Source = "Mobile app", Description = "Edited" },
                    new() { Timestamp = DateTime.Today.AddDays(-1).AddHours(15), Source = "Team workspace", Description = "Shared" },
                    new() { Timestamp = DateTime.Today.AddDays(-1).AddHours(10), Source = "Web app", Description = "Created" },
                }
            },
            new()
            {
                ItemCode = "DEMO-1002",
                Name = "Sample Item B",
                Status = "Pending",
                Category = "Premium",
                LastUpdated = DateTime.Today.AddDays(-1),
                Activity = new()
                {
                    new() { Timestamp = DateTime.Today.AddDays(-1).AddHours(16), Source = "Mobile app", Description = "Submitted for review" },
                    new() { Timestamp = DateTime.Today.AddDays(-3).AddHours(11), Source = "Web app", Description = "Created" },
                }
            },
            new()
            {
                ItemCode = "DEMO-1003",
                Name = "Sample Item C",
                Status = "Archived",
                Category = "Basic",
                LastUpdated = DateTime.Today.AddDays(-2),
                Activity = new()
                {
                    new() { Timestamp = DateTime.Today.AddDays(-2).AddHours(11), Source = "Web app", Description = "Archived" },
                    new() { Timestamp = DateTime.Today.AddDays(-5).AddHours(14), Source = "Mobile app", Description = "Created" },
                }
            }
        };

        ItemsList.ItemsSource = _items;
        SRIndicator.IsVisible = _amp.IsSessionReplayActive;

        _amp.Track("Screen Viewed", new() { { "screen_name", "Home" } });
    }

    private async void OnSearchClicked(object? sender, EventArgs e)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine("[Demo] Search clicked");

            var code = SearchEntry.Text?.Trim().ToUpperInvariant() ?? "";
            if (code.Length == 0)
            {
                await DisplayAlert("Item code", "Enter an item code.", "OK");
                return;
            }

            var match = _items.FirstOrDefault(
                i => i.ItemCode.Equals(code, StringComparison.OrdinalIgnoreCase));

            _amp.Track("Item Searched", new()
            {
                { "query_length", code.Length },
                { "input_method", "manual" },
                { "result_found", match != null }
            });

            if (match != null)
            {
                await Navigation.PushAsync(new ItemDetailPage(_amp, match));
            }
            else
            {
                await DisplayAlert("Not Found", $"No item found for {code}", "OK");
            }
        }
        catch (System.Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Demo] Search failed: {ex}");
            await DisplayAlert("Error", ex.Message, "OK");
        }
    }

    private async void OnItemSelected(object? sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is DemoItem item)
        {
            _amp.Track("Item Tapped", new()
            {
                { "item_category", item.Category },
                { "item_status", item.Status }
            });
            await Navigation.PushAsync(new ItemDetailPage(_amp, item));
            ((CollectionView)sender!).SelectedItem = null;
        }
    }
}
