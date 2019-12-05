using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Media;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;

namespace IconImage
{
	public partial class MainWindow : Window
	{
		#region Property

		public double ImageLength
		{
			get { return (double)GetValue(ImageLengthProperty); }
			set { SetValue(ImageLengthProperty, value); }
		}
		public static readonly DependencyProperty ImageLengthProperty =
			DependencyProperty.Register(
				"ImageLength",
				typeof(double),
				typeof(MainWindow),
				new PropertyMetadata(256D));

		#endregion

		public MainWindow()
		{
			InitializeComponent();
		}

		private void Save(object sender, RoutedEventArgs e) => SaveImage();

		private string _fileName = "Icon.png";
		private string _folderPath;

		public void SaveImage()
		{
			var sfd = new SaveFileDialog
			{
				FileName = _fileName,
				Filter = "*(.png)|*.png",
				InitialDirectory = _folderPath ?? Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
			};

			if (sfd.ShowDialog() != true)
				return;

			_fileName = Path.GetFileName(sfd.FileName);
			_folderPath = Path.GetDirectoryName(sfd.FileName);

			FrameworkElementImage.SaveImage(ImageContent, sfd.FileName, ImageLength, ImageLength);

			SystemSounds.Asterisk.Play();
		}
	}
}