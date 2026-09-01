using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using System.Windows.Media;

namespace TaskbarDock.Models
{
    public class DockItem : INotifyPropertyChanged
    {
        private string _id = Guid.NewGuid().ToString();
        private string _title = "Application";
        private string _executablePath = string.Empty;
        private string _arguments = string.Empty;
        private string _iconPath = string.Empty;
        private string? _customIconPath;
        private string? _customTitle;
        private bool _isPinned = true;
        private bool _isRunning;
        private bool _isSystemItem;
        private string _systemAction = string.Empty; // "start", "explorer", "terminal", "recyclebin", "settings", "store", "calc", "notepad"
        private double _currentSize = 56.0;
        private double _targetScale = 1.0;

        public string Id
        {
            get => _id;
            set { _id = value; OnPropertyChanged(); }
        }

        public string Title
        {
            get => _title;
            set { _title = value; OnPropertyChanged(); OnPropertyChanged(nameof(DisplayName)); }
        }

        public string? CustomTitle
        {
            get => _customTitle;
            set { _customTitle = value; OnPropertyChanged(); OnPropertyChanged(nameof(DisplayName)); }
        }

        [JsonIgnore]
        public string DisplayName => !string.IsNullOrWhiteSpace(CustomTitle) ? CustomTitle : Title;

        public string ExecutablePath
        {
            get => _executablePath;
            set { _executablePath = value; OnPropertyChanged(); }
        }

        public string Arguments
        {
            get => _arguments;
            set { _arguments = value; OnPropertyChanged(); }
        }

        public string IconPath
        {
            get => _iconPath;
            set { _iconPath = value; OnPropertyChanged(); }
        }

        public string? CustomIconPath
        {
            get => _customIconPath;
            set { _customIconPath = value; OnPropertyChanged(); }
        }

        public bool IsPinned
        {
            get => _isPinned;
            set { _isPinned = value; OnPropertyChanged(); }
        }

        public bool IsSystemItem
        {
            get => _isSystemItem;
            set { _isSystemItem = value; OnPropertyChanged(); }
        }

        public string SystemAction
        {
            get => _systemAction;
            set { _systemAction = value; OnPropertyChanged(); }
        }

        [JsonIgnore]
        public bool IsRunning
        {
            get => _isRunning;
            set { _isRunning = value; OnPropertyChanged(); }
        }

        [JsonIgnore]
        public double CurrentSize
        {
            get => _currentSize;
            set { _currentSize = value; OnPropertyChanged(); }
        }

        [JsonIgnore]
        public double TargetScale
        {
            get => _targetScale;
            set { _targetScale = value; OnPropertyChanged(); }
        }

        [JsonIgnore]
        public ImageSource? IconImage { get; set; }

        [JsonIgnore]
        public List<IntPtr> WindowHandles { get; } = new();

        [JsonIgnore]
        public List<int> ProcessIds { get; } = new();

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
