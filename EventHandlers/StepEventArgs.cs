using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DogBoardingPipeLine.EventHandlers
{
    public class StepEventArgs : EventArgs
    {
        private string stepMsg = null;

        public string StepMsg
        {
            get { return this.stepMsg; }
            set { this.stepMsg = value; }
        }
    }
}
