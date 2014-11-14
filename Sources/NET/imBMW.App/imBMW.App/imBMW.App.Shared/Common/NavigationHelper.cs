using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace imBMW.App.Common
{
    /// <summary>
    /// Класс NavigationHelper облегчает навигацию между страницами.  Он предоставляет команды для 
    /// переходы назад и вперед, а также регистрация стандартных действий мыши и клавиатуры 
    /// ярлыки, используемые для переходов назад и вперед в Windows, и аппаратная кнопка "Назад" в
    /// Windows Phone. Кроме того, он интегрируется с SuspensionManger для обработки управления
    /// жизненным циклом процесса и состоянием при переходах между страницами.
    /// </summary>
    /// <example>
    /// Чтобы воспользоваться NavigationHelper, выполните следующие два шага или
    /// начать с BasicPage или любого другого шаблона элементов страниц, отличного от BlankPage.
    /// 
    /// 1) Создайте экземпляр NavigationHelper в любом месте, например в 
    ///    конструктор для страницы и зарегистрировать обратный вызов для LoadState и
    ///     события SaveState.
    /// <code>
    ///     public MyPage()
    ///     {
    ///         this.InitializeComponent();
    ///         var navigationHelper = new NavigationHelper(this);
    ///         this.navigationHelper.LoadState += navigationHelper_LoadState;
    ///         this.navigationHelper.SaveState += navigationHelper_SaveState;
    ///     }
    ///     
    ///     private async void navigationHelper_LoadState(object sender, LoadStateEventArgs e)
    ///     { }
    ///     private async void navigationHelper_SaveState(object sender, LoadStateEventArgs e)
    ///     { }
    /// </code>
    /// 
    /// 2) Регистрируйте страницу для вызова NavigationHelper всякий раз, когда страница участвует 
    ///     в навигации, переопределяя <see cref="Windows.UI.Xaml.Controls.Page.OnNavigatedTo"/> 
    ///     и события <see cref="Windows.UI.Xaml.Controls.Page.OnNavigatedFrom"/>.
    /// <code>
    ///     protected override void OnNavigatedTo(NavigationEventArgs e)
    ///     {
    ///         navigationHelper.OnNavigatedTo(e);
    ///     }
    ///     
    ///     protected override void OnNavigatedFrom(NavigationEventArgs e)
    ///     {
    ///         navigationHelper.OnNavigatedFrom(e);
    ///     }
    /// </code>
    /// </example>
    [Windows.Foundation.Metadata.WebHostHidden]
    public class NavigationHelper : DependencyObject
    {
        private Page Page { get; set; }
        private Frame Frame { get { return this.Page.Frame; } }

        /// <summary>
        /// Инициализирует новый экземпляр класса <see cref="NavigationHelper"/>.
        /// </summary>
        /// <param name="page">Ссылка на текущую страницу, используемая для навигации.  
        /// Эта ссылка позволяет осуществлять различные действия с кадрами и гарантировать, что клавиатура 
        /// запросы навигации происходят, только когда страница занимает все окно.</param>
        public NavigationHelper(Page page)
        {
            this.Page = page;

            // Если данная страница является частью визуального дерева, возникают два изменения:
            // 1) Сопоставление состояния просмотра приложения с визуальным состоянием для страницы.
            // 2) Обработка запросов на аппаратные переходы
            this.Page.Loaded += (sender, e) =>
            {
#if WINDOWS_PHONE_APP
                Windows.Phone.UI.Input.HardwareButtons.BackPressed += HardwareButtons_BackPressed;
#else
                // Навигация с помощью мыши и клавиатуры применяется, только если страница занимает все окно
                if (this.Page.ActualHeight == Window.Current.Bounds.Height &&
                    this.Page.ActualWidth == Window.Current.Bounds.Width)
                {
                    // Непосредственное прослушивание окна, поэтому фокус не требуется
                    Window.Current.CoreWindow.Dispatcher.AcceleratorKeyActivated +=
                        CoreDispatcher_AcceleratorKeyActivated;
                    Window.Current.CoreWindow.PointerPressed +=
                        this.CoreWindow_PointerPressed;
                }
#endif
            };

            // Отмена тех же изменений, когда страница перестает быть видимой
            this.Page.Unloaded += (sender, e) =>
            {
#if WINDOWS_PHONE_APP
                Windows.Phone.UI.Input.HardwareButtons.BackPressed -= HardwareButtons_BackPressed;
#else
                Window.Current.CoreWindow.Dispatcher.AcceleratorKeyActivated -=
                    CoreDispatcher_AcceleratorKeyActivated;
                Window.Current.CoreWindow.PointerPressed -=
                    this.CoreWindow_PointerPressed;
#endif
            };
        }

        #region Поддержка навигации

        RelayCommand _goBackCommand;
        RelayCommand _goForwardCommand;

        /// <summary>
        /// <see cref="RelayCommand"/> используется для привязки к свойству Command кнопки "Назад"
        /// для перехода к последнему элементу в журнале обратных переходов, если кадр
        /// управляет собственным журналом навигации.
        /// 
        /// <see cref="RelayCommand"/> настроено на использование виртуального метода <see cref="GoBack"/>
        /// в качестве действия Execute Action и <see cref="CanGoBack"/> для CanExecute.
        /// </summary>
        public RelayCommand GoBackCommand
        {
            get
            {
                if (_goBackCommand == null)
                {
                    _goBackCommand = new RelayCommand(
                        () => this.GoBack(),
                        () => this.CanGoBack());
                }
                return _goBackCommand;
            }
            set
            {
                _goBackCommand = value;
            }
        }
        /// <summary>
        /// <see cref="RelayCommand"/> используется для перехода к самому последнему элементу в 
        /// журнал прямой навигации, если кадр самостоятельно управляет своим журналом навигации.
        /// 
        /// <see cref="RelayCommand"/> настроено на использование виртуального метода <see cref="GoForward"/>
        /// в качестве действия Execute Action и <see cref="CanGoForward"/> для CanExecute.
        /// </summary>
        public RelayCommand GoForwardCommand
        {
            get
            {
                if (_goForwardCommand == null)
                {
                    _goForwardCommand = new RelayCommand(
                        () => this.GoForward(),
                        () => this.CanGoForward());
                }
                return _goForwardCommand;
            }
        }

        /// <summary>
        /// Виртуальный метод, используемый свойством <see cref="GoBackCommand"/>
        /// для определения возможности перехода <see cref="Frame"/> назад.
        /// </summary>
        /// <returns>
        /// true, если <see cref="Frame"/> имеет хотя бы одно вхождение 
        /// в журнале обратной навигации.
        /// </returns>
        public virtual bool CanGoBack()
        {
            return this.Frame != null && this.Frame.CanGoBack;
        }
        /// <summary>
        /// Виртуальный метод, используемый свойством <see cref="GoForwardCommand"/>
        /// для определения возможности перехода <see cref="Frame"/> вперед.
        /// </summary>
        /// <returns>
        /// true, если <see cref="Frame"/> имеет хотя бы одно вхождение 
        /// в журнале прямой навигации.
        /// </returns>
        public virtual bool CanGoForward()
        {
            return this.Frame != null && this.Frame.CanGoForward;
        }

        /// <summary>
        /// Виртуальный метод, используемый свойством <see cref="GoBackCommand"/>
        /// для вызова метода <see cref="Windows.UI.Xaml.Controls.Frame.GoBack"/>.
        /// </summary>
        public virtual void GoBack()
        {
            if (this.Frame != null && this.Frame.CanGoBack) this.Frame.GoBack();
        }
        /// <summary>
        /// Виртуальный метод, используемый свойством <see cref="GoForwardCommand"/>
        /// для вызова метода <see cref="Windows.UI.Xaml.Controls.Frame.GoForward"/>.
        /// </summary>
        public virtual void GoForward()
        {
            if (this.Frame != null && this.Frame.CanGoForward) this.Frame.GoForward();
        }

#if WINDOWS_PHONE_APP
        /// <summary>
        /// Вызывается при нажатии аппаратной кнопки "Назад". Только для Windows Phone.
        /// </summary>
        /// <param name="sender">Экземпляр, инициировавший событие.</param>
        /// <param name="e">Данные события, описывающие условия, которые привели к возникновению события.</param>
        private void HardwareButtons_BackPressed(object sender, Windows.Phone.UI.Input.BackPressedEventArgs e)
        {
            if (this.GoBackCommand.CanExecute(null))
            {
                e.Handled = true;
                this.GoBackCommand.Execute(null);
            }
        }
#else
        /// <summary>
        /// Вызывается при каждом нажатии клавиши, включая системные клавиши, такие как клавиша ALT, если
        /// данная страница активна и занимает все окно.  Используется для обнаружения навигации с помощью клавиатуры
        /// между страницами, даже если сама страница не имеет фокуса.
        /// </summary>
        /// <param name="sender">Экземпляр, инициировавший событие.</param>
        /// <param name="e">Данные события, описывающие условия, которые привели к возникновению события.</param>
        private void CoreDispatcher_AcceleratorKeyActivated(CoreDispatcher sender,
            AcceleratorKeyEventArgs e)
        {
            var virtualKey = e.VirtualKey;

            // Дальнейшее изучение следует выполнять, только если нажата клавиша со стрелкой влево или вправо либо назначенная клавиша "Назад" или
            // нажаты
            if ((e.EventType == CoreAcceleratorKeyEventType.SystemKeyDown ||
                e.EventType == CoreAcceleratorKeyEventType.KeyDown) &&
                (virtualKey == VirtualKey.Left || virtualKey == VirtualKey.Right ||
                (int)virtualKey == 166 || (int)virtualKey == 167))
            {
                var coreWindow = Window.Current.CoreWindow;
                var downState = CoreVirtualKeyStates.Down;
                bool menuKey = (coreWindow.GetKeyState(VirtualKey.Menu) & downState) == downState;
                bool controlKey = (coreWindow.GetKeyState(VirtualKey.Control) & downState) == downState;
                bool shiftKey = (coreWindow.GetKeyState(VirtualKey.Shift) & downState) == downState;
                bool noModifiers = !menuKey && !controlKey && !shiftKey;
                bool onlyAlt = menuKey && !controlKey && !shiftKey;

                if (((int)virtualKey == 166 && noModifiers) ||
                    (virtualKey == VirtualKey.Left && onlyAlt))
                {
                    // Переход назад при нажатии клавиши "Назад" или сочетания клавиш ALT+СТРЕЛКА ВЛЕВО
                    e.Handled = true;
                    this.GoBackCommand.Execute(null);
                }
                else if (((int)virtualKey == 167 && noModifiers) ||
                    (virtualKey == VirtualKey.Right && onlyAlt))
                {
                    // Переход вперед при нажатии клавиши "Вперед" или сочетания клавиш ALT+СТРЕЛКА ВПРАВО
                    e.Handled = true;
                    this.GoForwardCommand.Execute(null);
                }
            }
        }

        /// <summary>
        /// Вызывается при каждом щелчке мыши, касании сенсорного экрана или аналогичном действии, если эта
        /// страница активна и занимает все окно.  Используется для обнаружения нажатий мышью кнопок "Вперед" и
        /// "Назад" в браузере для перехода между страницами.
        /// </summary>
        /// <param name="sender">Экземпляр, инициировавший событие.</param>
        /// <param name="e">Данные события, описывающие условия, которые привели к возникновению события.</param>
        private void CoreWindow_PointerPressed(CoreWindow sender,
            PointerEventArgs e)
        {
            var properties = e.CurrentPoint.Properties;

            // Пропуск сочетаний кнопок, включающих левую, правую и среднюю кнопки
            if (properties.IsLeftButtonPressed || properties.IsRightButtonPressed ||
                properties.IsMiddleButtonPressed) return;

            // Если нажата кнопка "Назад" или "Вперед" (но не обе), выполняется соответствующий переход
            bool backPressed = properties.IsXButton1Pressed;
            bool forwardPressed = properties.IsXButton2Pressed;
            if (backPressed ^ forwardPressed)
            {
                e.Handled = true;
                if (backPressed) this.GoBackCommand.Execute(null);
                if (forwardPressed) this.GoForwardCommand.Execute(null);
            }
        }
#endif

        #endregion

        #region Управление жизненным циклом процесса

        private String _pageKey;

        /// <summary>
        /// Регистрация данного события на текущей странице с целью заполнения страницы
        /// содержимым, передаваемым в процессе навигации, а также любым сохраненным
        /// состояние, предоставляемое при повторном создании страницы из предыдущего сеанса.
        /// </summary>
        public event LoadStateEventHandler LoadState;
        /// <summary>
        /// Регистрация данного события на текущей странице с целью сохранения
        /// состояние, связанное с текущей страницей в случае
        /// приложение приостановлено или страница удалена из
        /// кэше навигации.
        /// </summary>
        public event SaveStateEventHandler SaveState;

        /// <summary>
        /// Вызывается перед отображением этой страницы в кадре.  
        /// Данный метод вызывает <see cref="LoadState"/>, где все страничные
        /// необходимо создать логику навигации и управления жизненным циклом процессов.
        /// </summary>
        /// <param name="e">Данные о событиях, описывающие, каким образом была достигнута эта страница.  Свойство Parameter
        /// задает группу для отображения.</param>
        public void OnNavigatedTo(NavigationEventArgs e)
        {
            var frameState = SuspensionManager.SessionStateForFrame(this.Frame);
            this._pageKey = "Page-" + this.Frame.BackStackDepth;

            if (e.NavigationMode == NavigationMode.New)
            {
                // Очистка существующего состояния для перехода вперед при добавлении новой страницы в
                // стек навигации
                var nextPageKey = this._pageKey;
                int nextPageIndex = this.Frame.BackStackDepth;
                while (frameState.Remove(nextPageKey))
                {
                    nextPageIndex++;
                    nextPageKey = "Page-" + nextPageIndex;
                }

                // Передача параметра навигации на новую страницу
                if (this.LoadState != null)
                {
                    this.LoadState(this, new LoadStateEventArgs(e.Parameter, null));
                }
            }
            else
            {
                // Передача на страницу параметра навигации и сохраненного состояния страницы с использованием
                // той же стратегии загрузки приостановленного состояния и повторного создания страниц, удаленных
                // из кэша
                if (this.LoadState != null)
                {
                    this.LoadState(this, new LoadStateEventArgs(e.Parameter, (Dictionary<String, Object>)frameState[this._pageKey]));
                }
            }
        }

        /// <summary>
        /// Вызывается, если данная страница больше не отображается во фрейме.
        /// Этот метод вызывает <see cref="SaveState"/>, где все страничные
        /// необходимо создать логику навигации и управления жизненным циклом процессов.
        /// </summary>
        /// <param name="e">Данные о событиях, описывающие, каким образом была достигнута эта страница.  Свойство Parameter
        /// задает группу для отображения.</param>
        public void OnNavigatedFrom(NavigationEventArgs e)
        {
            var frameState = SuspensionManager.SessionStateForFrame(this.Frame);
            var pageState = new Dictionary<String, Object>();
            if (this.SaveState != null)
            {
                this.SaveState(this, new SaveStateEventArgs(pageState));
            }
            frameState[_pageKey] = pageState;
        }

        #endregion
    }

    /// <summary>
    /// Представляет метод, который будет обрабатывать событие <see cref="NavigationHelper.LoadState"/>
    /// </summary>
    public delegate void LoadStateEventHandler(object sender, LoadStateEventArgs e);
    /// <summary>
    /// Представляет метод, который будет обрабатывать событие <see cref="NavigationHelper.SaveState"/>
    /// </summary>
    public delegate void SaveStateEventHandler(object sender, SaveStateEventArgs e);

    /// <summary>
    /// Класс, используемый для хранения данных о событии, необходимых когда страница пытается загрузить состояние.
    /// </summary>
    public class LoadStateEventArgs : EventArgs
    {
        /// <summary>
        /// Значение параметра, передаваемое <see cref="Frame.Navigate(Type, Object)"/> 
        /// при первоначальном запросе этой страницы.
        /// </summary>
        public Object NavigationParameter { get; private set; }
        /// <summary>
        /// Словарь состояний, сохраненных этой страницей в ходе предыдущего
        /// сеанса.  Это значение будет равно NULL при первом посещении страницы.
        /// </summary>
        public Dictionary<string, Object> PageState { get; private set; }

        /// <summary>
        /// Инициализирует новый экземпляр класса <see cref="LoadStateEventArgs"/>.
        /// </summary>
        /// <param name="navigationParameter">
        /// Значение параметра, передаваемое <see cref="Frame.Navigate(Type, Object)"/> 
        /// при первоначальном запросе этой страницы.
        /// </param>
        /// <param name="pageState">
        /// Словарь состояний, сохраненных этой страницей в ходе предыдущего
        /// сеанса.  Это значение будет равно NULL при первом посещении страницы.
        /// </param>
        public LoadStateEventArgs(Object navigationParameter, Dictionary<string, Object> pageState)
            : base()
        {
            this.NavigationParameter = navigationParameter;
            this.PageState = pageState;
        }
    }
    /// <summary>
    /// Класс, используемый для хранения данных о событии, необходимых когда страница пытается сохранить состояние.
    /// </summary>
    public class SaveStateEventArgs : EventArgs
    {
        /// <summary>
        /// Пустой словарь для заполнения сериализуемым состоянием.
        /// </summary>
        public Dictionary<string, Object> PageState { get; private set; }

        /// <summary>
        /// Инициализирует новый экземпляр класса <see cref="SaveStateEventArgs"/>.
        /// </summary>
        /// <param name="pageState">Пустой словарь, заполняемый сериализуемым состоянием.</param>
        public SaveStateEventArgs(Dictionary<string, Object> pageState)
            : base()
        {
            this.PageState = pageState;
        }
    }
}
