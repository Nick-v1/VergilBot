using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VergilBot.Models.Entities
{
    public class ControlNet
    {
        public List<ControlNetArgs> args { get; set; }
    }

    public class ControlNetArgs
    {
        public string input_image { get; set; }
        public string module { get; set; }
        public string model { get; set; }
        public double weight { get; set; }
        public int threshold_a { get; set; }
        public int threshold_b { get; set; }
        public bool lowvram { get; set; }
        public bool pixel_perfect { get; set; }
        public double guidance_start { get; set; }
        public double guidance_end { get; set; }
        public string control_mode { get; set; }
        public object mask { get; set; }
        public string resize_mode { get; set; }
        public int processor_res { get; set; }
    }
}
