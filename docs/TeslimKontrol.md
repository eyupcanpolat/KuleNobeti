# YazLab I · Proje 1 teslim kontrolü

Yönerge: Kocaeli Üniversitesi 2026–27 güz, YazLab I Proje 1, bölümler 2–7. Son teslim **6 Kasım 2026 Cuma 17.00**. Bu liste çalışma planıdır; denemeden kutu işaretlenmez.

| PDF maddesi | Ne ile doğrulanır? |
| --- | --- |
| §2 grup ve GitHub | Zamanında grup kaydı. Ekip tek repo ve tek teslim kullanır. Her üyenin kendi hesabıyla yaptığı gerçek katkılar görünür. PDF bir commit sayısı vermiyor. |
| §3.1 fiziksel engel | Q öncesi oyuncu turuncu kapıdan veya alçak tünelden geçemez. Q ile K-07 gerçek NavMesh yolu hesaplar, tünelden geçer, şalteri Raycast ile doğrular. |
| §3.1 sensör ve karar | F4: ajan konumu, en yakın engelin mesafesi/açısı ve şalter ölçümü. README: durum diyagramı ve ajan akışı. |
| §3.2 yerel NPC | E ile kendi cümleni yaz. Ollama gerçek cevap döndürür. Geçerli `open_gate` sonrası Esc ile çıkıp yeşil alana yürü. |
| §4.2 JSON ve OOP | Unity menüsü 2 ile eksik/bozuk JSON, tanımsız eylem ve düz metni denetle. README mimari tablosunda ajanın, NPC'nin, ağ istemcisinin ve kapının ayrı sorumluluklarını açıkla. |
| §4.3 asenkron ve hata | Model düşünürken Esc ile çıkıp hareket edebil. Ollama kapalı, model eksik ve zaman aşımı durumlarını ayrı kaydet. Hata sonrası doğru ayarla yeniden dene; kapı hata halinde açılmamalı. |
| §4.4 ajan testi | F8 → F1/F2/F3 → Q. Başarıları ve süreleri kaydet. Başarı oranını ve ortalama başarılı süreyi README'ye ekle. |
| §4.4 beş diyalog | README'deki beş gerçek girdiyi kullan; cevap, eylem, gecikme ve gözlenen kapı durumunu yaz. API betiği model yanıtlarını ayrı ölçer. |
| §5 rapor | Ad, ekip, tarih, senaryo, model seçimi ve istemi, Mermaid durum/ajan/istek akışları, hata stratejisi, gerçek sonuçlar, ekran görüntüleri, sınırlılıklar. README ortak GitHub reposunda görünür. |
| §2 ve §7 teslim | Windows build'i EXE ve yan dosyalarıyla ayrı klasörde başlat. Kaynak, README ve tam build tek ZIP'te. Teslimden önce ZIP'i tekrar açıp kontrol et. |

## İki kişilik çalışma sırası

- **Kişi A:** Unity'de ilk kapı → üç başlangıç → sensör ve rota kanıtı → Windows build ve EXE denemesi.
- **Kişi B:** Yerel Ollama → beş farklı diyalog → hata denemeleri → gerçek cevaplar, gecikmeler, ekran görüntüleri ve README.

Her üye kendi gerçek değişikliklerini kaydeder. Doğal commit örnekleri: kaynak tabanı, sensör düzenlemesi, ajan test başlangıçları, model test betiği, ajan ölçümleri, diyalog ve hata sonuçları, ekran görüntüleri, teslim doğrulaması. Aynı dosyayı değiştirecekseniz önce son değişiklikleri alın; oturumu açık bir editörün üstüne dosya kopyalamayın. GitHub'a Unity'nin geçici klasörlerini, model dosyalarını veya EXE çıktısını koymanız gerekmiyor; çalışan Windows build teslim ZIP'inde bulunur.
