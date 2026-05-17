namespace SuperMarket.Services
{
    public static class LicenseService
    {
        // ============================================================
        //  ✏️  غيّر التاريخ هنا فقط لتمديد أو إنهاء الترخيص
        // ============================================================
        private static readonly DateTime ExpiryDate = new DateTime(2026, 12, 31);
        // ============================================================

        public static bool IsValid() => DateTime.Now <= ExpiryDate;

        public static DateTime GetExpiry() => ExpiryDate;

        public static int DaysRemaining()
        {
            var diff = ExpiryDate.Date - DateTime.Now.Date;
            return diff.Days;
        }
    }
}
