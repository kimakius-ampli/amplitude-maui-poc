using AmplitudeMauiPoc.Views;

namespace AmplitudeMauiPoc;

public partial class App : Application
{
    private readonly IServiceProvider _services;

    public App(IServiceProvider services)
    {
        InitializeComponent();
        _services = services;
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        var homePage = _services.GetRequiredService<HomePage>();
        return new Window(new NavigationPage(homePage)
        {
            BarBackgroundColor = Color.FromArgb("#1E61F0"),
            BarTextColor = Colors.White
        });
    }
}
