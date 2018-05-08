using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace OEE_WPF_Application
{
    public class Package : INotifyPropertyChanged
    {
        private string name = String.Empty;
        private bool primarypack = false;
        private int? primarypackdensity;

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
                    NotifyPropertyChanged();
                }
            }
        }

        public int? PrimaryPackDensity
        {
            get
            {
                if(this.primarypack)
                {
                    return 1;
                }
                else
                {
                    return this.primarypackdensity;
                }                
            }
            set
            {
                if (value >= 1)
                {
                    if (value != this.primarypackdensity)
                    {
                        this.primarypackdensity = value;
                        NotifyPropertyChanged();
                    }
                }
                else
                {
                    this.primarypackdensity = 1;
                    NotifyPropertyChanged();
                }
            }
        }

        public bool PrimaryPack
        {
            get
            {
                return this.primarypack;
            }
            set
            {
                if(value != this.primarypack)
                {
                    this.primarypack = value;
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
