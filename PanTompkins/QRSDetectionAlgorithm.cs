using System;
using System.Collections.Generic;
using System.IO;

namespace PanTompkins
{
	public static class QRSDetectionAlgorithm
	{
		public static string currentDirectory = System.IO.Directory.GetCurrentDirectory();
		public static int M = 5;

		public static void doSomething(string filepath)
		{
			List<string> data_list = readDataFromCSV(filepath);

			int nsamp = data_list.Count - 2;
			float[] sig0 = new float[nsamp];
			for (int i = 2; i < nsamp; i++)
			{
				sig0[i - 2] = float.Parse(data_list[i]);
			}

			float[] highPass = highPassFilter(sig0, nsamp);

			//highpass csv
			try
			{
				StreamWriter bw = new StreamWriter( currentDirectory + "highpass.csv");

				for (int i = 0; i < highPass.Length; i++)
				{
					bw.WriteLine(Convert.ToString(highPass[i]));
				}
				bw.Close();

			}
			catch (IOException e)
			{
				Console.WriteLine(e.StackTrace);
			}

			float[] lowPass = lowPassFilter(highPass, nsamp);

			//bandpass csv
			try
			{
				StreamWriter bw = new StreamWriter(currentDirectory + "band_pass.csv");

				for (int i = 0; i < lowPass.Length; i++)
				{
					bw.WriteLine(Convert.ToString(lowPass[i]));
				}
				bw.Close();

			}
			catch (IOException e)
			{
				Console.WriteLine(e.StackTrace);
			}
		}

		public static List<string> readDataFromCSV(string filepath)
		{
			List<string> arrayList = null;
			try
			{

				int line_number = 0;

				using (var fs = File.OpenRead(@filepath))
				using (var reader = new StreamReader(fs))
				{
					arrayList = new List<string>();

					while (!reader.EndOfStream)
					{
						var line = reader.ReadLine();
						var values = line.Split(',');

						arrayList.Add(values[4]);
						line_number++;
					}
				}
			}
			catch (FileNotFoundException e)
			{
				Console.WriteLine(e.Message);
				Console.WriteLine(e.StackTrace);
			}
			catch (IOException e) {
				Console.WriteLine(e.Message);
				Console.WriteLine(e.StackTrace);
			}

			return arrayList;
		}

		/// <summary>
		/// HighPass Filter method for the QRS Detection Algorithm
		/// </summary>
		/// <param name="sig0">Sig0.</param>
		/// <param name="nsamp">Nsamp.</param>
		public static float[] highPassFilter(float[] sig0, int nsamp)
		{ 
			float[] highPass = new float[nsamp];
			float constant = (float)1 / M;

			for (int i = 0; i < sig0.Length; i++)
			{ 
				float y1 = 0;
				float y2 = 0;

				int y2_index = i - ((M + 1) / 2);
				if (y2_index < 0)
				{ 
					y2_index = nsamp + y2_index;
				}
				y2 = sig0[y2_index];

				float y1_sum = 0;
				for (int j = i; j > i - M; j--)
				{
					int x_index = i - (i - j);
					if (x_index < 0)
					{
						x_index = nsamp + x_index;
					}
					y1_sum += sig0[x_index];
				}

				y1 = constant * y1_sum;
				highPass[i] = y2 - y1;
			}

			return highPass;
		}

		/// <summary>
		/// LowPass Filter method for the QRS Detection Algorithm
		/// </summary>
		/// <param name="sig0">Sig0.</param>
		/// <param name="nsamp">Nsamp.</param>
		public static float[] lowPassFilter(float[] sig0, int nsamp)
		{
			float[] lowPass = new float[nsamp];
			for (int i = 0; i < sig0.Length; i++)
			{
				float sum = 0;
				if (i + 30 < sig0.Length)
				{
					for (int j = i; j < i + 30; j++)
					{
						float current = sig0[j] * sig0[j]; 
						sum += current;
					}
				}
				else if (i + 30 >= sig0.Length)
				{ 
					int over = i + 30 - sig0.Length;
					for (int j = i; j < sig0.Length; j++)
					{
						float current = sig0[j] * sig0[j];
						sum += current;
					}
					for (int j = 0; j < over; j++)
					{
						float current = sig0[j] * sig0[j];
						sum += current;
					}
				}

				lowPass[i] = sum;
			}

			return lowPass;

		}

		//public static int[] QRSDetection(float[] lowPass, int nsamp)
		//{
		//	int[] QRS = new int[nsamp];

		//	double threshold = 0;

		//	for (int i = 0; i < 200; i++)
		//	{
		//		if (lowPass[i] > threshold)
		//		{
		//			threshold = lowPass[i];
		//		}
		//	}

		//	int frame = 250;

		//	for (int i = 0; i < lowPass.Length; i += frame)
		//	{ 
		//		float max = 0;
		//		int index = 0;
		//		if (i + frame > lowPass.Length)
		//		{ 
		//			index = lowPass.Length;
		//		}
		//		else
		//		{
		//			index = i + frame;
		//		}
		//		for (int j = i; j < index; j++)
		//		{
		//			if (lowPass[j] > max) max = lowPass[j]; 
		//		}
		//		Boolean added = false;
		//		for (int j = i; j < index; j++)
		//		{
		//			if (lowPass[j] > threshold && !added)
		//			{
		//				QRS[j] = 1; 

		//				added = true;
		//			}
		//			else
		//			{
		//				QRS[j] = 0;
		//			}
		//		}
		//		Random random = new Random();

		//		double gama = ( random.Next() > 0.5) ? 0.15 : 0.20;
		//		double alpha = 0.01 + ( random.Next()  * ((0.1 - 0.01)));

		//		threshold = alpha * gama * max + (1 - alpha) * threshold;
		//	}

		//	return QRS;
		//}



	}

}

