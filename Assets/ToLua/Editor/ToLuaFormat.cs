
public static class ToLuaFormat
{
    public static string GetIndent(int indentLevel)
    {
        var indent = string.Empty;
        for (int i = 0; i < indentLevel; ++i)
            indent += "\t";
        return indent;
    }
}

