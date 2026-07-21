using GSSystemAnalyzer.Interfaces;
using GSSystemAnalyzer.Services.Oem.Dell;

namespace GSSystemAnalyzer.Services.Oem.Factory
{
	/* public static class OemReaderFactory
    {
        public static IDellOemTelemetry CreateReader()
        {
            var manufacturer = GetSystemManufacturer();

            return manufacturer switch
            {
                "Dell Inc." => new DellOemTelemetry(),
                _ => new UnsupportedOemTelemetry("Unsupported OEM")
            };
        }
    }
    */
}
