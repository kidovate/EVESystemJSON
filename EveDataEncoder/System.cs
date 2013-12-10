using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EveDataEncoder
{
    public class System
    {
        public double security;
        public string name;
        public int id;
        public bool region = false;
        public int regionID;

        public bool Equals(System other)
        {
            return other.id == id;
        }
    }
}
