using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace imBMW.Universal.App.Models
{
    public class ObservableObject : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual bool Set<T>(ref T field, T value, [CallerMemberName]string property = "")
        {
            if (field == null && value == null || field != null && field.Equals(value))
            {
                return false;
            }
            field = value;
            OnPropertyChanged(property);
            return true;
        }

        protected void OnPropertyChanged([CallerMemberName]string property = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
        }
    }
}