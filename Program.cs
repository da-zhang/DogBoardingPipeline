using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Configuration;
using DogBoardingPipeLine.Pipeline;
using HtmlAgilityPack;

namespace DogBoardingPipeLine
{
    class Program
    {

        /// <summary>
        /// https://www.rover.com/top-dog-boarding-cities/
        /// https://dogvacay.com/
        /// </summary>
        /// <param name="args"></param>

        static void Main(string[] args)
        {
            string configurationInput = ConfigurationManager.AppSettings["configurationFile"];
            string input = File.ReadAllText(configurationInput);
            RoverPipeline pipeline = RoverPipeline.LoadFromXML(input);
            pipeline.Run();

            Console.WriteLine("pipeline done!");

            Console.Read();
        }
    }
}
