public class SenaryoOdemeModelServisi
{
    public struct Girdi
    {
        public int Bahis;
        public int ScatterYuzde;
        public int CarpanYuzde;
        public int MaxCarpanAdedi;
        public int ZorlaCarpan;
        public int MaxScatterPerSpin;
    }

    public struct Sonuc
    {
        public int OdemeEgilimiYuzde;
        public int OdemeDagilimiYuzde;
        public int MinOdemeTL;
        public int MaxOdemeTL;
    }

    public Sonuc Hesapla(Girdi girdi)
    {
        int bahis = ClampMin(girdi.Bahis, 1);
        int scatter = Clamp(girdi.ScatterYuzde, 0, 100);
        int carpan = Clamp(girdi.CarpanYuzde, 0, 100);
        int maxCarpan = ClampMin(girdi.MaxCarpanAdedi, 0);
        int zorlaCarpan = ClampMin(girdi.ZorlaCarpan, 0);
        int maxScatter = Clamp(girdi.MaxScatterPerSpin, 0, 5);

        int odemeEgilimi = Clamp(
            (int)(carpan * 0.55f) +
            (int)(scatter * 0.20f) +
            (maxCarpan * 7) +
            (zorlaCarpan > 0 ? 10 : 0),
            0, 100);

        int odemeDagilimi = Clamp(
            (int)(scatter * 0.65f) +
            (int)(carpan * 0.25f) +
            (maxScatter * 4),
            0, 100);

        int minOdeme = bahis * maxScatter;
        if (scatter <= 10) minOdeme = 0;

        int tavanCarpan = ClampMin((maxCarpan * 6) + (zorlaCarpan > 0 ? zorlaCarpan / 5 : 0), 1);
        int maxOdeme = bahis * tavanCarpan;
        if (maxOdeme < minOdeme) maxOdeme = minOdeme;

        return new Sonuc
        {
            OdemeEgilimiYuzde = odemeEgilimi,
            OdemeDagilimiYuzde = odemeDagilimi,
            MinOdemeTL = ClampMin(minOdeme, 0),
            MaxOdemeTL = ClampMin(maxOdeme, 0)
        };
    }

    private static int Clamp(int value, int min, int max)
    {
        if (value < min) return min;
        if (value > max) return max;
        return value;
    }

    private static int ClampMin(int value, int min)
    {
        return value < min ? min : value;
    }
}
