using System.Collections.Generic;
using System.ComponentModel;

namespace A0Utils.Wpf.Models
{
    public class UpdateModel : INotifyPropertyChanged
    {
        public string Name { get; set; }
        public string Category { get; set; }
        public IEnumerable<string> Urls { get; set; }
        public string Index { get; set; } = string.Empty;
        public string Date { get; set; } = string.Empty;

        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                _isSelected = value;
                OnPropertyChanged(nameof(IsSelected));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    }
}
