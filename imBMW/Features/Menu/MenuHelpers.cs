using imBMW.Features.Localizations;

namespace imBMW.Features.Menu
{
    public static class MenuHelpers
    {
        public static void AddBackButton(this MenuScreen screen, int index = -1)
        {
            screen.AddItem(new MenuItem(i => "« " + Localization.Current.Back, MenuItemType.Button, MenuItemAction.GoBackScreen), index);
        }
    }
}
