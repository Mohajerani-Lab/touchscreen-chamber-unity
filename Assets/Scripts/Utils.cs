using System.Linq;
using System.Xml.Linq;

namespace DefaultNamespace
{
    public class Utils
    {
        public static XElement FindElementByName(XElement parent, string childName)
        {
            return parent.Elements().Where(e => e.Name.ToString().Equals(childName)).ToArray()[0];
        }
    }
}