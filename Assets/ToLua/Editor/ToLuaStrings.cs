
namespace LuaInterface.Editor
{
    static public class ToLuaStrings
    {
        static public string RemoveChar(string str, char c)
        {
            int index = str.IndexOf(c);

            while (index > 0)
            {
                str = str.Remove(index, 1);
                index = str.IndexOf(c);
            }

            return str;
        }
    }
}
