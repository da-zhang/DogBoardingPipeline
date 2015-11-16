using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

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
