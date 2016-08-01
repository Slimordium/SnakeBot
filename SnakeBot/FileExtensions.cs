using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Streams;

namespace SnakeBot
{
    internal static class FileExtensions
    {
        internal static async Task<string> ReadStringFromFile(this string filename)
        {
            var text = string.Empty;

            try
            {
                //This code works on the PI
                //var appFolder = Windows.ApplicationModel.Package.Current.InstalledLocation;

                //Debug.WriteLine($"AppFolder {appFolder.Path}");

                //var storageFile = await appFolder.GetFileAsync(filename);
                //var stream = await storageFile.OpenAsync(FileAccessMode.Read);
                //var buffer = new Windows.Storage.Streams.Buffer((uint)stream.Size);

                //await stream.ReadAsync(buffer, (uint)stream.Size, InputStreamOptions.None);

                //if (buffer.Length > 0)
                //    text = Encoding.UTF8.GetString(buffer.ToArray());

                //Not sure if this code works on the PI
                var file = await ApplicationData.Current.LocalFolder.CreateFileAsync(filename, CreationCollisionOption.OpenIfExists).AsTask();

                Debug.WriteLine($"Reading {file.Path}");

                using (var stream = await file.OpenStreamForReadAsync())
                {
                    var buffer = new byte[stream.Length];

                    stream.Position = 0;
                    await stream.ReadAsync(buffer, 0, Convert.ToInt32(stream.Length));
                    
                    if (buffer.Length > 0)
                        text = Encoding.UTF8.GetString(buffer);

                }
            }
            catch (Exception e)
            {
                Debug.WriteLine($"Read failed {filename}, {e}");
            }

            return text;
        }

        //https://github.com/Microsoft/Windows-universal-samples/blob/master/Samples/ApplicationData/cs/Scenario1_Files.xaml.cs
        internal static async Task SaveStringToFile(string filename, string content)
        {
            var bytesToAppend = Encoding.UTF8.GetBytes(content.ToCharArray());
           
            try
            {
                var file = await ApplicationData.Current.LocalFolder.CreateFileAsync(filename, CreationCollisionOption.OpenIfExists).AsTask();

                Debug.WriteLine($"Writing {file.Path}");

                using (var stream = await file.OpenStreamForWriteAsync())
                {
                    stream.Position = stream.Length;
                    stream.Write(bytesToAppend, 0, bytesToAppend.Length);
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine($"Save failed {filename}, {e}");
            }

        }
    }
}