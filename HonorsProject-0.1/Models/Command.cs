using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace HonorsProject_0._1.Models
{

    internal sealed class Command : ICommand
    {
        public event EventHandler? CanExecuteChanged;

        private readonly Action action;

        public Command(Action a) {
            action = a;
        }

        public bool CanExecute(object? parameter)
        {
            return true;
        }

        public void Execute(object? parameter)
        {
            action();
        }
    }
}
