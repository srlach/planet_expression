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
        public int ID { get; set; }
        public string Gesture { get; set; }

        public Definitions() { }

        public Definitions(int _ID, string _Gesture)
        {
            ID = _ID;
            Gesture = _Gesture;
        }

        public IEnumerable<Definitions> ReadCSV(string filename)
        {
            // We change file extension here to make sure it's a .csv file.
            // TODO: Error checking.
            string[] lines = File.ReadAllLines(System.IO.Path.ChangeExtension(filename, ".csv"));

            // lines.Select allows me to project each line as a Person. 
            // This will give me an IEnumerable<Person> back.
            return lines.Select(line =>
            {
                string[] data = line.Split(',');
                // We return a person with the data in order.
                return new Definitions(Convert.ToInt32(data[0]), data[1]);
            });
        }
    }
}
