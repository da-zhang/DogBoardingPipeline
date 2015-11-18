using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.IO;
using HtmlAgilityPack;
using DogBoardingPipeLine.EventHandlers;

namespace DogBoardingPipeLine.Pipeline
{
    public class PipelineStep
    {
        public event StepHandler StepComplete = null;
        public event StepHandler StepStart = null;
        public event StepHandler StepError = null;
        public event StepHandler PageComplete = null;

        private int stepIndex = -1;
        private string stepName = null;
        private dynamic stepInput = null;
        private string stepInputFile = null;
        private Dictionary<string, List<string>> stepOutput = null;
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

        public Dictionary<string, List<string>> StepOutput
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
            this.stepOutput = new Dictionary<string, List<string>>();
            this.stepInput = string.Empty;
            this.stepInputFile = string.Empty;
            this.elementList = new List<Element>();
            this.pageTemplate = string.Empty;
        }

        public bool Run(ref string errorMsg)
        {
            if (this.StepStart != null)
            {
                StepEventArgs e = new StepEventArgs();
                e.StepMsg = string.Format("Step {0} started at {1}.", this.stepName, DateTime.Now.ToString());
                this.StepStart(this, e);
            }

            this.stepInput = File.ReadAllLines(this.StepInputFile);
            HtmlWeb web = new HtmlWeb();

            foreach (string input in this.stepInput)
            {
                this.GoThroughPages(input);
            }

            this.MergeStepOutput();

            if (this.StepComplete != null)
            {
                StepEventArgs e = new StepEventArgs();
                e.StepMsg = string.Format("Step {0} succeed at {1}.", this.stepName, DateTime.Now.ToString());
                this.StepComplete(this, e);
            }

            return true;
        }

        private void SaveStepOutput(bool removeDuplicates)
        {
            string outputFile = string.Empty;

            foreach (string key in this.stepOutput.Keys)
            {
                outputFile = string.Format("{0}-{1}", this.StepStorageFile, key);
                
                if (removeDuplicates)
                {
                    File.WriteAllLines(outputFile, this.stepOutput[key].Distinct());
                }
                else
                {
                    File.WriteAllLines(outputFile, this.stepOutput[key]);
                }
            }
        }

        private void MergeStepOutput()
        {
            List<string> finalList = new List<string>();
            string prefix = string.Format("{0}-", this.StepStorageFile);

            List<string> tempFiles = Directory.GetFiles(Environment.CurrentDirectory).Where(f => Path.GetFileName(f).StartsWith(prefix)).ToList();

            if (tempFiles.Count > 1)
            {
                List<string[]> contents = new List<string[]>();

                foreach (string file in tempFiles)
                {
                    contents.Add(File.ReadAllLines(file));
                }

                int arrayLength = contents.First().Length;

                if (contents.All(c => c.Length == arrayLength))
                {
                    for (int i = 0; i < arrayLength; i++)
                    {
                        StringBuilder finalLineBuilder = new StringBuilder();

                        foreach (string[] columnArray in contents)
                        {
                            finalLineBuilder.AppendFormat("\"{0}\",", columnArray[i]);
                        }

                        finalList.Add(finalLineBuilder.ToString());
                    }
                }

                File.WriteAllLines(this.stepStorageFile, finalList);
                tempFiles.ForEach(t => File.Delete(t));
            }
            else
            {
                if (File.Exists(this.stepStorageFile))
                {
                    File.Delete(this.stepStorageFile);
                }

                File.Move(tempFiles[0], this.stepStorageFile);
            }
        }

        private void GoThroughPages(string baseUrl)
        {
            HtmlWeb web = new HtmlWeb();
            HtmlDocument doc = null;
            string errorMsg = string.Empty;

            if (this.manyPage)
            {
                int startPage = 1;

                while (true)
                {
                    string currentPage = startPage == 1 ? baseUrl : string.Format(this.pageTemplate, baseUrl, startPage);
                    doc = web.Load(currentPage);
                    bool pageScraped = this.ExecuteScrapeOnDocument(doc, startPage, ref errorMsg);

                    if (pageScraped && this.PageComplete != null)
                    {
                        StepEventArgs args = new StepEventArgs();
                        args.StepMsg = string.Format("Page {0} of URL {1} complete, current step: {2}.", startPage, baseUrl, this.stepName);
                        this.PageComplete(this, args);
                    }

                    if (errorMsg == "Reached last page!")
                    {
                        break;
                    }

                    startPage++;
                }
            }
            else
            {
                doc = web.Load(baseUrl);
                this.ExecuteScrapeOnDocument(doc, 1, ref errorMsg);
            }
        }

        private bool ExecuteScrapeOnDocument(HtmlDocument doc, int pageIndex, ref string errorMsg)
        {
            foreach (Element e in this.elementList)
            {
                try
                {
                    bool succeed = e.Execute(doc, this.manyPage, ref errorMsg);

                    if (this.stepOutput.ContainsKey(e.ColumnName) == false)
                    {
                        this.stepOutput.Add(e.ColumnName, new List<string>());
                    }

                    this.stepOutput[e.ColumnName].AddRange(e.Values);
                    e.Values.Clear();

                    this.SaveStepOutput(e.Single);

                    if ((!succeed && this.manyPage) || e.Values.Count < 20 || (pageIndex > 1 && e.Values.Count < 60))
                    {
                        errorMsg = "Reached last page!";
                        //return true;
                    }
                }
                catch (Exception ex)
                {
                    if (this.StepError != null)
                    {
                        StepEventArgs arg = new StepEventArgs();
                        arg.StepMsg = string.Format("Step {0} encountered error: {1}.", this.stepName, ex.ToString());
                        this.StepError(this, arg);
                    }

                    errorMsg = ex.ToString();
                    return false;
                }
            }

            return true;
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
            step.pageTemplate = (string.IsNullOrEmpty(pageTemplate) || pageTemplate.StartsWith("http")) ? pageTemplate : File.ReadAllText(pageTemplate);

            return step;
        }
    }
}
