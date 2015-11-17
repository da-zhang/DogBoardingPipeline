using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using DogBoardingPipeLine.EventHandlers;

namespace DogBoardingPipeLine.Pipeline
{
    public class RoverPipeline
    {
        private List<PipelineStep> steps = null;

        public RoverPipeline()
        {
            this.steps = new List<PipelineStep>();
        }

        public List<PipelineStep> Steps
        {
            get
            {
                return this.steps;
            }
        }

        public static RoverPipeline LoadFromXML(string inputXML)
        {
            RoverPipeline pipeline = new RoverPipeline();

            TextReader pipelineReader = new StringReader(inputXML);
            XElement ele = XElement.Load(pipelineReader);

            IEnumerable<XElement> stepsElements = ele.Elements("step");

            foreach(XElement stepsElement in stepsElements)
            {
                PipelineStep step = PipelineStep.FromXml(stepsElement);

                step.StepStart += (sender, e) =>
                    {
                        Console.WriteLine(e.StepMsg);
                    };

                step.StepComplete += (sender, e) =>
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine(e.StepMsg);
                        Console.ResetColor();
                    };
                step.StepError += (sender, e) =>
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine(e.StepMsg);
                        Console.ResetColor();
                    };

                step.PageComplete += (sender, e) =>
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine(e.StepMsg);
                        Console.ResetColor();
                    };

                pipeline.steps.Add(step);
            }

            return pipeline;
        }

        public void Run()
        {
            this.steps.ForEach(step =>
                {
                    string errorMsg = string.Empty;

                    if (!step.Ignore)
                    {
                        step.Run(ref errorMsg);
                    }
                });
        }
    }
}
