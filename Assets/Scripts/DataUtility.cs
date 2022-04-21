using UnityEngine;

using System;
using System.IO;

namespace Stijn.Prototype.Data
{
    public static class DataUtility
    {
        private static readonly char DELIMETER = ',';

        public static void WriteCsvFile( string filePath, string title, string[] data)
        {
            StreamWriter writer = new StreamWriter(filePath);

            writer.WriteLine(title);

            for( int i=0; i < data.Length; ++i)
            {
                writer.WriteLine(data[i]);
            }

            writer.Flush();
            writer.Close();
        }

        public static void WriteCsvFile( string[] data)
        {
            WriteCsvFile("Performance", data);
        }

        public static void WriteCsvFile( string[][] data, string[] titles)
        {
            StreamWriter writer = new StreamWriter(GetFilePath());
            
            string s = "";
            // Set Titles
            for( int i=0; i < titles.Length; ++i)
            {
                s += titles[i];
                if (i < titles.Length -1)
                {
                    s += DELIMETER;
                }
            }
            writer.WriteLine(s);
            // Set Data
            for( int i=0; i < data[0].Length; ++i)
            {
                s = "";
                for( int j=0; j < data.Length; ++j)
                {
                    s += data[j][i];
                    if (j < data.Length - 1)
                    {
                        s += DELIMETER;
                    }
                }
                writer.WriteLine(s);
            }

            writer.Flush();
            writer.Close();
        }

        public static void WriteCsvFile(string title, string[] data)
        {
            string folder = Path.GetDirectoryName(Application.dataPath);
            string fileName = DateTime.Now.ToString("dd-MM-yy_HH-mm-ss") + "_DestructionPerformance.csv";

            WriteCsvFile(Path.Combine(folder, fileName), title, data);
        }

        public static string GetFilePath()
        {
            string folder = Path.GetDirectoryName(Application.dataPath);
            string fileName = DateTime.Now.ToString("dd-MM-yy_HH-mm-ss") + "_DestructionPerformance.csv";
            return Path.Combine(folder, fileName);
        }
    }
}
