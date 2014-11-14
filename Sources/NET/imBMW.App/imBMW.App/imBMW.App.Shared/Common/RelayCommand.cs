using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace imBMW.App.Common
{
    /// <summary>
    /// Команда, единственным назначением которой является передача собственной функциональности 
    /// другим объектам путем вызова делегатов. 
    /// По умолчанию метод CanExecute возвращает значение "true".
    /// <see cref="RaiseCanExecuteChanged"/> необходимо вызывать каждый раз, когда
    /// Ожидалось, что <see cref="CanExecute"/> вернет другое значение.
    /// </summary>
    public class RelayCommand : ICommand
    {
        private readonly Action _execute;
        private readonly Func<bool> _canExecute;

        /// <summary>
        /// Создано при вызове RaiseCanExecuteChanged.
        /// </summary>
        public event EventHandler CanExecuteChanged;

        /// <summary>
        /// Создает новую команду, которая всегда может выполняться.
        /// </summary>
        /// <param name="execute">Логика выполнения.</param>
        public RelayCommand(Action execute)
            : this(execute, null)
        {
        }

        /// <summary>
        /// Создает новую команду.
        /// </summary>
        /// <param name="execute">Логика выполнения.</param>
        /// <param name="canExecute">Логика состояния выполнения.</param>
        public RelayCommand(Action execute, Func<bool> canExecute)
        {
            if (execute == null)
                throw new ArgumentNullException("execute");
            _execute = execute;
            _canExecute = canExecute;
        }

        /// <summary>
        /// Определяет, можно ли выполнить эту команду <see cref="RelayCommand"/> в текущем состоянии.
        /// </summary>
        /// <param name="parameter">
        /// Данные, используемые командой. Если команда не требует передачи данных, этот объект можно установить равным NULL.
        /// </param>
        /// <returns>true, если команда может быть выполнена; в противном случае - false.</returns>
        public bool CanExecute(object parameter)
        {
            return _canExecute == null ? true : _canExecute();
        }

        /// <summary>
        /// Выполняет <see cref="RelayCommand"/> текущей цели команды.
        /// </summary>
        /// <param name="parameter">
        /// Данные, используемые командой. Если команда не требует передачи данных, этот объект можно установить равным NULL.
        /// </param>
        public void Execute(object parameter)
        {
            _execute();
        }

        /// <summary>
        /// Метод, используемый для создания события <see cref="CanExecuteChanged"/>
        /// чтобы показать, что возвращаемое значение <see cref="CanExecute"/>
        /// метод изменился.
        /// </summary>
        public void RaiseCanExecuteChanged()
        {
            var handler = CanExecuteChanged;
            if (handler != null)
            {
                handler(this, EventArgs.Empty);
            }
        }
    }
}