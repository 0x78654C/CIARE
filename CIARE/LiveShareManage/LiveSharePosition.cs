using System;
using System.Globalization;

namespace CIARE.LiveShareManage
{
    internal static class LiveSharePosition
    {
        internal const int MaxNicknameLength = 32;

        internal static bool TryNormalizeNickname(string value, out string nickname)
        {
            nickname = (value ?? string.Empty).Trim();
            if (nickname.Length == 0 || nickname.Length > MaxNicknameLength)
                return false;
            foreach (char character in nickname)
            {
                if (char.IsControl(character))
                    return false;
            }
            return true;
        }

        internal static string Encode(int line, int column, string nickname)
        {
            // The first two fields remain readable by older clients. At most 54
            // UTF-16 characters, below the existing hub's 64-character limit.
            string name = TryNormalizeNickname(nickname, out string normalized) ? normalized : "Guest";
            return string.Format(CultureInfo.InvariantCulture, "{0}|{1}|{2}",
                Math.Max(0, line), Math.Max(0, column), name);
        }

        internal static bool TryDecode(string value, out int line, out int column, out string nickname)
        {
            line = column = 0;
            nickname = string.Empty;
            if (string.IsNullOrEmpty(value) || value.Length > 64)
                return false;
            string[] parts = value.Split(new[] { '|' }, 3);
            if (parts.Length < 2 ||
                !int.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out line) ||
                !int.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out column))
                return false;
            if (parts.Length == 3 && TryNormalizeNickname(parts[2], out string normalized))
                nickname = normalized;
            return true;
        }
    }
}
