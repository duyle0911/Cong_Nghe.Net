using System.Collections.ObjectModel;
using System.Windows.Input;
using System.Windows.Media;
using QuanLyTaiChinhCaNhan_Nhom06.Commands;
using QuanLyTaiChinhCaNhan_Nhom06.Helpers;
using QuanLyTaiChinhCaNhan_Nhom06.Models;
using QuanLyTaiChinhCaNhan_Nhom06.Services;

namespace QuanLyTaiChinhCaNhan_Nhom06.ViewModels
{
    public class GoalViewModel : ViewModelBase
    {
        private readonly IGoalService _goalService;
        private readonly ISessionContext _sessionContext;
        private Goal? _selectedGoal;
        private string _name = string.Empty;
        private string _description = string.Empty;
        private decimal _targetAmount;
        private decimal _addAmount;
        private DateTime _targetDate = DateTime.Now.AddMonths(1);
        private string _color = "#06B6D4";
        private bool _isEditorOpen;
        private bool _hasActiveGoals;
        private bool _hasCompletedGoals;
        private string _editorTitle = "Tạo mục tiêu";
        private string _trackedGoalsText = "0";
        private string _totalTargetText = "0 ₫";
        private string _savedAmountText = "0 ₫";
        private string _completionText = "0 mục tiêu đã hoàn thành";

        public GoalViewModel(IGoalService goalService, ISessionContext sessionContext)
        {
            _goalService = goalService;
            _sessionContext = sessionContext;
            Goals = new ObservableCollection<Goal>();
            ActiveGoals = new ObservableCollection<GoalOverview>();
            CompletedGoals = new ObservableCollection<GoalOverview>();

            SaveCommand = new AsyncRelayCommand(_ => SaveAsync());

            // Sửa lỗi nút bị khóa:
            // Không dùng điều kiện _ => SelectedGoal != null ở đây,
            // vì AsyncRelayCommand hiện tại không tự cập nhật lại CanExecute khi SelectedGoal thay đổi.
            DeleteCommand = new AsyncRelayCommand(_ => DeleteAsync());
            AddMoneyCommand = new AsyncRelayCommand(_ => AddMoneyAsync());

            NewCommand = new RelayCommand(_ => OpenNewEditor());
            CancelCommand = new RelayCommand(_ => CloseEditor());
            EditCommand = new RelayCommand(item => OpenEditor((item as GoalOverview)?.Goal));

            _ = RunSafeAsync(LoadAsync);
        }

        public ObservableCollection<Goal> Goals { get; }
        public ObservableCollection<GoalOverview> ActiveGoals { get; }
        public ObservableCollection<GoalOverview> CompletedGoals { get; }

        public Goal? SelectedGoal
        {
            get => _selectedGoal;
            set
            {
                if (!SetProperty(ref _selectedGoal, value))
                    return;

                if (value == null)
                    return;

                Name = value.Name;
                Description = value.Description ?? string.Empty;
                TargetAmount = value.TargetAmount;
                TargetDate = value.TargetDate;
                Color = value.Color ?? "#06B6D4";
            }
        }

        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        public string Description
        {
            get => _description;
            set => SetProperty(ref _description, value);
        }

        public decimal TargetAmount
        {
            get => _targetAmount;
            set => SetProperty(ref _targetAmount, value);
        }

        public decimal AddAmount
        {
            get => _addAmount;
            set => SetProperty(ref _addAmount, value);
        }

        public DateTime TargetDate
        {
            get => _targetDate;
            set => SetProperty(ref _targetDate, value);
        }

        public string Color
        {
            get => _color;
            set => SetProperty(ref _color, value);
        }

        public bool IsEditorOpen
        {
            get => _isEditorOpen;
            set => SetProperty(ref _isEditorOpen, value);
        }

        public bool HasActiveGoals
        {
            get => _hasActiveGoals;
            set => SetProperty(ref _hasActiveGoals, value);
        }

        public bool HasCompletedGoals
        {
            get => _hasCompletedGoals;
            set => SetProperty(ref _hasCompletedGoals, value);
        }

        public string EditorTitle
        {
            get => _editorTitle;
            set => SetProperty(ref _editorTitle, value);
        }

        public string TrackedGoalsText
        {
            get => _trackedGoalsText;
            set => SetProperty(ref _trackedGoalsText, value);
        }

        public string TotalTargetText
        {
            get => _totalTargetText;
            set => SetProperty(ref _totalTargetText, value);
        }

        public string SavedAmountText
        {
            get => _savedAmountText;
            set => SetProperty(ref _savedAmountText, value);
        }

        public string CompletionText
        {
            get => _completionText;
            set => SetProperty(ref _completionText, value);
        }

        public ICommand SaveCommand { get; }
        public ICommand DeleteCommand { get; }
        public ICommand AddMoneyCommand { get; }
        public ICommand NewCommand { get; }
        public ICommand CancelCommand { get; }
        public ICommand EditCommand { get; }

        private async Task LoadAsync()
        {
            Goals.Clear();
            ActiveGoals.Clear();
            CompletedGoals.Clear();

            foreach (var goal in await _goalService.GetGoalsAsync(_sessionContext.CurrentUserId ?? 0))
            {
                Goals.Add(goal);

                var overview = new GoalOverview(goal);

                if (goal.IsCompleted)
                    CompletedGoals.Add(overview);
                else
                    ActiveGoals.Add(overview);
            }

            HasActiveGoals = ActiveGoals.Any();
            HasCompletedGoals = CompletedGoals.Any();

            TrackedGoalsText = ActiveGoals.Count.ToString();
            TotalTargetText = DashboardPresentation.FormatMoney(Goals.Sum(goal => goal.TargetAmount));
            SavedAmountText = DashboardPresentation.FormatMoney(Goals.Sum(goal => goal.CurrentAmount));
            CompletionText = $"{CompletedGoals.Count} mục tiêu đã hoàn thành";
        }

        private async Task SaveAsync()
        {
            if (!Validator.Required(Name))
            {
                DialogHelper.Error("Vui lòng nhập tên mục tiêu.");
                return;
            }

            if (TargetAmount <= 0)
            {
                DialogHelper.Error("Số tiền mục tiêu phải lớn hơn 0.");
                return;
            }

            var userId = _sessionContext.CurrentUserId ?? 0;

            var ok = SelectedGoal == null
                ? await _goalService.CreateGoalAsync(new Goal
                {
                    Name = Name,
                    Description = Description,
                    TargetAmount = TargetAmount,
                    TargetDate = TargetDate,
                    UserId = userId,
                    Color = Color
                }, userId)
                : await _goalService.UpdateGoalAsync(
                    SelectedGoal.Id,
                    Name,
                    Description,
                    TargetAmount,
                    TargetDate,
                    Color,
                    userId);

            if (!ok)
            {
                DialogHelper.Error("Không thể lưu mục tiêu.");
                return;
            }

            CloseEditor();
            await LoadAsync();
        }

        private async Task AddMoneyAsync()
        {
            if (SelectedGoal == null)
            {
                DialogHelper.Error("Vui lòng chọn một mục tiêu trước khi nạp tiền.");
                return;
            }

            if (AddAmount <= 0)
            {
                DialogHelper.Error("Số tiền thêm vào mục tiêu phải lớn hơn 0.");
                return;
            }

            var result = await _goalService.AddMoneyToGoalAsync(
                SelectedGoal.Id,
                AddAmount,
                _sessionContext.CurrentUserId ?? 0);

            if (!result.Success)
            {
                DialogHelper.Error(result.Message);
                return;
            }

            AddAmount = 0;
            await LoadAsync();
        }

        private async Task DeleteAsync()
        {
            if (SelectedGoal == null)
            {
                DialogHelper.Error("Vui lòng chọn một mục tiêu trước khi xóa.");
                return;
            }

            if (!DialogHelper.Confirm($"Xóa mục tiêu '{SelectedGoal.Name}'?"))
                return;

            var ok = await _goalService.DeleteGoalAsync(
                SelectedGoal.Id,
                _sessionContext.CurrentUserId ?? 0);

            if (!ok)
            {
                DialogHelper.Error("Không thể xóa mục tiêu.");
                return;
            }

            CloseEditor();
            await LoadAsync();
        }

        private void OpenNewEditor()
        {
            ClearForm();
            EditorTitle = "Tạo mục tiêu";
            IsEditorOpen = true;
        }

        private void OpenEditor(Goal? goal)
        {
            if (goal == null)
                return;

            SelectedGoal = goal;
            EditorTitle = "Chỉnh sửa mục tiêu";
            IsEditorOpen = true;
        }

        private void CloseEditor()
        {
            ClearForm();
            IsEditorOpen = false;
        }

        private void ClearForm()
        {
            SelectedGoal = null;
            Name = string.Empty;
            Description = string.Empty;
            TargetAmount = 0;
            AddAmount = 0;
            TargetDate = DateTime.Now.AddMonths(1);
            Color = "#06B6D4";
        }
    }

    public class GoalOverview
    {
        public GoalOverview(Goal goal)
        {
            Goal = goal;
            Name = goal.Name;

            DeadlineText = goal.IsCompleted
                ? $"Hoàn thành: {goal.CompletedDate ?? goal.UpdatedAt:dd/MM/yyyy}"
                : $"Hạn: {goal.TargetDate:dd/MM/yyyy}";

            AmountText = $"{DashboardPresentation.FormatMoney(goal.CurrentAmount)} / {DashboardPresentation.FormatMoney(goal.TargetAmount)}";

            RemainingText = goal.IsCompleted
                ? DashboardPresentation.FormatMoney(goal.TargetAmount)
                : $"Còn {DashboardPresentation.FormatMoney(Math.Max(0, goal.TargetAmount - goal.CurrentAmount))}";

            ProgressPercent = goal.ProgressPercent;
            ProgressText = $"{ProgressPercent:N0}%";

            AccentBrush = DashboardPresentation.CreateBrush(goal.Color ?? "#06B6D4");
            SoftAccentBrush = DashboardPresentation.CreateBrush(goal.IsCompleted ? "#F3E8FF" : "#E0F2FE");
        }

        public Goal Goal { get; }
        public string Name { get; }
        public string DeadlineText { get; }
        public string AmountText { get; }
        public string RemainingText { get; }
        public decimal ProgressPercent { get; }
        public string ProgressText { get; }
        public Brush AccentBrush { get; }
        public Brush SoftAccentBrush { get; }
    }
}