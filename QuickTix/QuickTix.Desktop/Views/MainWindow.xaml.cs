using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Wpf.Ui.Controls;
using Wpf.Ui;

namespace QuickTix.Desktop.Views
{
    /// <summary>
    /// Lógica de interacción para MainWindow.xaml
    /// </summary>
    public partial class MainWindow : INavigationWindow
    {

        public MainViewModel ViewModel { get; }
        public MainWindow(MainViewModel viewModel, INavigationService navigationService)
        {
            ViewModel = viewModel;
            DataContext = viewModel;

            InitializeComponent();

            navigationService.SetNavigationControl(RootNavigation);

            SetPaneControl();
        }



        public void SetPaneControl()
        {

            RootNavigation.Navigated += (s, e) =>
            {


                var currentPage = e.Page;

                if (currentPage is LoginView)
                {
                    RootNavigation.IsPaneVisible = false;
                    RootNavigation.OpenPaneLength = 0;
                    RootNavigation.CompactPaneLength = 0;

                }
                else
                {
                    RootNavigation.OpenPaneLength = 180;
                    RootNavigation.CompactPaneLength = double.NaN;
                }
            };
        }

        public INavigationView GetNavigation() => RootNavigation;

        public bool Navigate(Type pageType) => RootNavigation.Navigate(pageType);

        public void ShowWindow() => Show();
        public void CloseWindow() => Close();

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            Application.Current.Shutdown();
        }

        public void SetServiceProvider(IServiceProvider serviceProvider)
        {
            throw new NotImplementedException();
        }

        public void SetPageService(IPageService pageService)
        {

            RootNavigation.SetPageService(pageService);
        }

    }
}
