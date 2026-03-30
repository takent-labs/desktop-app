using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using takent_desktop_app.Models;
using takent_desktop_app.Services;
using Windows.Foundation;
using Windows.Foundation.Collections;

namespace takent_desktop_app
{
    public sealed partial class MainWindow : Window
    {
        private readonly PostService _post;
        private readonly ObservableCollection<Post> _posts = new();
        private readonly AuthService _auth = new AuthService();
        private bool _isLoginActive = true;

        public MainWindow()
        {
            this.InitializeComponent();
            PostsList.ItemsSource = _posts;
            PostContentBox.TextChanged += (s, e) =>
            {
                var len = PostContentBox.Text.Length;
                CharCounter.Text = $"{len} / 500";
                CharCounter.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(
                    len > 500
                        ? Microsoft.UI.ColorHelper.FromArgb(0xFF, 0xB5, 0x5A, 0x4A)
                        : Microsoft.UI.ColorHelper.FromArgb(0xFF, 0xB8, 0xB3, 0xB0));
            };
            CheckExistingSession();
            _post = new PostService(_auth);
        }

        private void CheckExistingSession()
        {
            var token = _auth.GetStoredToken();
            if (token != null)
                ShowHome();
        }

        private void LoginTabBtn_Click(object sender, RoutedEventArgs e)
        {
            if (_isLoginActive) return;
            _isLoginActive = true;

            LoginForm.Visibility = Visibility.Visible;
            RegisterForm.Visibility = Visibility.Collapsed;
            AuthSubtitle.Text = "Inicia sesión en tu cuenta";

            LoginTabIndicator.Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(
                Microsoft.UI.ColorHelper.FromArgb(0xFF, 0x29, 0x25, 0x24));
            LoginTabBtn.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(
                Microsoft.UI.ColorHelper.FromArgb(0xFF, 0xFF, 0xF7, 0xED));

            RegisterTabIndicator.Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(
                Microsoft.UI.Colors.Transparent);
            RegisterTabBtn.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(
                Microsoft.UI.ColorHelper.FromArgb(0xFF, 0x6C, 0x67, 0x65));

            HideError();
        }

        private void RegisterTabBtn_Click(object sender, RoutedEventArgs e)
        {
            if (!_isLoginActive) return;
            _isLoginActive = false;

            LoginForm.Visibility = Visibility.Collapsed;
            RegisterForm.Visibility = Visibility.Visible;
            AuthSubtitle.Text = "Crea tu cuenta nueva";

            RegisterTabIndicator.Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(
                Microsoft.UI.ColorHelper.FromArgb(0xFF, 0x29, 0x25, 0x24));
            RegisterTabBtn.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(
                Microsoft.UI.ColorHelper.FromArgb(0xFF, 0xFF, 0xF7, 0xED));

            LoginTabIndicator.Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(
                Microsoft.UI.Colors.Transparent);
            LoginTabBtn.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(
                Microsoft.UI.ColorHelper.FromArgb(0xFF, 0x6C, 0x67, 0x65));

            HideError();
        }

        private async void LoginBtn_Click(object sender, RoutedEventArgs e)
        {
            var email = LoginEmailBox.Text.Trim();
            var password = LoginPasswordBox.Password;

            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                ShowError("Por favor, completa el email y la contraseña.");
                return;
            }

            SetLoading(true);
            var result = await _auth.LoginAsync(email, password);
            SetLoading(false);

            if (result.Success)
                ShowHome();
            else
                ShowError(result.ErrorMessage ?? "Error desconocido.");
        }

        private async void RegisterBtn_Click(object sender, RoutedEventArgs e)
        {
            var username = RegisterUsernameBox.Text.Trim();
            var email = RegisterEmailBox.Text.Trim();
            var password = RegisterPasswordBox.Password;

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                ShowError("Por favor, completa todos los campos.");
                return;
            }

            SetLoading(true);
            var result = await _auth.RegisterAsync(username, email, password);
            SetLoading(false);

            if (result.Success)
                ShowHome();
            else
                ShowError(result.ErrorMessage ?? "Error desconocido.");
        }

        private void LogoutBtn_Click(object sender, RoutedEventArgs e)
        {
            _auth.Logout();
            ShowAuth();
        }

        private void Input_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Enter)
                LoginBtn_Click(sender, new RoutedEventArgs());
        }

        private async void ShowHome()
        {
            AuthPanel.Visibility = Visibility.Collapsed;
            HomePanel.Visibility = Visibility.Visible;
            await LoadPostsAsync();
        }

        private void ShowAuth()
        {
            LoginEmailBox.Text = "";
            LoginPasswordBox.Password = "";
            RegisterUsernameBox.Text = "";
            RegisterEmailBox.Text = "";
            RegisterPasswordBox.Password = "";

            HideError();
            HomePanel.Visibility = Visibility.Collapsed;
            AuthPanel.Visibility = Visibility.Visible;
        }

        private void SetLoading(bool active)
        {
            LoadingPanel.Visibility = active ? Visibility.Visible : Visibility.Collapsed;
            HideError();

            LoginBtn.IsEnabled = !active;
            RegisterBtn.IsEnabled = !active;
            LoginEmailBox.IsEnabled = !active;
            LoginPasswordBox.IsEnabled = !active;
            RegisterUsernameBox.IsEnabled = !active;
            RegisterEmailBox.IsEnabled = !active;
            RegisterPasswordBox.IsEnabled = !active;
            LoginTabBtn.IsEnabled = !active;
            RegisterTabBtn.IsEnabled = !active;
        }

        private void ShowError(string message)
        {
            ErrorMsg.Text = message;
            ErrorBorder.Visibility = Visibility.Visible;
            LoadingPanel.Visibility = Visibility.Collapsed;
        }

        private void HideError() =>
            ErrorBorder.Visibility = Visibility.Collapsed;

        private async Task LoadPostsAsync()
        {
            FeedLoadingPanel.Visibility = Visibility.Visible;
            FeedErrorBorder.Visibility = Visibility.Collapsed;
            EmptyFeedPanel.Visibility = Visibility.Collapsed;
            _posts.Clear();

            var result = await _post.GetPostsAsync();

            FeedLoadingPanel.Visibility = Visibility.Collapsed;

            if (!result.Success)
            {
                FeedErrorMsg.Text = result.ErrorMessage ?? "Error al cargar posts.";
                FeedErrorBorder.Visibility = Visibility.Visible;
                return;
            }

            if (result.Posts.Count == 0)
            {
                EmptyFeedPanel.Visibility = Visibility.Visible;
                return;
            }

            foreach (var post in result.Posts)
                _posts.Add(post);
        }

        private async void CreatePostBtn_Click(object sender, RoutedEventArgs e)
        {
            var content = PostContentBox.Text.Trim();

            if (string.IsNullOrEmpty(content))
            {
                PostErrorMsg.Text = "El post no puede estar vacío.";
                PostErrorBorder.Visibility = Visibility.Visible;
                return;
            }

            if (content.Length > 500)
            {
                PostErrorMsg.Text = "El post no puede superar los 500 caracteres.";
                PostErrorBorder.Visibility = Visibility.Visible;
                return;
            }

            PostErrorBorder.Visibility = Visibility.Collapsed;
            PostLoadingPanel.Visibility = Visibility.Visible;
            CreatePostBtn.IsEnabled = false;
            PostContentBox.IsEnabled = false;

            var result = await _post.CreatePostAsync(content);

            PostLoadingPanel.Visibility = Visibility.Collapsed;
            CreatePostBtn.IsEnabled = true;
            PostContentBox.IsEnabled = true;

            if (!result.Success)
            {
                PostErrorMsg.Text = result.ErrorMessage ?? "Error al publicar.";
                PostErrorBorder.Visibility = Visibility.Visible;
                return;
            }

            PostContentBox.Text = "";
            CharCounter.Text = "0 / 500";

            if (result.CreatedPost != null)
            {
                _posts.Insert(0, result.CreatedPost);
                EmptyFeedPanel.Visibility = Visibility.Collapsed;
            }
            else
            {
                await LoadPostsAsync();
            }
        }
    }
}
