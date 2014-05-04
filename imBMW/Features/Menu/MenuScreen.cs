using System;
using Microsoft.SPOT;
using System.Collections;
using imBMW.Tools;

namespace imBMW.Features.Menu
{
    public delegate void MenuScreenEventHandler(MenuScreen screen);

    public delegate void MenuScreenItemEventHandler(MenuScreen screen, MenuItem item);

    public delegate string MenuScreenGetTextHandler(MenuScreen screen);

    public class MenuScreen
    {
        string title;
        string status;
        bool updateSuspended;
        MenuBase parentMenu;

        public MenuScreen(string title = null)
            : this()
        {
            if (title != null)
            {
                Title = title;
            }
        }

        public MenuScreen(MenuScreenGetTextHandler titleCallback = null)
            : this()
        {
            TitleCallback = titleCallback;
        }

        public MenuScreen()
        {
            Items = new ArrayList();
        }

        public MenuScreenGetTextHandler TitleCallback { get; set; }

        public string Title
        {
            get
            {
                if (TitleCallback != null)
                {
                    title = TitleCallback(this);
                }
                return title;
            }
            set
            {
                if (title == value)
                {
                    return;
                }
                title = value;
                OnUpdated();
            }
        }

        public string Status
        {
            get
            {
                return status;
            }
            set
            {
                if (status == value)
                {
                    return;
                }
                status = value;
                OnUpdated();
            }
        }

        protected ArrayList Items { get; private set; }

        public int ItemsCount
        {
            get
            {
                return Items.Count;
            }
        }

        public MenuItem GetItem(int index)
        {
            if (index >= 0 && index < Items.Count)
            {
                return Items[index] as MenuItem;
            }
            return null;
        }

        public void AddItem(MenuItem menuItem, int index = -1)
        {
            if (index > 9 || index < 0 && ItemsCount == 10)
            {
                Logger.Error("Can't add screen item \"" + menuItem + "\" with index=" + index + ", count=" + ItemsCount);
                index = 9;
            }
            if (index < 0)
            {
                Items.Add(menuItem);
            }
            else
            {
                if (index < Items.Count)
                {
                    UnsubscribeItem(Items[index] as MenuItem);
                    Items.RemoveAt(index);
                }
                while (index > Items.Count)
                {
                    Items.Add(null);
                }
                Items.Insert(index, menuItem);
            }
            menuItem.Changed += menuItem_Changed;
            menuItem.Clicked += menuItem_Clicked;
            OnUpdated();
        }

        public void ClearItems()
        {
            if (Items.Count > 0)
            {
                foreach (var i in Items)
                {
                    UnsubscribeItem(i as MenuItem);
                }
            }
            Items.Clear();
            OnUpdated();
        }

        public virtual bool OnNavigatedTo(MenuBase menu)
        {
            if (parentMenu == menu)
            {
                return false;
            }
            if (parentMenu != null)
            {
                throw new Exception("Already navigated to screen " + this + " in another menu " + parentMenu + ". Can't navigate in " + menu);
            }
            parentMenu = menu;

            var e = NavigatedTo;
            if (e != null)
            {
                e(this);
            }

            return true;
        }

        public virtual bool OnNavigatedFrom(MenuBase menu)
        {
            if (parentMenu == menu)
            {
                parentMenu = null;

                var e = NavigatedFrom;
                if (e != null)
                {
                    e(this);
                }

                return true;
            }
            if (parentMenu != null)
            {
                throw new Exception("Navigated to screen " + this + " in another menu " + parentMenu + ". Can't navigate from in " + menu);
            }
            return false;
        }

        public override string ToString()
        {
            return Title;
        }

        public void WithUpdateSuspended(MenuScreenEventHandler callback)
        {
            IsUpdateSuspended = true;
            callback(this);
            IsUpdateSuspended = false;
        }

        /// <summary>
        /// Menu navigated to this screen and screen is not suspended (screen, but not screen update).
        /// </summary>
        public bool IsNavigated
        {
            get
            {
                return parentMenu != null;
            }
        }

        /// <summary>
        /// Is screen update suspended, eg. for batch update.
        /// </summary>
        public bool IsUpdateSuspended
        {
            get
            {
                return updateSuspended;
            }
            set
            {
                if (updateSuspended == value)
                {
                    return;
                }
                updateSuspended = value;
                if (!value)
                {
                    OnUpdated();
                }
            }
        }

        public event MenuScreenItemEventHandler ItemClicked;

        public event MenuScreenEventHandler Updated;

        public event MenuScreenEventHandler NavigatedTo;

        public event MenuScreenEventHandler NavigatedFrom;

        protected void UnsubscribeItem(MenuItem item)
        {
            if (item == null)
            {
                return;
            }
            item.Clicked -= menuItem_Clicked;
            item.Changed -= menuItem_Changed;
        }

        protected void menuItem_Clicked(MenuItem item)
        {
            OnItemClicked(item);
        }

        protected void menuItem_Changed(MenuItem item)
        {
            OnUpdated();
        }

        protected void OnUpdated()
        {
            if (updateSuspended)
            {
                return;
            }
            var e = Updated;
            if (e != null)
            {
                e(this);
            }
        }

        protected void OnItemClicked(MenuItem item)
        {
            var e = ItemClicked;
            if (e != null)
            {
                e(this, item);
            }
        }
    }
}
