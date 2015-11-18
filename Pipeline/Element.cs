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
        private bool single = false;

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

        public string ValueIndicator
        {
            get { return this.valueIndicator; }
            set { this.valueIndicator = value; }
        }

        public bool Single
        {
            get { return this.single; }
            set { this.single = value; }
        }

        public Element(string locator, string locatorType, string columnName, string valueIndicator, bool single)
        {
            this.columnName = columnName;
            this.locator = locator;
            this.locatorType = locatorType;
            this.valueIndicator = valueIndicator;
            this.values = new List<string>();
            this.single = single;
        }

        public Element(XElement elementProfile)
        {
            this.columnName = elementProfile.Element("columnName").Value;
            this.locator = elementProfile.Element("locator").Value;
            this.locatorType = elementProfile.Element("locatorType").Value;
            this.valueIndicator = elementProfile.Element("valueIndicator").Value;
            this.single = bool.Parse(elementProfile.Element("single").Value);
            this.values = new List<string>();
        }

        public bool Execute(HtmlDocument doc, bool failOnEmpty, ref string errorMsg)
        {
            try
            {
                HtmlNodeCollection targetList = null;

                if (this.locatorType == "xpath")
                {
                    targetList = doc.DocumentNode.SelectNodes(this.locator);
                }
                else if (this.locatorType == "id")
                {
                    targetList.Add(doc.GetElementbyId(this.locator));
                }
                else
                {
                    if (this.locatorType.StartsWith("children"))
                    {
                        targetList = doc.DocumentNode.SelectNodes(this.locator);
                    }
                }

                if (targetList == null)
                {
                    errorMsg = "No results.";
                    return failOnEmpty ? false : true;
                }

                foreach (HtmlNode target in targetList)
                {
                    string oneValue = null;
                    string[] indicators = this.valueIndicator.Split(':');

                    if (this.locatorType.Contains(":"))
                    {
                        string childrenLocator = this.locatorType.Split(':')[1];
                        string tagName = childrenLocator.Split(new string[] { "***" }, StringSplitOptions.RemoveEmptyEntries)[0];
                        string attributeName = childrenLocator.Split(new string[] { "***" }, StringSplitOptions.RemoveEmptyEntries)[1];
                        string attributeValue = childrenLocator.Split(new string[] { "***" }, StringSplitOptions.RemoveEmptyEntries)[2];
                        IEnumerable<HtmlNode> children = target.Descendants(tagName).Where(item => item.Attributes.Contains(attributeName) && item.Attributes[attributeName].Value == attributeValue);
                        StringBuilder valueBuilder = new StringBuilder();

                        foreach (HtmlNode child in children)
                        {
                            string oneCell = child.Attributes[indicators[1]].Value;
                            valueBuilder.AppendFormat("{0};", oneCell);
                        }

                        oneValue = valueBuilder.ToString().Trim(';');
                        oneValue = string.IsNullOrEmpty(oneValue) ? "No Badges" : oneValue;
                    }
                    else
                    {
                        if (indicators.Length == 1)
                        {
                            if (indicators[0].ToUpper() == "INNERTEXT")
                            {
                                oneValue = target.InnerText;
                            }
                            else if (indicators[0].ToUpper() == "INNERHTML")
                            {
                                oneValue = target.InnerHtml;
                            }
                        }
                        else
                        {
                            oneValue = target.Attributes[indicators[1]].Value;

                            if (indicators[1] == "href")
                            {
                                if (!oneValue.StartsWith("http"))
                                {
                                    string homeUrl = ConfigurationManager.AppSettings["homeUrl"];
                                    oneValue = string.Format("{0}/{1}", homeUrl, oneValue.Trim('/').Split('?')[0]);
                                }
                            }
                        }
                    }

                    this.values.Add(oneValue);
                }

                this.values = this.single ? this.values.Distinct().ToList() : this.values;
            }
            catch (Exception ex)
            {
                throw ex;
                //errorMsg = ex.ToString();
                //return false;
            }

            return true;
        }
    }
}
