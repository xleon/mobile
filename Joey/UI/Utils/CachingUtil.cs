using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Android.Content;
using Android.Graphics;
using Toggl.Phoebe;
using XPlatUtils;
using FilePath = System.IO.Path;

namespace Toggl.Joey.UI.Utils
{
    public class CachingUtil
    {
        private static readonly string LogTag = "CachingUtil";

        private static string GetMd5Hash (string input)
        {
            var md5Hash = MD5.Create ();
            // Convert the input string to a byte array and compute the hash. 
            byte[] data = md5Hash.ComputeHash (Encoding.UTF8.GetBytes (input));

            // Create a new Stringbuilder to collect the bytes 
            // and create a string.
            StringBuilder sBuilder = new StringBuilder ();

            // Loop through each byte of the hashed data  
            // and format each one as a hexadecimal string. 
            for (int i = 0; i < data.Length; i++) {
                sBuilder.Append (data [i].ToString ("x2"));
            }

            // Return the hexadecimal string. 
            return sBuilder.ToString ();
        }

        private static string GetCachePath (string url, Context ctx)
        {
            var hash = GetMd5Hash (url);
            return FilePath.Combine (ctx.ExternalCacheDir.AbsolutePath, "UserImageryContent", hash);
        }

        public static Bitmap GetBitmapFromCache (string url, Context ctx)
        {
            var imagePath = GetCachePath (url, ctx) + ".png";
            if (File.Exists (imagePath)) {
                return BitmapFactory.DecodeFile (imagePath);
            } else {
                return null;
            }
        }

        public static bool PutBitmapToCache (string url, Bitmap bitmap, Context ctx)
        {
            var imagePath = GetCachePath (url, ctx);

            try {
                Directory.CreateDirectory (FilePath.GetDirectoryName (imagePath));
                var fileStream = new FileStream (imagePath + ".png", FileMode.Create);
                bitmap.Compress (Bitmap.CompressFormat.Png, 90, fileStream);
                fileStream.Close ();
                return true;
            } catch (Exception ex) {
                var log = ServiceContainer.Resolve<Logger> ();
                log.Warning (LogTag, ex, "Failed to save stream to cache."); 
                return false;
            }
        }
    }
}

