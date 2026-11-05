# Servis odası yüzeyleri — kaynak ve lisans

5 Kasım 2026'da Poly Haven'ın herkese açık API'sinden indirilen orijinal CC0 doku haritalarıdır. Projeye dosya olarak eklenmiştir; çalışma sırasında internet, ücretli paket veya ek Unity paketi gerekmez.

| Set | Yazarlar | Fiziksel tekrar | Kullanım |
| --- | --- | --- | --- |
| [Concrete Floor Worn 001](https://polyhaven.com/a/concrete_floor_worn_001) | Dimitrios Savva (fotoğraf), Rico Cilliers (işleme) | 3 × 3 m | İlk servis odası ve K-07 geçidinin zemini |
| [Concrete Wall 003](https://polyhaven.com/a/concrete_wall_003) | Dimitrios Savva (fotoğraf), Rico Cilliers (işleme) | 3 × 3 m | İlk odanın beton duvar yüzeyleri |
| [Green Metal Rust](https://polyhaven.com/a/green_metal_rust) | Rob Tuytel | 1 × 1 m | Boyalı alt duvar sacı, pano, kapı ve çelik ayrıntılar |

Her setin 1024 × 1024 JPG temel renk (`diff`), OpenGL normal (`nor_gl`) ve birleşik AO/pürüzlülük/metallik (`arm`) haritası bulunur. ARM sırası: R = ambient occlusion, G = roughness, B = metallic. Normal haritaları Unity'de Normal Map, ARM haritaları doğrusal veri olarak içe aktarılır. UV'ler metre cinsinden mesh boyutlarından türetilir; uzun duvarlar boyunca doku esnetilmez.

Metal setinin özgün yeşil rengi, `ServiceSurface.shader` içindeki doygunluk ve renk çarpanıyla kirli krem, kömür grisi ve güvenlik sarısına uyarlanmıştır. Kaynak JPG'ler değiştirilmemiştir. Yüzey shader'ı, mesh'ler ve elektrik panosu bu proje için kodla oluşturulmuştur; insan modeli içermez.

Lisans: [Poly Haven — CC0](https://polyhaven.com/license), [CC0 1.0 Public Domain Dedication](https://creativecommons.org/publicdomain/zero/1.0/). Bu lisans doku dosyalarına aittir; projenin mevcut koduna genel bir lisans atamaz. İndirme adresleri, kaynak ölçüleri, yazarlar, resmi MD5 ile doğrulanmış indirme ve SHA-256 değerleri [Sources.json](Sources.json) içindedir.

`ServiceRoomAssets.cs`, yalnız bu klasördeki dokuların içe aktarım ayarlarını hazırlar ve eksik materyalleri `Assets/Resources/ServiceRoom/` altında üretir. Var olan materyallerde kullanıcının yaptığı ayarları yeniden yazmaz. Aynı işlem Unity'de **YazLab → 7 - Servis Odası Malzemelerini Hazırla** menüsünden çalıştırılabilir.

## Eksik insan modelleri

Projede kullanılabilir insan modeli, Avatar, prefab veya animasyon bulunmadığından mevcut oyuncu/Sera görselleri bu turda değiştirilmemiştir; yeni kapsül veya kutu insan üretilmemiştir. Bağlama için şu dosyalar gerekir:

- `Assets/Art/Characters/Player/Player.fbx`: rig'li, Unity Humanoid uyumlu oyuncu; ayaklar yer hizasında, metre ölçeğinde ve T/A pozunda referans iskelet.
- `Assets/Art/Characters/Sera/Sera.fbx`: aynı koşulları sağlayan nöbetçi modeli. Kullanıma hazır `.prefab` ve ona bağlı tüm model/malzeme/doku dosyaları da uygundur.
- Varsa `Player_Idle.fbx` ve `Player_Walk.fbx`: uyumlu Humanoid rig üzerinde, yerinde döngü animasyonları. Unity `.anim` klipleri de uyumlu Avatar/iskeletle birlikte kullanılabilir.
- PNG/TGA temel renk ve normal dokuları; varsa roughness/metallic haritaları. Kaynak adresi, yazar ve model/animasyon lisansını belirten `LICENSE.txt`.

Gelecek bağlama mevcut `Oyuncu` ve `Nöbetçi Sera` nesnelerine görsel çocuk eklemeli; model çarpıştırıcıları devre dışı ve `Animator.applyRootMotion=false` olmalıdır. Durma/yürüme, gerçek yatay `CharacterController.velocity` üzerinden sürülmelidir; böylece yan görev yavaşlaması ve konuşma sırasında durma doğru yansır. Bu dosyalar yokken animasyon veya model bağlama yapılmış sayılmaz.
