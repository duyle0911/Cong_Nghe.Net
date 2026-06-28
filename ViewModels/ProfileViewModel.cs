using System.Windows.Input;
using Microsoft.Win32;
using QuanLyTaiChinhCaNhan_Nhom06.Commands;
using QuanLyTaiChinhCaNhan_Nhom06.Helpers;
using QuanLyTaiChinhCaNhan_Nhom06.Services;

namespace QuanLyTaiChinhCaNhan_Nhom06.ViewModels
{
    public class ProfileViewModel : ViewModelBase
    {
        private readonly IAuthenticationService _authService;
        private readonly IAppearanceService _appearanceService;
        private string _fullName = string.Empty;
        private string _email = string.Empty;
        private string _avatar = string.Empty;
        private string _currentPassword = string.Empty;
        private string _newPassword = string.Empty;
        private string _confirmPassword = string.Empty;
        private string _message = string.Empty;
        private bool _isDarkMode;
        private string _selectedAccentColor;
        private string _selectedLanguageCode;

        public ProfileViewModel(IAuthenticationService authService, IAppearanceService appearanceService)
        {
            _authService = authService;
            _appearanceService = appearanceService;
            FullName = authService.CurrentUser?.FullName ?? string.Empty;
            Email = authService.CurrentUser?.Email ?? string.Empty;
            Avatar = authService.CurrentUser?.Avatar ?? string.Empty;
            _isDarkMode = appearanceService.Settings.IsDarkMode;
            _selectedAccentColor = appearanceService.Settings.AccentColor;
            _selectedLanguageCode = appearanceService.Settings.LanguageCode;
            SaveProfileCommand = new AsyncRelayCommand(_ => SaveProfileAsync());
            ChooseAvatarCommand = new RelayCommand(_ => ChooseAvatar());
            ClearAvatarCommand = new RelayCommand(_ => ClearAvatar(), _ => HasAvatar);
            ChangePasswordCommand = new AsyncRelayCommand(_ => ChangePasswordAsync());
            SaveAppearanceCommand = new RelayCommand(_ => SaveAppearance());
        }

        public string Username => _authService.CurrentUser?.Username ?? string.Empty;
        public string Initials => CreateInitials(FullName, Username);
        public string MemberSinceText => $"Thành viên từ {_authService.CurrentUser?.CreatedAt:dd/MM/yyyy}";
        public string FullName
        {
            get => _fullName;
            set
            {
                if (SetProperty(ref _fullName, value))
                    OnPropertyChanged(nameof(Initials));
            }
        }
        public string Email { get => _email; set => SetProperty(ref _email, value); }
        public string Avatar
        {
            get => _avatar;
            set
            {
                if (!SetProperty(ref _avatar, value))
                    return;

                OnPropertyChanged(nameof(HasAvatar));
                if (ClearAvatarCommand is RelayCommand relayCommand)
                    relayCommand.RaiseCanExecuteChanged();
            }
        }
        public bool HasAvatar => !string.IsNullOrWhiteSpace(Avatar);
        public string CurrentPassword { get => _currentPassword; set => SetProperty(ref _currentPassword, value); }
        public string NewPassword { get => _newPassword; set => SetProperty(ref _newPassword, value); }
        public string ConfirmPassword { get => _confirmPassword; set => SetProperty(ref _confirmPassword, value); }
        public string Message { get => _message; set => SetProperty(ref _message, value); }
        public bool IsDarkMode
        {
            get => _isDarkMode;
            set
            {
                if (SetProperty(ref _isDarkMode, value))
                    _appearanceService.SetDarkMode(value);
            }
        }
        public string SelectedAccentColor
        {
            get => _selectedAccentColor;
            set
            {
                if (SetProperty(ref _selectedAccentColor, value))
                    _appearanceService.SetAccentColor(value);
            }
        }
        public string SelectedLanguageCode
        {
            get => _selectedLanguageCode;
            set
            {
                if (SetProperty(ref _selectedLanguageCode, value))
                    _appearanceService.SetLanguage(value);
            }
        }
        public IReadOnlyList<AccentColorOption> AccentColors => _appearanceService.AccentColors;
        public IReadOnlyList<LanguageOption> Languages => _appearanceService.Languages;
        public ICommand SaveProfileCommand { get; }
        public ICommand ChooseAvatarCommand { get; }
        public ICommand ClearAvatarCommand { get; }
        public ICommand ChangePasswordCommand { get; }
        public ICommand SaveAppearanceCommand { get; }
        public event EventHandler? PasswordChangeSucceeded;

        private async Task SaveProfileAsync()
        {
            Email = Email.Trim();

            if (!Validator.Email(Email))
            {
                Message = "Email không hợp lệ.";
                return;
            }

            if (await _authService.UpdateProfileAsync(FullName, Email, Avatar))
            {
                Message = "Đã lưu hồ sơ.";
                OnPropertyChanged(nameof(Initials));
                OnPropertyChanged(nameof(HasAvatar));
            }
            else
            {
                Message = "Không thể lưu hồ sơ. Email có thể đã tồn tại.";
            }
        }

        private void ChooseAvatar()
        {
            var dialog = new OpenFileDialog
            {
                Title = "Chọn ảnh đại diện",
                Filter = "Ảnh đại diện (*.png;*.jpg;*.jpeg)|*.png;*.jpg;*.jpeg|Tất cả tệp (*.*)|*.*"
            };

            if (dialog.ShowDialog() != true)
                return;

            Avatar = dialog.FileName;
            Message = "Đã chọn ảnh đại diện. Nhấn Lưu thay đổi để cập nhật hồ sơ.";
        }

        private void ClearAvatar()
        {
            Avatar = string.Empty;
            Message = "Đã gỡ ảnh đại diện. Nhấn Lưu thay đổi để cập nhật hồ sơ.";
        }

        private async Task ChangePasswordAsync()
        {
            if (NewPassword != ConfirmPassword)
            {
                Message = "Mật khẩu xác nhận không khớp.";
                return;
            }

            var result = await _authService.ChangePasswordAsync(CurrentPassword, NewPassword);
            Message = result.Message;

            if (result.Success)
            {
                CurrentPassword = string.Empty;
                NewPassword = string.Empty;
                ConfirmPassword = string.Empty;
                PasswordChangeSucceeded?.Invoke(this, EventArgs.Empty);
            }
        }

        private void SaveAppearance()
        {
            _appearanceService.Save();
            Message = "Đã lưu tùy chỉnh giao diện và ngôn ngữ.";
        }

        private static string CreateInitials(string fullName, string username)
        {
            var source = string.IsNullOrWhiteSpace(fullName) ? username : fullName;
            var parts = source.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            return parts.Length switch
            {
                0 => "MF",
                1 => parts[0][..1].ToUpperInvariant(),
                _ => $"{parts[0][0]}{parts[^1][0]}".ToUpperInvariant()
            };
        }
    }
}
