using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bridge;
using Bridge.Html5;

namespace Kingdom
{
    class textureMap
    {
        private HTMLImageElement tMap;
        public static textureMap txMap;

        public textureMap(HTMLImageElement tMap)
        {
            this.tMap = tMap;
            txMap = this;
        }

        public Point getSubImageCoords(App.Textures texture)
        {
            return new Point((int)texture * (int)App.textureMapTextureLength, 0);
        }
    }
}
