using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WindowsADExplorer.Models
{
    public class ErrorModel : ObservableModel<ErrorModel>
    {
        public string ErrorMessage
        {
            get { return Get(x => x.ErrorMessage); }
            set { Set(x => x.ErrorMessage, value); }
        }

        public string StackTrace
        {
            get { return Get(x => x.StackTrace); }
            set { Set(x => x.StackTrace, value); }
        }
    }
}
