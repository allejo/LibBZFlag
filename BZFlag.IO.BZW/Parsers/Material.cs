using BZFlag.Map.Elements;

namespace BZFlag.IO.BZW.Parsers
{
    public class MaterialParser : BasicObjectParser
    {
        public MaterialParser()
        {
            Object = new Material();
        }

        public MaterialParser(Material obj)
        {
            Object = obj;
        }
    }
}