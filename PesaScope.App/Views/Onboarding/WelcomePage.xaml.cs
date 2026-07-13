namespace PesaScope.App.Views.Onboarding;

public partial class WelcomePage : UraniumUI.Pages.UraniumContentPage
{
    public List<OnboardingPageModel> Pages { get; } =
    [
        new()
        {
            Title                  = "Know Your Spending Habits",
            Description            = "Hundreds of M-Pesa transactions can make it difficult to understand where your money goes. PesaScope brings everything together in one clear view.",
            ImageSource = "onboarding_1.png"
        },
        new()
        {
            Title                  = "Your Transactions, Organized",
            Description            = "Food, transport, shopping, bills, and more. PesaScope automatically categorizes your expenses so you can quickly understand your spending patterns.",
            ImageSource = "onboarding_2.png"
        },
        new()
        {
            Title                  = "Take Control of Your Money",
            Description            = "Create budgets, monitor progress, and make informed financial decisions with insights built from your everyday M-Pesa activity.",
            ImageSource = "onboarding_3.png"
        }
    ];

    private readonly IServiceProvider _services;

    public WelcomePage(IServiceProvider services)
    {
        BindingContext = this;
        InitializeComponent();
        _services = services;
    }

    // ── Events ────────────────────────────────────────────────────────────────

    private void OnCarouselPositionChanged(object? sender, PositionChangedEventArgs e)
    {
        PageCounterLabel.Text = $"{e.CurrentPosition + 1}/{Pages.Count}";

        NextButton.Text = e.CurrentPosition == Pages.Count - 1
            ? "Get Started"
            : "Next";

        // Hide Skip on the last page — Get Started serves the same purpose
        // SkipButton.IsVisible = e.CurrentPosition < Pages.Count - 1;
    }

    private void OnNextClicked(object? sender, EventArgs e)
    {
        var currentPosition = OnboardingCarousel.Position;

        if (currentPosition < Pages.Count - 1)
        {
            // Advance to the next card
            OnboardingCarousel.Position = currentPosition + 1;
        }
        else
        {
            // Last page — move to the permission page
            NavigateToPermissionPage();
        }
    }

    private void OnSkipClicked(object? sender, EventArgs e) =>
        NavigateToPermissionPage();

    private void NavigateToPermissionPage()
    {
        // Replace the window root so the user can't press back into onboarding
        if (Application.Current?.Windows.FirstOrDefault() is Window window)
            window.Page = _services.GetRequiredService<PermissionPage>();
    }
}

// ── Model ─────────────────────────────────────────────────────────────────────

public class OnboardingPageModel
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ImageSource { get; set; } = string.Empty;  
}