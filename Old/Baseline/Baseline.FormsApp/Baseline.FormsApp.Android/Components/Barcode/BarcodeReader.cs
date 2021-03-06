namespace Baseline.FormsApp.Droid.Components.Barcode
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    using Baseline.FormsApp.Components.Barcode;

    using ZXing;
    using ZXing.Mobile;

    public sealed class BarcodeReader : Baseline.FormsApp.Components.Barcode.IBarcodeReader
    {
        private static readonly Dictionary<BarcodeFormat, SymbologyType> FormatToSymbology = new Dictionary<BarcodeFormat, SymbologyType>();

        private static readonly Dictionary<SymbologyType, BarcodeFormat> SymbologyToFormat = new Dictionary<SymbologyType, BarcodeFormat>();

        private readonly MobileBarcodeScanningOptions options = new MobileBarcodeScanningOptions
        {
            PossibleFormats = new List<BarcodeFormat>
            {
                BarcodeFormat.EAN_13,
                BarcodeFormat.EAN_8
            },
            TryHarder = true,
            TryInverted = true,
            DisableAutofocus = false
        };

        static BarcodeReader()
        {
            RegisterDictionary(BarcodeFormat.QR_CODE, SymbologyType.QR);
            RegisterDictionary(BarcodeFormat.EAN_13, SymbologyType.Ean13);
            RegisterDictionary(BarcodeFormat.EAN_8, SymbologyType.Ean8);
        }

        private static void RegisterDictionary(BarcodeFormat format, SymbologyType symbology)
        {
            FormatToSymbology[format] = symbology;
            SymbologyToFormat[symbology] = format;
        }

        public void SetSymbology(SymbologyType symbology)
        {
            options.PossibleFormats.Clear();

            foreach (SymbologyType value in Enum.GetValues(typeof(SymbologyType)))
            {
                if (symbology.HasFlag(value) && SymbologyToFormat.TryGetValue(value, out var format))
                {
                    options.PossibleFormats.Add(format);
                }
            }
        }

        public async Task<ScanResult> Scan()
        {
            var scanner = new MobileBarcodeScanner();
            var result = await scanner.Scan(options);

            return result != null && FormatToSymbology.TryGetValue(result.BarcodeFormat, out var symbology)
                ? new ScanResult(symbology, result.Text)
                : ScanResult.None;
        }
    }
}