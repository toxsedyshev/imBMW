using System;
using Microsoft.SPOT;

namespace imBMW.Features.Menu
{
    public static class MenuHelpers
    {
        public static void AddBackButton(this MenuScreen screen, int index = -1)
        {
            screen.AddItem(new MenuItem("<<", MenuItemType.Button, MenuItemAction.GoBackScreen), index);
        }
    }
}
