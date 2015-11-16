using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.IO;
using HtmlAgilityPack;

namespace DogBoardingPipeLine.Pipeline
{
    public class PipelineStep
    {
        private int stepIndex = -1;
        private string stepName = null;
        private dynamic stepInput = null;
        private string stepInputFile = null;
        private List<string> stepOutput = null;
        private string stepStorageFile = null;
        private List<Element> elementList = null;
        private bool manyPage = false;
        private string pageTemplate = null;
        private bool ignore = false;

        public int StepIndex
        {
            get { return stepIndex; }
            set { stepIndex = value; }
        }

        public string StepName
        {
            get { return this.stepName; }
            set { this.stepName = value; }
        }

        public dynamic StepInput
        {
            get { return this.stepInput; }
            set { this.stepInput = value; }
        }

        public List<string> StepOutput
        {
            get { return this.stepOutput; }
            set { this.stepOutput = value; }
        }

        public string StepStorageFile
        {
            get { return this.stepStorageFile; }
            set { this.stepStorageFile = value; }
        }

        public string StepInputFile
        {
            get { return this.stepInputFile; }
            set { this.stepInputFile = value; }
        }

        public bool ManyPage
        {
            get { return this.manyPage; }
            set { this.manyPage = value; }
        }

        public bool Ignore
        {
            get { return this.ignore; }
            set { this.ignore = value; }
        }

        public PipelineStep()
        {
            this.stepIndex = 0;
            this.stepName = string.Empty;
            this.stepOutput = new List<string>();
            this.stepInput = string.Empty;
            this.stepInputFile = string.Empty;
            this.elementList = new List<Element>();
            this.pageTemplate = string.Empty;
        }

        public bool Run(ref string errorMsg)
        {
            this.stepInput = File.ReadAllLines(this.StepInputFile);
            HtmlWeb web = new HtmlWeb();

            foreach (string input in this.stepInput)
            {
                if (input.StartsWith("C-") && input.EndsWith("-C"))
                {
                    continue;
                }

                HtmlDocument doc = web.Load(input);

                foreach (Element e in this.elementList)
                {
                    try
                    {
                        e.Execute(doc, ref errorMsg);
                        this.stepOutput.Add(string.Format("C-{0}-C", e.ColumnName));
                        this.stepOutput.AddRange(e.Values);
                        e.Values.Clear();
                    }
                    catch (Exception ex)
                    {
                        errorMsg = ex.ToString();
                        return false;
                    }
                }
            }

            File.WriteAllLines(this.stepStorageFile, this.stepOutput);

            return true;
        }

        private void GoThroughPages()
        {
            if(this.manyPage)
            {

            }
        }

        public static PipelineStep FromXml(XElement stepData)
        {
            PipelineStep step = new PipelineStep();

            step.stepIndex = int.Parse(stepData.Element("id").Value);
            step.stepName = stepData.Attribute("name").Value;
            step.stepStorageFile = stepData.Element("outputTextFile").Value;
            step.stepInputFile = stepData.Element("inputTextFile").Value;

            foreach (XElement element in stepData.Element("elements").Elements("element"))
            {
                step.elementList.Add(new Element(element));
            }

            step.manyPage = bool.Parse(stepData.Element("manyPage").Value);
            string ignore = stepData.Element("ignore").Value;
            step.ignore = bool.Parse(ignore);
            string pageTemplate = stepData.Element("pageUrl").Value;
            step.pageTemplate = pageTemplate.EndsWith(".txt") ? File.ReadAllText(pageTemplate) : pageTemplate;

            return step;
        }
    }
}
