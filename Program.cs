using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IndexSkipList
{
    class Program
    {
        static void Main(string[] args)
        {
            Random rand = new Random();
            List<SkipList> skipLists = new List<SkipList>();

            skipLists.Add(new SkipList("0".ToString()));

            Stopwatch sw = new Stopwatch();
            sw.Start();

            //skipLists[0].FIndByScore(5000);
            skipLists[0].PrintList();

            Console.WriteLine("{0}", sw.ElapsedMilliseconds);
            sw.Stop();

        }
    }
}
