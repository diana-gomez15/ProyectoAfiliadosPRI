using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProyectoAfiliadosPRI
{
    internal class Archivo
    {

            public string Texto { get; set; }
            public string Ruta { get; set; }
            public override string ToString()
            {
                return Texto ?? Ruta ?? base.ToString();
            }

      
}
}
