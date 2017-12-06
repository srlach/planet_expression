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
    public class Positions
    {
        public double previousX { get; set; }
        public double previousY { get; set; }
        public double previousZ { get; set; }

        public Positions(double x, double y, double z)
        {
            previousX = x;
            previousY = y;
            previousZ = z;
        }
    }

    public class KinectCSVManager
    {
        int _current = 0;

        bool _hasEnumeratedJoints = false;

        DataTable bodyData;
        List<KeyValuePair<string, string>> bodyPositions;
        List<string> jointTypes;

        List<JointType> omitJoints = new List<JointType>
        {
            JointType.KneeLeft,
            JointType.KneeRight,
            JointType.HipLeft,
            JointType.HipRight,
            JointType.AnkleLeft,
            JointType.AnkleRight,
            JointType.AnkleLeft,
            JointType.FootLeft,
            JointType.FootRight,
            JointType.Head,
            JointType.Neck,
            JointType.SpineBase,
            JointType.SpineMid,
            JointType.SpineShoulder,
            JointType.ThumbLeft,
            JointType.ThumbRight
        };

        double speedX;
        double speedY;
        double speedZ;

        private string joint_vector;

        bool isFirst = true;

        Dictionary<JointType, Positions> previousFrames;

        public bool IsRecording { get; protected set; }

        public string Folder { get; protected set; }

        public string Result { get; protected set; }

        public string Result_
        {
            get
            {
                return joint_vector;
            }
        }

        public void Reset()
        {
            _current = 0;

            speedX = 0;
            speedY = 0;
            speedZ = 0;

            isFirst = true;
        }

        public void Start()
        {
            IsRecording = true;
            Folder = DateTime.Now.ToString("yyy_MM_dd_HH_mm_ss");

            Directory.CreateDirectory(Folder);
            bodyData = new DataTable();
            bodyData.TableName = "body_table";

            bodyPositions = new List<KeyValuePair<string, string>>();
            jointTypes = new List<string>();


            previousFrames = new Dictionary<JointType, Positions>();
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
                        if (!omitJoints.Contains(joint.JointType))
                        {

                            line.Append(string.Format("{0},,,,,,,,,,", joint.JointType.ToString()));
                            string s_ = string.Format("{0}", joint.JointType.ToString());
                            jointTypes.Add(s_);

                            string s = string.Format("{0},,,,,,,,,,", joint.JointType.ToString());

                            bodyData.Columns.Add(s);
                        }
                    }
                    line.AppendLine();

                    foreach (var joint in body.Joints.Values)
                    {
                        if (!omitJoints.Contains(joint.JointType))
                        {
                            line.Append("X,Y,Z,v(X),v(Y),v(Z),|v|,alpha,beta,gamma,");
                        }
                    }
                    line.AppendLine();

                    _hasEnumeratedJoints = true;
                }


                foreach (var joint in body.Joints.Values)
                {
                    if (!omitJoints.Contains(joint.JointType))
                    {
                        if (isFirst)
                        {
                            previousFrames.Add(joint.JointType, new Positions(0, 0, 0));
                            speedX = 0;
                            speedY = 0;
                            speedZ = 0;
                        }
                        else
                        {

                            speedX = (joint.Position.X - previousFrames[joint.JointType].previousX) * 30.0;
                            speedY = (joint.Position.Y - previousFrames[joint.JointType].previousY) * 30.0;
                            speedZ = (joint.Position.Z - previousFrames[joint.JointType].previousZ) * 30.0;
                        }

                        double mag = Math.Sqrt(Math.Pow(speedX, 2) + Math.Pow(speedY, 2) + Math.Pow(speedZ, 2));

                        double alpha = 0;
                        double beta = 0;
                        double gamma = 0;

                        if (mag > 0)
                        {
                            alpha = Math.Acos(speedX / mag);
                            beta = Math.Acos(speedY / mag);
                            gamma = Math.Acos(speedZ / mag);
                        }

                        line.Append(string.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},", 
                            joint.Position.X - origin.Position.X, joint.Position.Y - origin.Position.Y, joint.Position.Z - origin.Position.Z, 
                            speedX, speedY, speedZ,
                            mag,
                            alpha, beta, gamma));                                

                        string s_ = string.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},",
                            joint.Position.X - origin.Position.X, joint.Position.Y - origin.Position.Y, joint.Position.Z - origin.Position.Z,
                            speedX, speedY, speedZ,
                            mag,
                            alpha, beta, gamma);

                        bodyPositions.Add(new KeyValuePair<string, string>(joint.JointType.ToString(), s_));

                        //previousX = joint.Position.X;
                        //previousY = joint.Position.Y;
                        //previousZ = joint.Position.Z;

                        previousFrames[joint.JointType] = new Positions(joint.Position.X, joint.Position.Y, joint.Position.Z);

                        string s = string.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},",
                            joint.Position.X - origin.Position.X, joint.Position.Y - origin.Position.Y, joint.Position.Z - origin.Position.Z,
                            speedX, speedY, speedZ,
                            mag,
                            alpha, beta, gamma);

                        bodyData.Rows.Add(s);
                    }
                }

                writer.Write(line);
                _current++;

                isFirst = false;
            }
        }

        public void Stop()
        {
            IsRecording = false;
            _hasEnumeratedJoints = false;

            /* try this */
            int n_frames = 20;
            ILookup<string, string> lookup = bodyPositions.ToLookup(kvp => kvp.Key, kvp => kvp.Value);
            int incrementor = _current / n_frames;

            if (incrementor == 0)
            {
                _current = 0;
                return;
            }

            int pos = 0;

            //string Joint_vector = "";
            joint_vector = "";

            foreach (string joints in jointTypes)
            {
                // get 10 points
                for (int i = 0; i < n_frames; i++)
                {
                    joint_vector += lookup[joints].ElementAt(pos);
                    pos += incrementor;
                    //System.Diagnostics.Debug.Write(s);
                }
                pos = 0;
            }

            //string _Result = "Dictionary.csv";
            //using (StreamWriter file = new StreamWriter(_Result))
            //{
            //    file.WriteLine(joint_vector);
            //}

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

            pos = 0;
        }
    }
}
