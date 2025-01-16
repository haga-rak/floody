namespace floody
{
    public static class FormatHelper
    {
        public static string FormatBytes(double bytes)
        {
            string[] suffix = ["B", "KB", "MB", "GB", "TB"];
            int i;
            double dblSByte = bytes;

            for (i = 0; i < suffix.Length && bytes >= 1024; i++, bytes /= 1024)
            {
                dblSByte = bytes / 1024.0;
            }

            return $"{dblSByte:0.##} {suffix[i]}";
        }
    }
}