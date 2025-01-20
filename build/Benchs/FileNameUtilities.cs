namespace build.Benchs
{
    internal static class FileNameUtilities
    {
        public static string StripeInvalidPathChars(this string path)
        {
            var invalidChars = Path.GetInvalidFileNameChars();
            
            // Replace with _
            
            foreach (var invalidChar in invalidChars)
            {
                path = path.Replace(invalidChar, '_');
            }
            
            return path;
        }
    }
}