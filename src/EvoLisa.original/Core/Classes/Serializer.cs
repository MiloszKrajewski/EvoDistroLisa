﻿using GenArt.Core.AST;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Windows.Forms;
using System.Xml.Serialization;

namespace GenArt.Core.Classes
{
	public static class Serializer
	{
		public static void Serialize(Settings settings)
		{
			if (string.IsNullOrEmpty(Application.LocalUserAppDataPath))
				return;

			string fileName = Application.LocalUserAppDataPath + "\\Settings.xml";
			Serialize(settings, fileName);
		}

		public static void Serialize(Settings settings, string fileName)
		{
			if (fileName == null)
				return;

			try
			{
				var serializer = new XmlSerializer(settings.GetType());
				using (var writer = new FileStream(fileName, FileMode.Create))
				{
					serializer.Serialize(writer, settings);
				}
			}
			catch
			{
				// do nothing
			}
		}

		public static void Serialize(DnaDrawing drawing, string fileName)
		{
			if (fileName == null)
				return;

			if (fileName.EndsWith("xml"))
			{
				try
				{
					var serializer = new XmlSerializer(drawing.GetType());
					using (var writer = new FileStream(fileName, FileMode.Create))
					{
						serializer.Serialize(writer, drawing);
					}
				}
				catch
				{
					// do nothing
				}
			}
			else
			{
				SerializeBinary(drawing, fileName);
			}
		}

		public static void SerializeBinary(DnaDrawing drawing, string fileName)
		{
			if (fileName == null)
				return;

			try
			{
				var formatter = new BinaryFormatter();
				using (var writer = new FileStream(fileName, FileMode.Create))
				{
					formatter.Serialize(writer, drawing);
				}
			}
			catch
			{
				// do nothing
			}
		}

		public static Settings DeserializeSettings()
		{
			if (string.IsNullOrEmpty(Application.LocalUserAppDataPath))
				return null;

			string fileName = Application.LocalUserAppDataPath + "\\Settings.xml";
			return DeserializeSettings(fileName);
		}

		public static Settings DeserializeSettings(string fileName)
		{
			if (!File.Exists(fileName))
				return null;

			try
			{
				var serializer = new XmlSerializer(typeof(Settings));
				using (var reader = new FileStream(fileName, FileMode.Open))
				{
					return (Settings)serializer.Deserialize(reader);
				}
			}
			catch
			{
				// do nothing
			}
			return null;
		}

		public static DnaDrawing DeserializeDnaDrawing(string fileName)
		{
			if (!File.Exists(fileName))
				return null;

			if (!fileName.EndsWith("xml"))
				return DeserializeDnaDrawingBinary(fileName);

			try
			{
				var serializer = new XmlSerializer(typeof(DnaDrawing));
				using (var reader = new FileStream(fileName, FileMode.Open))
				{
					return (DnaDrawing)serializer.Deserialize(reader);
				}
			}
			catch
			{
				// do nothing
			}
			return null;
		}

		public static DnaDrawing DeserializeDnaDrawingBinary(string fileName)
		{
			if (!File.Exists(fileName))
				return null;

			try
			{
				var formatter = new BinaryFormatter();
				using (var reader = new FileStream(fileName, FileMode.Open))
				{
					return (DnaDrawing)formatter.Deserialize(reader);
				}
			}
			catch
			{
				// do nothing
			}
			return null;
		}
	}
}
