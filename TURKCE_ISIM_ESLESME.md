# Türkçe İsim Eşlemesi (Kalanlar)

Aşağıdaki eşleme, "dosya / sınıf / arayüz / metod" türkçeleştirmesi için referanstır.  
**Yapılanlar:** GameGuardService→OyunKorumaServisi, DifficultyService→ZorlukServisi, CollapseFlowService→CokmeAkisServisi, GridInitService→IzgaraBaslatmaServisi, GameBootstrapService→OyunBootstrapServisi, MultiplierPlacementService→CarpanYerlestirmeServisi, GameUIUpdateService→OyunUIGuncellemeServisi, SpinFlowService→DonusAkisServisi (IDonusAkisBaglami), ScatterEffectService→ScatterEfektServisi (IScatterEfektBaglami), TumbleFlowService→TumbleAkisServisi (ITumbleAkisBaglami), SpinService→DonusServisi, TumbleService→TumbleServisi, GridService→IzgaraServisi, EconomyService→EkonomiServisi, AnimationService→AnimasyonServisi, ScenarioService→SenaryoServisi, PayoutService→OdemeServisi, MultiplierService→CarpanServisi, SpinRecordService→DonusKayitServisi, SpeedAndSfxService→HizVeSesServisi, GameFormatService→OyunFormatServisi, SceneWiringService→SahneBaglamaServisi, AdminTuningUIService→AdminAyarUIServisi, LogService→LogServisi, UIService→UIServisi, BonusUIFlowService→BonusUIAkisServisi, BonusBuyFlowService→BonusSatinAlmaAkisServisi, CarpanOverlayService→CarpanOverlayServisi, CoroutineService→KorutinServisi.

## Servisler (sınıf + dosya)

| Eski (EN) | Yeni (TR) |
|-----------|-----------|
| CollapseFlowService | CokmeAkisServisi |
| ICollapseFlowContext | ICokmeAkisBaglami |
| GridInitService | IzgaraBaslatmaServisi |
| IGridInitContext | IIzgaraBaslatmaBaglami |
| GameBootstrapService | OyunBootstrapServisi |
| IGameBootstrapContext | IOyunBootstrapBaglami |
| MultiplierPlacementService | CarpanYerlestirmeServisi |
| ICarpanPlacementContext | ICarpanYerlestirmeBaglami |
| GameUIUpdateService | OyunUIGuncellemeServisi |
| IGameUIUpdateContext | IOyunUIGuncellemeBaglami |
| SpinFlowService | DonusAkisServisi |
| ISpinFlowContext | IDonusAkisBaglami |
| SpinService | DonusServisi |
| TumbleFlowService | TumbleAkisServisi |
| ITumbleFlowContext | ITumbleAkisBaglami |
| TumbleService | TumbleServisi |
| GridService | IzgaraServisi |
| EconomyService | EkonomiServisi (zaten EkonomiAyarlari var; servis adı) |
| AnimationService | AnimasyonServisi |
| ScenarioService | SenaryoServisi |
| PayoutService | OdemeServisi |
| SpinRecordService | DonusKayitServisi |
| SceneWiringService | SahneBaglamaServisi |
| AdminTuningUIService | AdminAyarUIServisi |
| ScatterEffectService | ScatterEfektServisi |
| GameFormatService | OyunFormatServisi |
| CarpanOverlayService | CarpanOverlayServisi |
| MultiplierService | CarpanServisi |
| BonusUIFlowService | BonusUIAkisServisi |
| BonusBuyFlowService | BonusSatinAlmaAkisServisi |
| SpeedAndSfxService | HizVeSesServisi |
| LogService | LogServisi |
| CoroutineService | KorutinServisi |
| UIService | UIServisi |

## MonoBehaviours (sahne referansı kırılır; dikkatli ol)

| Eski | Yeni (öneri) |
|------|----------------|
| GameManager | OyunYonetici (veya kalabilir) |
| AdminPanel | AdminPaneli |
| PlayerProfile | OyuncuProfili |
| SpinIconRotate | DonusIkonuDondur |
| SaveSystem | KayitSistemi |
| StatsEntry | IstatistikKaydi |
| GameLogEntry | OyunLogKaydi |

**Ek (metod/arayüz):** IWiringTarget→IBaglamaHedefi, LoadEconomyFromGameManagerOrPrefs→EkonomiYukleGameManagerVeyaPrefs, SyncEconomyToGameManagerAndPrefs→EkonomiSenkronizeEt, SetContext→SetBaglam (tüm servisler). AutoWireUIIfNeeded→UIAutoBaglaGerekirse, SetAutoWireUIIfNeededImpl→SetUIAutoBaglaGerekirseImpl, GetOverlaysForAnimation→AnimasyonIcinOverlayleriAl.
