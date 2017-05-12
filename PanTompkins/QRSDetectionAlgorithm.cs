using System;
using System.Collections.Generic;
using System.IO;

namespace PanTompkins
{
	public static class QRSDetectionAlgorithm
	{

		public static int M = 5;
		public static int N = 30;
		public static int winSize;
		public static float HP_CONSTANT = (float)1 / M;
		public static string currentDirectory = System.IO.Directory.GetCurrentDirectory();

		/// <summary>
		/// Function to execute the algorithm and produce the result in the current file directory
		/// </summary>
		/// <returns>The execute.</returns>
		/// <param name="filepath">Filepath where the current file to be processed is residing.</param>
		/// <param name="position">Position of the column in which the heartrate information is saved in the file.</param>
		/// <param name="windowSize">Window size is the size of the window to consider for the Moving Window filter.</param>
		public static void execute(string filepath, int position, int windowSize = 230)
		{
			winSize = windowSize;
			List<String> data_list = readDataFromCSV(filepath, position);

			int nsamp = data_list.Count - 2;
			float[] ecg = new float[nsamp];
			for (int i = 2; i < nsamp; i++)
			{
				ecg[i - 2] = float.Parse(data_list[i]);
			}

			int[] QRS = detect(ecg);

			// Writing the detected data into the file
			try
			{
				var fw = File.OpenWrite(currentDirectory + "/QRS.csv");
				StreamWriter bw = new StreamWriter(fw);

				for (int i = 0; i < QRS.Length; i++)
				{
					bw.WriteLine(QRS[i].ToString());
				}
				bw.Close();

			}
			catch (IOException e)
			{
				Console.WriteLine(e.StackTrace);
			}

		}

		// Function to extract data from the CSV file
		public static List<string> readDataFromCSV(string filepath, int position)
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

						arrayList.Add(values[position]);
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

		// Detection Function for detecting the peaks 
		public static int[] detect(float[] ecg)
		{
			// circular buffer for input ecg signal
			// we need to keep a history of M + 1 samples for High Pass filter
			float[] ecg_circ_buff = new float[M + 1];
			int ecg_circ_WR_idx = 0;
			int ecg_circ_RD_idx = 0;

			// circular buffer for input ecg signal
			// we need to keep a history of N+1 samples for Low Pass filter
			float[] hp_circ_buff = new float[N + 1];
			int hp_circ_WR_idx = 0;
			int hp_circ_RD_idx = 0;

			// Low Pass filter outputs a single point for every input point
			// This goes straight to adaptive filtering for eval
			float next_eval_pt = 0;

			// output 
			int[] QRS = new int[ecg.Length];

			// running sums for High Pass and Low Pass filters, values shifted in FILO
			float hp_sum = 0;
			float lp_sum = 0;

			// parameters for adaptive thresholding
			double treshold = 0;
			Boolean triggered = false;
			int trig_time = 0;
			float win_max = 0;
			int win_idx = 0;

			for (int i = 0; i < ecg.Length; i++)
			{
				ecg_circ_buff[ecg_circ_WR_idx++] = ecg[i];
				ecg_circ_WR_idx %= (M + 1);

				/* High pass filtering */
				if (i < M)
				{
					// first fill buffer with enough points for High Pass filter
					hp_sum += ecg_circ_buff[ecg_circ_RD_idx];
					hp_circ_buff[hp_circ_WR_idx] = 0;
				}
				else
				{
					hp_sum += ecg_circ_buff[ecg_circ_RD_idx];

					int tmp = ecg_circ_RD_idx - M;
					if (tmp < 0)
					{
						tmp += M + 1;
					}
					hp_sum -= ecg_circ_buff[tmp];

					float y1 = 0;
					float y2 = 0;

					tmp = (ecg_circ_RD_idx - ((M + 1) / 2));
					if (tmp < 0)
					{
						tmp += M + 1;
					}
					y2 = ecg_circ_buff[tmp];

					y1 = HP_CONSTANT * hp_sum;

					hp_circ_buff[hp_circ_WR_idx] = y2 - y1;
				}

				ecg_circ_RD_idx++;
				ecg_circ_RD_idx %= (M + 1);

				hp_circ_WR_idx++;
				hp_circ_WR_idx %= (N + 1);

				/* Low pass filtering */

				// shift in new sample from high pass filter
				lp_sum += hp_circ_buff[hp_circ_RD_idx] * hp_circ_buff[hp_circ_RD_idx];

				if (i < N)
				{
					// first fill buffer with enough points for Low Pass filter
					next_eval_pt = 0;

				}
				else
				{
					// shift out oldest data point
					int tmp = hp_circ_RD_idx - N;
					if (tmp < 0)
					{
						tmp += N + 1;
					}
					lp_sum -= hp_circ_buff[tmp] * hp_circ_buff[tmp];

					next_eval_pt = lp_sum;
				}

				hp_circ_RD_idx++;
				hp_circ_RD_idx %= (N + 1);

				/* Adapative thresholding beat detection */
				// set initial threshold				
				if (i < winSize)
				{
					if (next_eval_pt > treshold)
					{
						treshold = next_eval_pt;
					}
				}

				// check if detection hold off period has passed
				if (triggered)
				{
					trig_time++;

					if (trig_time >= 100)
					{
						triggered = false;
						trig_time = 0;
					}
				}

				// find if we have a new max
				if (next_eval_pt > win_max) win_max = next_eval_pt;

				// find if we are above adaptive threshold
				if (next_eval_pt > treshold && !triggered)
				{
					QRS[i] = 1;

					triggered = true;
				}
				else
				{
					QRS[i] = 0;
				}

				// adjust adaptive threshold using max of signal found 
				// in previous window            
				if (++win_idx > winSize)
				{
					// weighting factor for determining the contribution of
					// the current peak value to the threshold adjustment
					double gamma = 0.175;

					Random random = new Random();

					// forgetting factor - 
					// rate at which we forget old observations
					double alpha = 0.01 + (random.Next() * ((0.1 - 0.01)));

					treshold = alpha * gamma * win_max + (1 - alpha) * treshold;

					// reset current window ind
					win_idx = 0;
					win_max = -10000000;
				}
			}

			return QRS;
		}
	}
}
