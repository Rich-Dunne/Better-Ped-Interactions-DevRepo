using System.Linq;

namespace BetterPedInteractions.Utils
{
    internal static class Extensions
    {
        internal static bool IsCategoryElementDefined(this MenuItem menuItem, string element)
        {
            if (menuItem.Element.Parent.Elements(element).Any() && !string.IsNullOrEmpty(menuItem.Element.Parent.Element(element).Value))
            {
                return true;
            }

            return false;
        }

        internal static bool IsMenuItemElementDefined(this MenuItem menuItem, string element)
        {
            if (menuItem.Element.Elements(element).Any() && !string.IsNullOrEmpty(menuItem.Element.Element(element).Value))
            {
                return true;
            }

            return false;
        }

        internal static bool IsAttributeDefined(this MenuItem menuItem, string element, string attribute)
        {
            var matchingElement = menuItem.Element.Elements(element).First(x => x.Attribute(attribute) != null && !string.IsNullOrEmpty(x.Attribute(attribute).Value));
            if (matchingElement != null)
            {
                return true;
            }
            return false;
        }
    }
}
