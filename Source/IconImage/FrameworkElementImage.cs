using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace IconImage
{
	public class FrameworkElementImage
	{
		/// <summary>
		/// Saves the image of a specified FrameworkElement to file in PNG format.
		/// </summary>
		/// <param name="source">FrameworkElement</param>
		/// <param name="filePath">File path</param>
		/// <param name="width">Width of saved image (optional)</param>
		/// <param name="height">Height of saved image (optional)</param>
		public static void SaveImage(FrameworkElement source, string filePath, double width = 0D, double height = 0D)
		{
			if (source is null)
				throw new ArgumentNullException(nameof(source));
			if (string.IsNullOrWhiteSpace(filePath))
				throw new ArgumentNullException(nameof(filePath));

			var rtb = new RenderTargetBitmap(
				(int)source.Width,
				(int)source.Height,
				96D, 96D,
				PixelFormats.Pbgra32);

			rtb.Render(source);

			var encoder = new PngBitmapEncoder();
			encoder.Frames.Add(BitmapFrame.Create(rtb));

			if ((0 < width) || (0 < height))
			{
				var bi = new BitmapImage();

				using (var ms = new MemoryStream())
				{
					encoder.Save(ms);
					ms.Seek(0, SeekOrigin.Begin);

					bi.BeginInit();
					bi.CacheOption = BitmapCacheOption.OnLoad;
					bi.StreamSource = ms;

					if (0 < width)
						bi.DecodePixelWidth = (int)width;
					if (0 < height)
						bi.DecodePixelHeight = (int)height;

					bi.EndInit();
				}

				encoder = new PngBitmapEncoder(); // Save method cannot be used twice.
				encoder.Frames.Add(BitmapFrame.Create(bi));
			}

			using (var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite))
			{
				encoder.Save(fs);
			}
		}
	}
}