using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LightBuzz.Kinect2CSV.Test
{
    public class Definitions
    {
        public Int64 ID { get; set; }
        public string Gesture { get; set; }

        public Definitions() { }

        public Definitions(Int64 _ID, string _Gesture)
        {
            ID = _ID;
            Gesture = _Gesture;
        }

        public IEnumerable<Definitions> ReadCSV(string filename)
        {
            // We change file extension here to make sure it's a .csv file.
            // TODO: Error checking.

            if (!(File.Exists(System.IO.Path.ChangeExtension(filename, ".csv"))))
            {
                return null;
            }

            string[] lines = File.ReadAllLines(System.IO.Path.ChangeExtension(filename, ".csv"));

            if(lines == null)
            {
                return null;
            }

            // lines.Select allows me to project each line as a Person. 
            // This will give me an IEnumerable<Person> back.
            return lines.Select(line =>
            {
                string[] data = line.Split(',');
                // We return a person with the data in order.
                return new Definitions(Convert.ToInt64(data[0]), data[1]);
            });
        }
    }
}
