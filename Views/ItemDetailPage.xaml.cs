using AmplitudeMauiPoc.Models;
using AmplitudeMauiPoc.Services;

namespace AmplitudeMauiPoc.Views;

public partial class ItemDetailPage : ContentPage
{
    private const string ScreenName = "Item Detail";

    private readonly IAmplitudeService _amp;
    private readonly DemoItem _item;
    private readonly DateTime _opened;

    public ItemDetailPage(IAmplitudeService amp, DemoItem item)
    {
        InitializeComponent();
        _amp = amp;
        _item = item;
        _opened = DateTime.UtcNow;

        StatusLbl.Text = item.Status;
        NameLbl.Text = $"{item.Name} ({item.ItemCode})";
        CategoryLbl.Text = item.Category;
        UpdatedDate.Text = item.LastUpdated.ToString("dd MMMM yyyy");
        UpdatedDay.Text = item.LastUpdated.ToString("dddd");

        StatusBanner.BackgroundColor = item.Status switch
        {
            "Active" => Color.FromArgb("#2E7D32"),
            "Archived" => Color.FromArgb("#555770"),
            _ => Color.FromArgb("#EF6C00")
        };

        Timeline.ItemsSource = item.Activity;

        _amp.Track("Screen Viewed", new()
        {
            { "screen_name", ScreenName },
            { "item_category", item.Category },
            { "item_status", item.Status },
            { "activity_count", item.Activity.Count }
        });
    }

    private async void OnAddClicked(object? sender, EventArgs e)
    {
        _amp.Track("Button Tapped", new()
        {
            { "button_name", "Add to My List" },
            { "screen_name", ScreenName },
            { "time_on_page_sec", (int)(DateTime.UtcNow - _opened).TotalSeconds }
        });

        await DisplayAlert("Added", $"{_item.Name} was added to your list.", "OK");

        _amp.Track("Item Added", new()
        {
            { "item_category", _item.Category }
        });
    }

    private async void OnOptionsClicked(object? sender, EventArgs e)
    {
        _amp.Track("Button Tapped", new()
        {
            { "button_name", "More Options" },
            { "screen_name", ScreenName },
            { "time_on_page_sec", (int)(DateTime.UtcNow - _opened).TotalSeconds }
        });

        var choice = await DisplayActionSheet(
            "Choose an option", "Cancel", null,
            "Share",
            "Duplicate",
            "Mark as favourite",
            "Archive");

        if (choice != null && choice != "Cancel")
        {
            _amp.Track("Option Selected", new()
            {
                { "option", choice },
                { "item_category", _item.Category }
            });

            await DisplayAlert("Confirmed", $"Selected: {choice.ToLower()}.", "OK");
        }
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _amp.Track("Screen Closed", new()
        {
            { "screen_name", ScreenName },
            { "time_on_page_sec", (int)(DateTime.UtcNow - _opened).TotalSeconds }
        });
        _amp.Flush();
    }
}
