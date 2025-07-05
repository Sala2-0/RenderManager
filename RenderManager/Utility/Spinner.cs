namespace RenderManager.Utility;

internal class Spinner
{
    private static readonly char[] SpinnerChars = ['|', '/', '-', '\\'];

    public static char Spin(int index) => SpinnerChars[index];
}