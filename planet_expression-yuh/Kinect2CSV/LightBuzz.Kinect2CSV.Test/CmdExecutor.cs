using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LightBuzz.Kinect2CSV.Test
{
    class CmdExecutor
    {
        private List<Int64> predictions = null;

        public List<Int64> Result
        {
            get
            {
                return predictions;
            }
        }

        public void Train(string tPath)
        {
            var proc = new System.Diagnostics.Process
            {
                StartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = tPath,
                    Arguments = "Dictionary.csv",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                }
            };

            proc.Start();

            while (!proc.StandardOutput.EndOfStream)
            {
                // read line by line from standard output
                string line = proc.StandardOutput.ReadLine();
                System.Diagnostics.Debug.Write(line + "\n");
            }
        }

        public void Classify(string cPath, string csv)
        {
            var proc = new System.Diagnostics.Process
            {
                StartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = cPath,
                    Arguments = csv,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                }
            };

            proc.Start();

            while (!proc.StandardOutput.EndOfStream)
            {
                // read line by line from standard output
                string line = proc.StandardOutput.ReadLine();

                System.Diagnostics.Debug.Write(line + "\n");
                string key = "PredictedClassLabel: ";
                int pos = line.IndexOf(key);

                if (pos > 0)
                {
                    string substr = line.Substring(pos + (key.Length));
                    string[] id = substr.Split(null);

                    predictions = new List<Int64>
                    {
                        //Convert.ToInt64(line.ElementAt(pos + (key.Length)))
                        Convert.ToInt64(id[0])
                    };
                }
            }
        }
    }
}
