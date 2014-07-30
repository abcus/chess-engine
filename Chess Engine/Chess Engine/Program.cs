using System;
using System.Security.AccessControl;
using System.Threading;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace Chess_Engine {
    
    public sealed class Program {
        
        static void Main(string[] args) {

            Console.BufferHeight = 2000 ;

            //Stream inputStream = Console.OpenStandardInput(8192);
            //Console.SetIn(new StreamReader(inputStream, Encoding.ASCII, false, 8192));

            Engine e = new Engine();
            System.Threading.Thread t = new System.Threading.Thread(e.run);
            t.Start();

        }

    }

}

