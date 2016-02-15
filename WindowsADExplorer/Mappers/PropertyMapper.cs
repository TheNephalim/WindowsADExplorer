using WindowsADExplorer.Entities;
using WindowsADExplorer.Models;

namespace WindowsADExplorer.Mappers
{
    public interface IPropertyMapper
    {
        PropertyModel GetModel(Property property);
    }

    public class PropertyMapper : IPropertyMapper
    {
        public PropertyModel GetModel(Property property)
        {
            PropertyModel model = new PropertyModel();
            model.Name = property.Name;
            model.Value = property.Value;
            return model;
        }
    }
}
