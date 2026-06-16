namespace Fale7_POS.Properties
{
    internal sealed partial class Settings
    {
        public bool PrinterWidth80mm
        {
            get
            {
                var raw = this["PrinterWidth80mm"];
                return raw is bool value && value;
            }
            set
            {
                this["PrinterWidth80mm"] = value;
            }
        }
    }
}
