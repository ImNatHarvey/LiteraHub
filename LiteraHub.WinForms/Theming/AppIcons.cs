using FontAwesome.Sharp;

namespace LiteraHub.WinForms.Theming;

// Every icon the app uses is chosen here. UNVERIFIED: the IconChar names below were written from memory.
// If one is underlined red, delete the part after "IconChar." and pick a similar name from IntelliSense.
public static class AppIcons
{
    public static IconChar Home => IconChar.Home;
    public static IconChar Books => IconChar.Book;
    public static IconChar Authors => IconChar.PenNib;
    public static IconChar Categories => IconChar.Tags;
    public static IconChar Students => IconChar.UserGraduate;
    public static IconChar BorrowReturn => IconChar.ExchangeAlt;
    public static IconChar Reports => IconChar.ChartBar;
    public static IconChar Users => IconChar.UsersCog;
    public static IconChar Logout => IconChar.SignOutAlt;
    public static IconChar Demo => IconChar.SlidersH;
}