using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Voxmat
{
    public class MaterialData
    {
        public Dictionary<int, string> Materials;
        public static string[] SupportedFormatsInput = new string[] { "Main material format (.amt)|*.amt" };
        public MaterialData(Stream file)
        {
            Materials = new Dictionary<int, string>();
            LoadMaterials(file);
        }

        private void LoadMaterials(Stream file)
        {
            StreamReader reader = new StreamReader(file);
            string line = "";
            while((line = reader.ReadLine()) != null)
            {
                line = line.Replace(" ", String.Empty);
                string[] rowData = line.Split('=');

                string str = rowData[1];
                short number = Convert.ToInt16(rowData[0]);

                Materials.Add(number, str);
                
            }
        }
    }
}
