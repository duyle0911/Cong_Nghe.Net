using System.Windows.Input;
using QuanLyTaiChinhCaNhan_Nhom06.Commands;
using QuanLyTaiChinhCaNhan_Nhom06.Helpers;
using QuanLyTaiChinhCaNhan_Nhom06.Services;

namespace QuanLyTaiChinhCaNhan_Nhom06.ViewModels
{
    public class RegisterViewModel : ViewModelBase
    {
        private readonly IAuthenticationService _authService;
        private readonly MainViewModel _mainViewModel;
        private readonly IAppearanceService _appearanceService;

        private string _username = string.Empty;
        private string _email = string.Empty;
        private string _fullName = string.Empty;
        private string _password = string.Empty;
        private string _confirmPassword = string.Empty;
        private string _message = string.Empty;

        public RegisterViewModel(IAuthenticationService authService, MainViewModel mainViewModel, IAppearanceService appearanceService)
        {
            _authService = authService;
            _mainViewModel = mainViewModel;
            _appearanceService = appearanceService;

            RegisterCommand = new AsyncRelayCommand(_ => RegisterAsync());
            ShowLoginCommand = new RelayCommand(_ => _mainViewModel.ShowLogin());
        }

        public string Username
        {
            get => _username;
            set => SetProperty(ref _username, value);
        }

        public string Email
        {
            get => _email;
            set => SetProperty(ref _email, value);
        }

        public string FullName
        {
            get => _fullName;
            set => SetProperty(ref _fullName, value);
        }

        public string Password
        {
            get => _password;
            set => SetProperty(ref _password, value);
        }

        public string ConfirmPassword
        {
            get => _confirmPassword;
            set => SetProperty(ref _confirmPassword, value);
        }

        public string Message
        {
            get => _message;
            set => SetProperty(ref _message, value);
        }

        public ICommand RegisterCommand { get; }
        public ICommand ShowLoginCommand { get; }

        private async Task RegisterAsync()
        {
            if (!Validator.Required(Username) || Username.Any(char.IsWhiteSpace))
            {
                Message = _appearanceService.T("InvalidUsernameMessage");
                return;
            }

            if (!Validator.Email(Email))
            {
                Message = _appearanceService.T("InvalidEmailMessage");
                return;
            }

            if (Password.Length < 6)
            {
                Message = _appearanceService.T("PasswordTooShortMessage");
                return;
            }

            if (Password != ConfirmPassword)
            {
                Message = _appearanceService.T("PasswordMismatchMessage");
                return;
            }

            if (await _authService.RegisterAsync(Username, Email, Password, FullName))
            {
                Message = _appearanceService.T("RegisterSuccessMessage");
                _mainViewModel.ShowLogin();
                return;
            }

            Message = _appearanceService.T("RegisterExistsMessage");
        }
    }
}
