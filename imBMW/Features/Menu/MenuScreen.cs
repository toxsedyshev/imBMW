using System;
using Microsoft.SPOT;
using System.Collections;
using imBMW.Tools;

namespace imBMW.Features.Menu
{
    public delegate void MenuScreenEventHandler(MenuScreen screen);

    public delegate void MenuScreenItemEventHandler(MenuScreen screen, MenuItem item);

    public class MenuScreen
    {
        string title;
        string status;
        bool updateSuspended;

        public MenuScreen()
        {
            Items = new ArrayList();
        }

        public string Title
        {
            get
            {
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
                Logger.Error("Can't add menu item \"" + menuItem + "\" with index=" + index + ", count=" + ItemsCount);
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
                Items.Capacity = System.Math.Max(Items.Capacity, index + 1);
                Items.Insert(index, menuItem);
            }
            menuItem.Changed += menuItem_Changed;
            menuItem.Clicked += menuItem_Clicked;
            OnUpdated();
        }

        void UnsubscribeItem(MenuItem item)
        {
            if (item == null)
            {
                return;
            }
            item.Clicked -= menuItem_Clicked;
            item.Changed -= menuItem_Changed;
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

        public bool UpdateSuspended
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

        void menuItem_Clicked(MenuItem item)
        {
            OnItemClicked(item);
        }

        void menuItem_Changed(MenuItem item)
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
