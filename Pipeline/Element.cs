using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HtmlAgilityPack;
using System.Xml.Linq;
using System.Configuration;

namespace DogBoardingPipeLine.Pipeline
{
    public class Element
    {
        // The value scraped from web page
        private List<string> values = null;
        // The method to locate the HTML element on the page
        // Could be: element Id, XPath
        private string locator = null;
        // The method type to locate the element on the page
        // could be element Id, XPath
        private string locatorType = null;
        private string valueIndicator = null;
        private string columnName = null;

        public List<string> Values
        {
            get { return this.values; }
        }

        public string Locator
        {
            get { return this.locator; }
            set { this.locator = value; }
        }

        public string LocatorType
        {
            get { return this.locatorType; }
            set { this.locatorType = value; }
        }

        public string ColumnName
        {
            get { return this.columnName; }
            set { this.columnName = value; }
        }

        private string ValueIndicator
        {
            get { return this.valueIndicator; }
            set { this.valueIndicator = value; }
        }

        public Element(string locator, string locatorType, string columnName, string valueIndicator)
        {
            this.columnName = columnName;
            this.locator = locator;
            this.locatorType = locatorType;
            this.valueIndicator = valueIndicator;
            this.values = new List<string>();
        }

        public Element(XElement elementProfile)
        {
            this.columnName = elementProfile.Element("columnName").Value;
            this.locator = elementProfile.Element("locator").Value;
            this.locatorType = elementProfile.Element("locatorType").Value;
            this.valueIndicator = elementProfile.Element("valueIndicator").Value;
            this.values = new List<string>();
        }

        public bool Execute(HtmlDocument doc, bool failOnEmpty,ref string errorMsg)
        {
            try
            {
                List<HtmlNode> targetList = null;
                
                if(this.locatorType == "xpath")
                {
                    targetList = doc.DocumentNode.SelectNodes(this.locator).ToList();
                }
                else if(this.locatorType == "id")
                {
                    targetList.Add(doc.GetElementbyId(this.locator));
                }

                if(targetList == null)
                {
                    errorMsg = "No results.";
                    return failOnEmpty ? false : true;
                }

                foreach(HtmlNode target in targetList)
                {
                    string[] valueIndicator = this.valueIndicator.Split(':');
                    string oneValue = null;

                    if(valueIndicator.Length == 1)
                    {
                        if (valueIndicator[0].ToUpper() == "INNERTEXT")
                        {
                            oneValue = target.InnerText;
                        }
                        else if (valueIndicator[0].ToUpper() == "INNERHTML")
                        {
                            oneValue = target.InnerHtml;
                        }
                    }
                    else
                    {
                        oneValue = target.Attributes[valueIndicator[1]].Value;

                        if(valueIndicator[1] == "href")
                        {
                            if(!oneValue.StartsWith("http"))
                            {
                                string homeUrl = ConfigurationManager.AppSettings["homeUrl"];
                                oneValue = string.Format("{0}/{1}", homeUrl, oneValue.Trim('/').Split('?')[0]);
                            }
                        }
                    }

                    this.values.Add(oneValue);
                }

                this.values = this.values.Distinct().ToList();
            }
            catch (Exception ex)
            {
                errorMsg = ex.ToString();
            }

            return true;
        }
    }
}
