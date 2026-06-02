using UraniumUI.Material.Resources;

namespace PesaLens.App
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();

            MainPage = new AppShell();
        }
    }
}
