using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MNN = MathNet.Numerics;

namespace OEE_ExcelAddIn_2010
{
    public class Unit_Op
    {
        private string name = String.Empty;
        private int? designspeed = null;
        private int? speedloss = null;
        private int? currentspeed = null;
        private float? overspeed = null;
        private int? mttr = null;
        private int? mtbf = null;
        private float? qualityloss = null;
        private int? buffer = null;
        private int? buffer_count = 0;
        private bool running = false;
        private bool paused = false;
        private bool faulted = false;
        private int? thisup_duration = null;
        private int? nextdown_duration = null;
        private int totaluptime = 0;
        private int totaldowntime = 0;
        private Package package = new Package();
        private MNN.Distributions.Exponential mtbf_dist;
        private MNN.Distributions.Exponential mttr_dist;

        public string Name
        {
            get
            {
                return this.name;
            }
            set
            {
                if (value != this.Name)
                {
                    this.name = value;
                }
            }
        }

        public int? DesignSpeed
        {
            get
            {
                return this.designspeed;
            }
            set
            {
                if (value >= 0)
                {
                    if (value != this.designspeed)
                    {
                        this.designspeed = value;
                    }
                }
                else
                {
                    this.designspeed = null;
                }
            }
        }

        public int? SpeedLoss
        {
            get
            {
                if(speedloss.HasValue)
                {
                    return this.speedloss;
                }
                else
                {
                    return 0;
                }
            }
            set
            {
                if(value.HasValue)
                {
                    if (value != this.speedloss)
                    {
                        this.speedloss = value;
                    }
                }
                else
                {
                    this.speedloss = 0;
                }
            }
        }

        public int? CurrentSpeed
        {
            get
            {
                return this.currentspeed;
            }
            set
            {
                if(value != this.currentspeed)
                {
                    this.currentspeed = value;
                }
            }
        }

        public int? ActualSpeed
        {
            get
            {
                if (this.designspeed.HasValue && this.speedloss.HasValue)
                {
                    return this.designspeed - this.speedloss;
                }
                else
                {
                    return null;
                }
            }
        }

        public float? Overspeed
        {
            get
            {
                return this.overspeed;
            }
            set
            {
                if (value != this.overspeed)
                {
                    this.overspeed = value;
                }
            }
        }

        public int? MTTR
        {
            get
            {
                return this.mttr;
            }
            set
            {
                if (value >= 0)
                {
                    if (value != this.mttr)
                    {
                        this.mttr = value;
                        this.mttr_dist = new MNN.Distributions.Exponential(1.0 / (double)this.mttr);
                    }
                }
                else
                {
                    this.mtbf = null;
                }
            }
        }

        public int? MTBF
        {
            get
            {
                return this.mtbf;
            }
            set
            {
                if (value >= 0)
                {
                    if (value != this.mtbf)
                    {
                        this.mtbf = value;
                        this.mtbf_dist = new MNN.Distributions.Exponential(1.0 / (double)this.mtbf);
                    }
                }
                else
                {
                    this.mtbf = null;
                }
            }
        }

        public int? ThisUpTime
        {
            get
            {
                return thisup_duration;
            }
            set
            {
                this.thisup_duration = value;                
            }
        }

        public int? NextDownTime
        {
            get
            {
                return nextdown_duration;
            }
            set
            {
                this.nextdown_duration = value;
            }
        }

        public float? Availability
        {
            get
            {
                if (this.mtbf.HasValue && this.mttr.HasValue)
                {
                    return this.mtbf / (this.mttr + this.mtbf);
                }
                else
                {
                    return null;
                }
            }
        }

        public float? QualityLoss
        {
            get
            {
                return this.qualityloss;
            }
            set
            {
                if (value != this.qualityloss)
                {
                    this.qualityloss = value;
                }
            }
        }

        public int? Buffer
        {
            get
            {
                return this.buffer;
            }
            set
            {
                if(value != this.buffer)
                {
                    if(value == null)
                    {
                        this.buffer = 0;
                    }
                    else
                    {
                        this.buffer = value;
                    }                    
                }
            }
        }

        public int? Buffer_Count
        {
            get
            {
                return this.buffer_count;
            }
            set
            {
                if(value != this.buffer_count)
                {
                    this.buffer_count = value;
                }
            }
        }

        public bool Buffer_Full
        {
            get
            {
                if(this.buffer_count < this.buffer)
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }
        }

        public bool Buffer_Empty
        {
            get
            {
                if (this.buffer_count >= this.buffer)
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }
        }

        public bool Running
        {
            get
            {
                return this.running;
            }
            set
            {
                if (value != this.running)
                {
                    this.running = value;
                }

                if(value)
                {
                    this.Paused = false;
                    this.Faulted = false;
                }
            }
        }

        public bool Paused
        {
            get
            {
                return this.paused;
            }
            set
            {
                if (value != this.paused)
                {
                    this.paused = value;                    
                }

                if(value)
                {
                    this.Running = false;
                    this.Faulted = false;
                    this.CurrentSpeed = 0;
                }
            }
        }

        public bool Faulted
        {
            get
            {
                return this.faulted;
            }
            set
            {
                if (value != this.faulted)
                {
                    this.faulted = value;
                }

                if(value)
                {
                    this.Running = false;
                    this.Paused = false;
                    this.CurrentSpeed = 0;
                }
            }
        }

        public Package Package
        {
            get
            {
                return this.package;
            }
            set
            {
                if (value != this.package)
                {
                    this.package = value;
                }
            }
        }

        public int TotalUpTime
        {
            get
            {
                return this.totaluptime;
            }
            set
            {
                if(value >= 0 && value != this.totaluptime)
                {
                    this.totaluptime = value;
                }
            }
        }

        public int TotalDownTime
        {
            get
            {
                return this.totaldowntime;
            }
            set
            {
                if (value >= 0 && value != this.totaldowntime)
                {
                    this.totaldowntime = value;
                }
            }
        }

        public void Sim_UpTime()
        {
            if(!this.mtbf.HasValue || this.mtbf == 0)
            {
                this.ThisUpTime = int.MaxValue;
            }
            else
            {
                int holder = (int)mtbf_dist.Sample();
                if (holder == 0)
                {
                    holder = 1;
                }
                this.ThisUpTime = holder;
            }            
        }

        public void Sim_DownTime()
        {
            if (!this.mttr.HasValue || this.mttr == 0)
            {
                this.NextDownTime = 0;
            }
            else
            {
                int holder = (int)mttr_dist.Sample();
                if (holder == 0)
                {
                    holder = 1;
                }
                this.NextDownTime = holder;
            }
        }
    }

    public class Package
    {
        public string name;
        public int? density;
        public string period;

        public string Name
        {
            get
            {
                return this.name;
            }
            set
            {
                if(value != this.name)
                {
                    this.name = value;
                }
            }
        }

        public int? Density
        {
            get
            {
                return this.density;
            }
            set
            {
                if(value != this.density)
                {
                    this.density = value;
                }
            }
        }

        public string Period
        {
            get
            {
                return this.period;
            }
            set
            {
                if(value != this.period)
                {
                    this.period = value;
                }
            }
        }
    }
}
