using Microsoft.Kinect;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;

namespace LightBuzz.Kinect2CSV
{
    public class KinectCSVManager
    {
        int _current = 0;

        bool _hasEnumeratedJoints = false;

        DataTable bodyData;
        List<KeyValuePair<string, string>> bodyPositions;
        List<string> jointTypes;

        public bool IsRecording { get; protected set; }

        public string Folder { get; protected set; }

        public string Result { get; protected set; }

        public void Start()
        {
            IsRecording = true;
            Folder = DateTime.Now.ToString("yyy_MM_dd_HH_mm_ss");

            Directory.CreateDirectory(Folder);
            bodyData = new DataTable();
            bodyData.TableName = "body_table";

            bodyPositions = new List<KeyValuePair<string, string>>();
            jointTypes = new List<string>();
        }

        public void Update(Body body)
        {
            if (!IsRecording) return;
            if (body == null || !body.IsTracked) return;

            string path = Path.Combine(Folder, _current.ToString() + ".line");

            using (StreamWriter writer = new StreamWriter(path))
            {
                StringBuilder line = new StringBuilder();

                Joint origin = body.Joints[JointType.SpineBase];

                if (!_hasEnumeratedJoints)
                {
                    foreach (var joint in body.Joints.Values)
                    {
                        if (joint != origin)
                        {
                            line.Append(string.Format("{0},,,", joint.JointType.ToString()));
                        }
                        string s = string.Format("{0},,,", joint.JointType.ToString());
                        string s_ = string.Format("{0}", joint.JointType.ToString());

                        bodyData.Columns.Add(s);
                        jointTypes.Add(s_);
                    }
                    line.AppendLine();

                    foreach (var joint in body.Joints.Values)
                    {
                        line.Append("X,Y,Z,");
                    }
                    line.AppendLine();

                    _hasEnumeratedJoints = true;
                }

                foreach (var joint in body.Joints.Values)
                {
                    if (joint != origin)
                    {
                        line.Append(string.Format("{0},{1},{2},", joint.Position.X - origin.Position.X, joint.Position.Y - origin.Position.Y, joint.Position.Z - origin.Position.Z));
                    }
                    string s = string.Format("{0},{1},{2},", joint.Position.X, joint.Position.Y, joint.Position.Z);
                    string s_ = string.Format("{0},{1},{2}", joint.Position.X, joint.Position.Y, joint.Position.Z);

                    bodyData.Rows.Add(s);
                    bodyPositions.Add(new KeyValuePair<string, string>(joint.JointType.ToString(), s_));
                }

                writer.Write(line);

                _current++;
            }
        }

        public void Stop()
        {
            IsRecording = false;
            _hasEnumeratedJoints = false;
            System.Diagnostics.Debug.WriteLine("I guess this might be the number of frames: " + _current);

            /* try this */
            int n_frames = 10;
            ILookup<string, string> lookup = bodyPositions.ToLookup(kvp => kvp.Key, kvp => kvp.Value);
            int incrementor = _current / n_frames;

            string joint_vector = "";
            foreach (string joints in jointTypes)
            {
                for (int i = 0; i < _current; i += incrementor)
                {
                    //joint_vector += lookup[joints].ElementAt(i);
                    //System.Diagnostics.Debug.Write(s);
                }
            }

            string _Result = "Dictionary.csv";
            using (StreamWriter file = new StreamWriter(_Result))
            {
                file.WriteLine(joint_vector);
            }

            Result = DateTime.Now.ToString("yyy_MM_dd_HH_mm_ss") + ".csv";

            using (StreamWriter writer = new StreamWriter(Result))
            {
                for (int index = 0; index < _current; index++)
                {
                    string path = Path.Combine(Folder, index.ToString() + ".line");

                    if (File.Exists(path))
                    {
                        string line = string.Empty;

                        using (StreamReader reader = new StreamReader(path))
                        {
                            line = reader.ReadToEnd();
                        }

                        writer.WriteLine(line);
                    }
                }
            }

            //bodyData.WriteXml("dtDataxml");
            Directory.Delete(Folder, true);
        }
    }
}
