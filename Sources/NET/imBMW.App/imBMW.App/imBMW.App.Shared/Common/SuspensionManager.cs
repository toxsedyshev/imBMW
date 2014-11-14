using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace imBMW.App.Common
{
    /// <summary>
    /// SuspensionManager записывает глобальное состояние сеанса, чтобы упростить управление жизненным циклом процессов
    /// для приложения.  Обратите внимание, что состояние сеанса автоматически очищается в самых различных
    /// условиях, и его следует использовать только для хранения сведений, которые удобно
    /// сохранять между сеансами, но которые должны игнорироваться при сбое или
    /// обновлении приложения.
    /// </summary>
    internal sealed class SuspensionManager
    {
        private static Dictionary<string, object> _sessionState = new Dictionary<string, object>();
        private static List<Type> _knownTypes = new List<Type>();
        private const string sessionStateFilename = "_sessionState.xml";

        /// <summary>
        /// Предоставление доступа к глобальному состоянию сеанса для текущего сеанса.  Это состояние
        /// сериализуется методом <see cref="SaveAsync"/> и восстанавливается
        /// методом <see cref="RestoreAsync"/>, поэтому значения обязаны поддерживать сериализацию
        /// классом <see cref="DataContractSerializer"/> и должны быть максимально компактными.  Настоятельно рекомендуется
        /// использовать строки и другие самодостаточные типы данных.
        /// </summary>
        public static Dictionary<string, object> SessionState
        {
            get { return _sessionState; }
        }

        /// <summary>
        /// Список пользовательских типов, предоставляемых сериализатору <see cref="DataContractSerializer"/> при
        /// чтении или записи состояния сеанса.  Первоначально список является пустым; для настройки процесса сериализации
        /// можно добавить дополнительные типы.
        /// </summary>
        public static List<Type> KnownTypes
        {
            get { return _knownTypes; }
        }

        /// <summary>
        /// Сохранение текущего <see cref="SessionState"/>.  Любые экземпляры <see cref="Frame"/>,
        /// зарегистрированные с помощью <see cref="RegisterFrame"/>, также сохранят свой текущий
        /// стек навигации, который, в свою очередь, предоставляет их активной <see cref="Page"/> возможность
        /// сохранения своего состояния.
        /// </summary>
        /// <returns>Асинхронная задача, отражающая сохранение состояния сеанса.</returns>
        public static async Task SaveAsync()
        {
            try
            {
                // Сохранение состояния навигации для всех зарегистрированных фреймов
                foreach (var weakFrameReference in _registeredFrames)
                {
                    Frame frame;
                    if (weakFrameReference.TryGetTarget(out frame))
                    {
                        SaveFrameNavigationState(frame);
                    }
                }

                // Синхронная сериализация состояния сеанса с целью запрета асинхронного доступа к общему
                // состоянию
                MemoryStream sessionData = new MemoryStream();
                DataContractSerializer serializer = new DataContractSerializer(typeof(Dictionary<string, object>), _knownTypes);
                serializer.WriteObject(sessionData, _sessionState);

                // Получение выходного потока для файла SessionState и асинхронная запись состояния
                StorageFile file = await ApplicationData.Current.LocalFolder.CreateFileAsync(sessionStateFilename, CreationCollisionOption.ReplaceExisting);
                using (Stream fileStream = await file.OpenStreamForWriteAsync())
                {
                    sessionData.Seek(0, SeekOrigin.Begin);
                    await sessionData.CopyToAsync(fileStream);
                }
            }
            catch (Exception e)
            {
                throw new SuspensionManagerException(e);
            }
        }

        /// <summary>
        /// Восстанавливает ранее сохраненный объект <see cref="SessionState"/>.  Любые экземпляры <see cref="Frame"/>,
        /// зарегистрированные с помощью <see cref="RegisterFrame"/>, также восстановят свое предыдущее
        /// состояние навигации, которое, в свою очередь, предоставляет их активной <see cref="Page"/> возможность восстановления
        /// состояния.
        /// </summary>
        /// <param name="sessionBaseKey">Необязательный ключ, определяющий тип сеанса.
        /// Его можно использовать для различения нескольких сеансов запуска приложения.</param>
        /// <returns>Асинхронная задача, отражающая чтение состояния сеанса.  Содержимое
        /// <see cref="SessionState"/> не должно считаться достоверным, пока эта задача не будет
        /// завершена.</returns>
        public static async Task RestoreAsync(String sessionBaseKey = null)
        {
            _sessionState = new Dictionary<String, Object>();

            try
            {
                // Получение входного потока для файла SessionState
                StorageFile file = await ApplicationData.Current.LocalFolder.GetFileAsync(sessionStateFilename);
                using (IInputStream inStream = await file.OpenSequentialReadAsync())
                {
                    // Десериализация состояния сеанса
                    DataContractSerializer serializer = new DataContractSerializer(typeof(Dictionary<string, object>), _knownTypes);
                    _sessionState = (Dictionary<string, object>)serializer.ReadObject(inStream.AsStreamForRead());
                }

                // Восстановление всех зарегистрированных фреймов к сохраненному состоянию
                foreach (var weakFrameReference in _registeredFrames)
                {
                    Frame frame;
                    if (weakFrameReference.TryGetTarget(out frame) && (string)frame.GetValue(FrameSessionBaseKeyProperty) == sessionBaseKey)
                    {
                        frame.ClearValue(FrameSessionStateProperty);
                        RestoreFrameNavigationState(frame);
                    }
                }
            }
            catch (Exception e)
            {
                throw new SuspensionManagerException(e);
            }
        }

        private static DependencyProperty FrameSessionStateKeyProperty =
            DependencyProperty.RegisterAttached("_FrameSessionStateKey", typeof(String), typeof(SuspensionManager), null);
        private static DependencyProperty FrameSessionBaseKeyProperty =
            DependencyProperty.RegisterAttached("_FrameSessionBaseKeyParams", typeof(String), typeof(SuspensionManager), null);
        private static DependencyProperty FrameSessionStateProperty =
            DependencyProperty.RegisterAttached("_FrameSessionState", typeof(Dictionary<String, Object>), typeof(SuspensionManager), null);
        private static List<WeakReference<Frame>> _registeredFrames = new List<WeakReference<Frame>>();

        /// <summary>
        /// Регистрация экземпляра <see cref="Frame"/>, чтобы обеспечить сохранение его журнала навигации в
        /// объекте <see cref="SessionState"/> и восстановления журнала из этого объекта.  Фреймы должны регистрироваться один раз
        /// сразу после создания, если планируется включить их в управление состоянием сеанса.  Если при
        /// регистрации состояние для указанного ключа уже было восстановлено,
        /// журнал навигации немедленно восстанавливается.  Последовательные вызовы
        /// <see cref="RestoreAsync"/> приведут к восстановлению журнала навигации.
        /// </summary>
        /// <param name="frame">Экземпляр, журнал навигации которого должен управляться диспетчером
        /// <see cref="SuspensionManager"/></param>
        /// <param name="sessionStateKey">Уникальный ключ в объекте <see cref="SessionState"/>, используемый для
        /// хранения данных, связанных с навигацией.</param>
        /// <param name="sessionBaseKey">Необязательный ключ, определяющий тип сеанса.
        /// Его можно использовать для различения нескольких сеансов запуска приложения.</param>
        public static void RegisterFrame(Frame frame, String sessionStateKey, String sessionBaseKey = null)
        {
            if (frame.GetValue(FrameSessionStateKeyProperty) != null)
            {
                throw new InvalidOperationException("Frames can only be registered to one session state key");
            }

            if (frame.GetValue(FrameSessionStateProperty) != null)
            {
                throw new InvalidOperationException("Frames must be either be registered before accessing frame session state, or not registered at all");
            }

            if (!string.IsNullOrEmpty(sessionBaseKey))
            {
                frame.SetValue(FrameSessionBaseKeyProperty, sessionBaseKey);
                sessionStateKey = sessionBaseKey + "_" + sessionStateKey;
            }

            // Свойство зависимостей используется для связывания сеансового ключа с фреймом и сохранения списка фреймов, состоянием навигации которых
            // необходимо управлять
            frame.SetValue(FrameSessionStateKeyProperty, sessionStateKey);
            _registeredFrames.Add(new WeakReference<Frame>(frame));

            // Проверяет возможность восстановления состояния навигации
            RestoreFrameNavigationState(frame);
        }

        /// <summary>
        /// Отменяет связь объекта <see cref="Frame"/>, ранее зарегистрированного с помощью <see cref="RegisterFrame"/>,
        /// с объектом <see cref="SessionState"/>.  Любое ранее записанное состояние навигации будет
        /// удалено.
        /// </summary>
        /// <param name="frame">Экземпляр, управление журналом навигации которого должно быть
        /// прекращено.</param>
        public static void UnregisterFrame(Frame frame)
        {
            // Удаление состояние сеанса и удаление фрейма из списка фреймов, состояние навигации которых
            // по-прежнему будет сохраняться (вместе со слабыми ссылками, которые перестанут быть доступными)
            SessionState.Remove((String)frame.GetValue(FrameSessionStateKeyProperty));
            _registeredFrames.RemoveAll((weakFrameReference) =>
            {
                Frame testFrame;
                return !weakFrameReference.TryGetTarget(out testFrame) || testFrame == frame;
            });
        }

        /// <summary>
        /// Предоставляет хранилище для состояния сеанса, связанного с указанным объектом <see cref="Frame"/>.
        /// Состояние сеанса фреймов, ранее зарегистрированных с помощью <see cref="RegisterFrame"/>,
        /// сохраняется и восстанавливается автоматически в составе глобального объекта
        /// <see cref="SessionState"/>.  Незарегистрированные фреймы имеют переходное состояние,
        /// которое, тем не менее, можно использовать при восстановлении страниц, удаленных из
        /// кэша навигации.
        /// </summary>
        /// <remarks>Приложения могут использовать <see cref="NavigationHelper"/> для управления
        /// состоянием, относящимся к странице, вместо непосредственного обращения к состоянию сеанса фрейма.</remarks>
        /// <param name="frame">Экземпляр, для которого требуется состояние сеанса.</param>
        /// <returns>Коллекция состояния, к которой применяется такой же механизм сериализации, как к объекту
        /// <see cref="SessionState"/>.</returns>
        public static Dictionary<String, Object> SessionStateForFrame(Frame frame)
        {
            var frameState = (Dictionary<String, Object>)frame.GetValue(FrameSessionStateProperty);

            if (frameState == null)
            {
                var frameSessionKey = (String)frame.GetValue(FrameSessionStateKeyProperty);
                if (frameSessionKey != null)
                {
                    // Зарегистрированные фреймы отражают соответствующее состояние сеанса
                    if (!_sessionState.ContainsKey(frameSessionKey))
                    {
                        _sessionState[frameSessionKey] = new Dictionary<String, Object>();
                    }
                    frameState = (Dictionary<String, Object>)_sessionState[frameSessionKey];
                }
                else
                {
                    // Незарегистрированные фреймы имеют переходное состояние
                    frameState = new Dictionary<String, Object>();
                }
                frame.SetValue(FrameSessionStateProperty, frameState);
            }
            return frameState;
        }

        private static void RestoreFrameNavigationState(Frame frame)
        {
            var frameState = SessionStateForFrame(frame);
            if (frameState.ContainsKey("Navigation"))
            {
                frame.SetNavigationState((String)frameState["Navigation"]);
            }
        }

        private static void SaveFrameNavigationState(Frame frame)
        {
            var frameState = SessionStateForFrame(frame);
            frameState["Navigation"] = frame.GetNavigationState();
        }
    }
    public class SuspensionManagerException : Exception
    {
        public SuspensionManagerException()
        {
        }

        public SuspensionManagerException(Exception e)
            : base("SuspensionManager failed", e)
        {

        }
    }
}
