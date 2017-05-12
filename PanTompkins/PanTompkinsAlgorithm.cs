using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;


namespace PanTompkins
{
	public static class PanTompkinsAlgorithm
	{
		private static double PEAK = 0;
		private static double SPKI = 0;
		private static double NPKI = 0;
		private static double RRAVERAGE1;
		private static double RRAVERAGE2;
		private static double RR_LOW_LIMIT;
		private static double RR_HIGH_LIMIT;
		private static double RR_MISSED_LIMIT;
		private static double TRESHOLD1 = 0;
		private static double TRESHOLD2 = 0;
		private static int lastQRS = 0;
		
		public static List<string> readDataFromCSV(string filepath, int location)
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

						arrayList.Add(values[location]);
						line_number++;
					}
				}
			}
			catch (FileNotFoundException e)
			{
				Console.WriteLine(e.Message);
				Console.WriteLine(e.StackTrace);
			}
			catch (IOException e)
			{
				Console.WriteLine(e.Message);
				Console.WriteLine(e.StackTrace);
			}


			return arrayList;
		}

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

		public static float[] squareFilter(float[] sig0, int nsamp) {
			float[] squarePass = new float[nsamp];

			for (int i = 0; i < sig0.Length; i++) {
				squarePass[i] = sig0[i] * sig0[i];
			}
			return squarePass;
		}

		public static void movingWindowIntegration() { 
			
		}
	}

}
