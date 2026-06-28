using System.Windows.Input;
using QuanLyTaiChinhCaNhan_Nhom06.Commands;
using QuanLyTaiChinhCaNhan_Nhom06.Helpers;
using QuanLyTaiChinhCaNhan_Nhom06.Services;

namespace QuanLyTaiChinhCaNhan_Nhom06.ViewModels
{
    public class LoginViewModel : ViewModelBase
    {
        private readonly IAuthenticationService _authService;
        private readonly MainViewModel _mainViewModel;
        private string _username = string.Empty;
        private string _password = string.Empty;
        private string _message = string.Empty;

        public LoginViewModel(IAuthenticationService authService, MainViewModel mainViewModel)
        {
            _authService = authService;
            _mainViewModel = mainViewModel;
            LoginCommand = new AsyncRelayCommand(_ => LoginAsync());
            ShowRegisterCommand = new RelayCommand(_ => _mainViewModel.ShowRegister());
        }

        public string Username { get => _username; set => SetProperty(ref _username, value); }
        public string Password { get => _password; set => SetProperty(ref _password, value); }
        public string Message { get => _message; set => SetProperty(ref _message, value); }
        public ICommand LoginCommand { get; }
        public ICommand ShowRegisterCommand { get; }

        private async Task LoginAsync()
        {
            if (!Validator.Required(Username) || !Validator.Required(Password))
            {
                Message = "Vui lòng nhập tên đăng nhập và mật khẩu.";
                return;
            }

            if (await _authService.LoginAsync(Username, Password))
            {
                Message = string.Empty;
                _mainViewModel.OnLoginSuccess();
                return;
            }

            Message = "Tên đăng nhập hoặc mật khẩu không đúng.";
        }
    }
}

