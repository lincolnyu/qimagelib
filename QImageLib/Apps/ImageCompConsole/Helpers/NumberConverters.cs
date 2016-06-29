namespace ImageCompConsole.Helpers
{
    public static class NumberConverters
    {
        public static string ConvertToM(this int val)
        {
            const int m = 1024 * 1024;
            var d = val / m;
            if (d * m != val)
            {
                return string.Format("{0:0.0}", (double)val / m);
            }
            else
            {
                return d.ToString();
            }
        }
    }
}
