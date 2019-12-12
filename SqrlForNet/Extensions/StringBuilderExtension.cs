using System.Text;

namespace SqrlForNet.Extensions
{
    internal static class StringBuilderExtension
    {
        internal static void AppendLine(this StringBuilder stringBuilder, string value, bool windowsStyle = false)
        {
            if (windowsStyle)
            {
                stringBuilder.Append($"{value}\r\n");
            }
            else
            {
                stringBuilder.AppendLine(value);
            }
        }
    }
}