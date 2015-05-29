using Android.Content;

namespace Toggl.Joey.UI.Utils
{
    public static class DimensionsExtensions
    {
        public static int DpsToPxls (this int dps, Context ctx)
        {
            return (int) (ctx.Resources.DisplayMetrics.Density * dps + 0.5f);
        }
    }
}

