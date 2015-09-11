using System.ComponentModel;
using System.Windows;
using System.Windows.Media.Imaging;

namespace EvoDistroLisa.UI
{
	/// <summary>
	/// Interaction logic for ImageViewer.xaml
	/// </summary>
	public partial class ImageViewer: INotifyPropertyChanged
	{
		private BitmapSource _image;

		public event PropertyChangedEventHandler PropertyChanged = delegate { };

		public ImageViewer()
		{
			InitializeComponent();
		}

		public BitmapSource Image
		{
			get { return _image; }
			set
			{
				_image = value;
				PropertyChanged(this, new PropertyChangedEventArgs("Image"));
			}
		}

		private void Window_Closing(object sender, CancelEventArgs e)
		{
			e.Cancel = true;
			WindowState = WindowState.Minimized;
		}
	}
}
