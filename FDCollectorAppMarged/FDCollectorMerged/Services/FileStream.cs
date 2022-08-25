using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AttendenceApp.Services
{
    class FileStream
    {
        public static void WriteToFile(string text)
        {
            using (StreamWriter writer = new StreamWriter("QueryQueue.txt"))
            {
                writer.WriteLine(text);
            }
        }

        public static List<string> ProcessQueue()
        {
            List<string> lstQueue = new List<string>();

            foreach (string line in System.IO.File.ReadLines(@"QueryQueue.txt"))
            {
                lstQueue.Add(line);
            }
            return lstQueue;
        }
    }
}
