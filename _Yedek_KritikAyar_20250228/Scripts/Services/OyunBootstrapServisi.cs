/// <summary>
/// OyunBootstrapServisi için context arayüzü. OyunYoneticisi implement eder.
/// </summary>
public interface IOyunBootstrapBaglami
{
    void BootstrapMantiginiCalistir();
}

/// <summary>
/// Oyun başlangıç kurulumu: servis oluşturma ve delegate bağlama orkestrasyonu.
/// OY.Start() SyncFromAyarClasses + _bootstrap.Calistir() çağırır; asıl blok context.BootstrapMantiginiCalistir() ile çalışır.
/// </summary>
public class OyunBootstrapServisi
{
    private IOyunBootstrapBaglami _ctx;

    public void SetBaglam(IOyunBootstrapBaglami ctx)
    {
        _ctx = ctx;
    }

    public void Calistir()
    {
        if (_ctx == null) return;
        _ctx.BootstrapMantiginiCalistir();
    }
}
