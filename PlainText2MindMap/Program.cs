using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlainText2MindMap {
    class Program {

        public static bool DEBUG = true;

        static void Main(string[] args)
        {
            var fileName = "";
            if (DEBUG) fileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "examples", "pg1342.txt");
            else fileName = args[0];

            if (String.IsNullOrEmpty(fileName)) return;

            MindMap mm = new MindMap();
            mm.build(fileName);
        }
    }
}
