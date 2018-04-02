using System;

namespace EssayStorage.StaticClasses
{
    public static class Rand
    {
        static Random random;

        static Rand()
        {
            random = new Random();
        }

        public static string GetRandomEnglishLiteralString(int length)
        {
            string ans = "";
            for (int i = 0; i < length; i++)
                ans += (char)(random.Next(0, 2) == 0 ? 'a' + random.Next(0, 26) : 'A' + random.Next(0, 26));
            return ans;
        }
    }
}