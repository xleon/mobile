using System;
using System.Linq.Expressions;
using Newtonsoft.Json;

namespace Toggl.Phoebe.Data.Models
{
	public class ProjectColor
    {
        public static ProjectColor[] All;

        static ProjectColor() 
        {
            string[] hexColorsIndex = new string[] {
                "#4dc3ff", "#bc85e6", "#df7baa", "#f68d38", "#b27636",
                "#8ab734", "#14a88e", "#268bb5", "#6668b4", "#a4506c",
                "#67412c", "#3c6526", "#094558", "#bc2d07", "#999999"
            };
            All = new ProjectColor[hexColorsIndex.Length];
            for (var i = 0; i < hexColorsIndex.Length; i++) {
                All [i] = new ProjectColor (hexColorsIndex [i]);
            }
        }

        private string hex;

        private ProjectColor (string hex) {
            this.hex = hex;
        }

        public string Hex{
            get { return hex;}
        }

        public static ProjectColor Default(){
            return All [All.Length - 1];
        }
	}
}
