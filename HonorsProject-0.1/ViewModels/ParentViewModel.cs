using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace HonorsProject_0._1
{
    /**
     * This is the parent class for all View Models.
     * @author Ellie Moore
     * @version 04.10.2023
     */
    internal class ParentViewModel : INotifyPropertyChanged
    {

        /**
         * Bring a Property Changed event handler into scope.
         */
        public event PropertyChangedEventHandler PropertyChanged;

        /**
         * The "SetProperty" function from the Microsoft tutorial.
         */
        protected virtual bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string propertyName = "")
        {
            if (EqualityComparer<T>.Default.Equals(storage, value))
                return false;
            storage = value;
            this.OnPropertyChanged(propertyName);
            return true;
        }

        /**
         * The "OnPropertyChanged" function from the Microsoft tutorial.
         */
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
