using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace OEE_Console
{
    public class Unit_Op : INotifyPropertyChanged
    {
        private string name = String.Empty;
        private int? designspeed = null;
        private int? speedloss = null;
        private float? overspeed = null;
        private int? mttr = null;
        private int? mtbf = null;
        private float? qualityloss = null;

        public string Name
        {
            get
            {
                return this.name;
            }
            set
            {
                if (value != this.name)
                {
                    this.name = value;
                    NotifyPropertyChanged();
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
                        NotifyPropertyChanged();
                    }
                }
                else
                {
                    this.designspeed = null;
                    NotifyPropertyChanged();
                }
            }
        }

        public int? SpeedLoss
        {
            get
            {
                return this.speedloss = null;
            }
            set
            {
                if(value != this.speedloss)
                {
                    this.speedloss = value;
                    NotifyPropertyChanged();
                }
            }
        }

        public int? ActualSpeed
        {
            get
            {
                if(this.designspeed > 0 && this.speedloss > 0)
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
                if(value != this.overspeed)
                {
                    this.overspeed = value;
                    NotifyPropertyChanged();
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
                        NotifyPropertyChanged();
                    }
                }
                else
                {
                    this.mtbf = null;
                    NotifyPropertyChanged();
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
                        NotifyPropertyChanged();
                    }
                }
                else
                {
                    this.mtbf = null;
                    NotifyPropertyChanged();
                }
            }
        }

        public float? Availability
        {
            get
            {
                if(this.mtbf.HasValue && this.mttr.HasValue)
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
                if(value != this.qualityloss)
                {
                    this.qualityloss = value;
                    NotifyPropertyChanged();
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void NotifyPropertyChanged([CallerMemberName] String propName = "")
        {
            if (this.PropertyChanged != null)
            {
                this.PropertyChanged(this, new PropertyChangedEventArgs(propName));
            }
        }        
    }
}
