using System;

namespace WindowsADExplorer.Models
{
    public class PropertyModel : ObservableModel<PropertyModel>
    {
        public string Name 
        {
            get { return Get(x => x.Name); }
            set { Set(x => x.Name, value); }
        }

        public string Value 
        {
            get { return Get(x => x.Value); }
            set { Set(x => x.Value, value); }
        }
    }
}
