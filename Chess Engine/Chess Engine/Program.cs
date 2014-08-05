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

            var stream = Console.OpenStandardInput(8196);
            Console.SetIn(new StreamReader(stream, Encoding.ASCII));

            CancellationTokenSource cts = new CancellationTokenSource();
            
            Engine e = new Engine();
            e.run();
        } 
    }
}

