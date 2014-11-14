using imBMW.App.Data;
using imBMW.App.Common;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Windows.Input;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// Документацию по шаблону проекта "Универсальное приложение с Hub" см. по адресу http://go.microsoft.com/fwlink/?LinkID=391955

namespace imBMW.App
{
    /// <summary>
    /// Страница, на которой отображаются сведения об отдельном элементе внутри группы.
    /// </summary>
    public sealed partial class ItemPage : Page
    {
        private NavigationHelper navigationHelper;
        private ObservableDictionary defaultViewModel = new ObservableDictionary();

        public ItemPage()
        {
            this.InitializeComponent();
            this.navigationHelper = new NavigationHelper(this);
            this.navigationHelper.LoadState += this.NavigationHelper_LoadState;
        }

        /// <summary>
        /// Получает NavigationHelper, используемый для облегчения навигации и управления жизненным циклом процессов.
        /// </summary>
        public NavigationHelper NavigationHelper
        {
            get { return this.navigationHelper; }
        }

        /// <summary>
        /// Получает DefaultViewModel. Эту модель можно изменить на модель строго типизированных представлений.
        /// </summary>
        public ObservableDictionary DefaultViewModel
        {
            get { return this.defaultViewModel; }
        }

        /// <summary>
        /// Заполняет страницу содержимым, передаваемым в процессе навигации.  Также предоставляется любое сохраненное состояние
        /// при повторном создании страницы из предыдущего сеанса.
        /// </summary>
        /// <param name="sender">
        /// Источник события; как правило, <see cref="NavigationHelper"/>
        /// </param>
        /// <param name="e">Данные события, предоставляющие параметр навигации, который передается
        /// <see cref="Frame.Navigate(Type, object)"/> при первоначальном запросе этой страницы, и
        /// словарь состояний, сохраненных этой страницей в ходе предыдущего
        /// сеанса.  Это состояние будет равно NULL при первом посещении страницы.</param>
        private async void NavigationHelper_LoadState(object sender, LoadStateEventArgs e)
        {
            // TODO: Создание соответствующей модели данных для области проблемы, чтобы заменить пример данных
            var item = await SampleDataSource.GetItemAsync((string)e.NavigationParameter);
            this.DefaultViewModel["Item"] = item;
        }

        #region Регистрация NavigationHelper

        /// <summary>
        /// Методы, предоставленные в этом разделе, используются исключительно для того, чтобы
        /// NavigationHelper для отклика на методы навигации страницы.
        /// Логика страницы должна быть размещена в обработчиках событий для 
        /// <see cref="Common.NavigationHelper.LoadState"/>
        /// и <see cref="Common.NavigationHelper.SaveState"/>.
        /// Параметр навигации доступен в методе LoadState 
        /// в дополнение к состоянию страницы, сохраненному в ходе предыдущего сеанса.
        /// </summary>
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            this.navigationHelper.OnNavigatedTo(e);
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            this.navigationHelper.OnNavigatedFrom(e);
        }

        #endregion
    }
}