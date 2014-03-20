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
    public static class CachingUtil
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

        private static string GetCachePathForUrl (string url, Context ctx)
        {
            return GetCachePathForUrl (url, "UserContent", ctx);
        }

        private static string GetCachePathForUrl (string url, String directoryName, Context ctx)
        {
            var hash = GetMd5Hash (url);
            return FilePath.Combine (ctx.ExternalCacheDir.AbsolutePath, directoryName, hash);
        }

        public static Bitmap GetBitmapFromCacheByUrl (string url, Context ctx)
        {
            try {
                var imagePath = GetCachePathForUrl (url, "UserImages", ctx) + ".png";
                return File.Exists (imagePath) ? BitmapFactory.DecodeFile (imagePath) : null;
            } catch (Exception ex) {
                var log = ServiceContainer.Resolve<Logger> ();
                log.Warning (LogTag, ex, "Failed to load bitmap from cache."); 
                return null;
            }
        }

        public static bool PutBitmapToCacheByUrl (string url, Bitmap bitmap, Context ctx)
        {
            var imagePath = GetCachePathForUrl (url, "UserImages", ctx);

            try {
                Directory.CreateDirectory (FilePath.GetDirectoryName (imagePath));
                using (var fileStream = new FileStream (imagePath + ".png", FileMode.Create)) {
                    bitmap.Compress (Bitmap.CompressFormat.Png, 90, fileStream);
                }
                return true;
            } catch (IOException ex) {
                var log = ServiceContainer.Resolve<Logger> ();
                if (ex.Message.StartsWith ("Sharing violation on")) {
                    // Treat FAT filesystem related failure as expected behaviour
                    log.Info (LogTag, ex, "Failed to save bitmap to cache.");
                } else {
                    log.Warning (LogTag, ex, "Failed to save bitmap to cache.");
                }
                return false;
            } catch (Exception ex) {
                var log = ServiceContainer.Resolve<Logger> ();
                log.Warning (LogTag, ex, "Failed to save bitmap to cache.");
                return false;
            }
        }
    }
}

