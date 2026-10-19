# Tilt Mirror: assetlar

Hammasi original: kod bilan generatsiya qilingan (grafika va ovoz), hech qanday uchinchi tomon rasm yoki sample ishlatilmagan. `Art/_preview/contact_sheet.png` da hammasi bir varaqda ko'rinadi.

## Joylashuvi

```
Assets/
  Resources/TiltMirror/            <- papka nomi aynan shunday bo'lishi shart (kod Resources.Load qiladi)
    Sprites/   (256 px = 1 unit)
      sun_half.png, moon_half.png      Qahramonlar (yarim doira, gradient, yaltirash)
      sun_ring.png, moon_ring.png      Maqsad halqalari (punktir)
      wall_sun.png, wall_moon.png      Devorlar
      seam_glow.png                    Tikuv atrofidagi yorug'lik (g'alabada yonadi)
      eye_dot.png                      Ko'z (oq, kod rang beradi)
    UI/        (100 px = 1 unit)
      logo_disc.png                    Birlashgan doira (WinPanel va splash uchun)
      ui_back, ui_restart, ui_undo, ui_lock, ui_star, ui_play,
      ui_settings, ui_levels, ui_sound_on, ui_sound_off   Oq ikonkalar (Image.color bilan bo'yaladi)
      ui_round_rect.png                9-slice (border 48), tugma/karta foni
      ui_circle.png                    Aylana
      dpad_up/down/left/right.png      Oltin + mint strelkalar (tayyor ranglarda, bo'yamang)
    Audio/
      sfx_move, sfx_blocked, sfx_join, sfx_star_1..3, sfx_click, sfx_undo
      music_ambient_loop.wav           32 soniya, uzluksiz sikl
  Art/AppIcon/
    AppIcon_1024.png                   iOS (alfa kanalsiz)
    PlayStore_Icon_512.png             Google Play
    AndroidAdaptive_Background_432.png, _Foreground_432.png, _Monochrome_432.png
  Art/_preview/contact_sheet.png
```

## Qanday ishlaydi

- `TiltMirrorAssetImporter` (Editor) `Resources/TiltMirror` ichidagi PNG'larga o'zi to'g'ri import sozlamasini beradi: Sprite, mipmap yo'q, game sprite 256 PPU, UI 100 PPU, `ui_round_rect` uchun 9-slice.
- `SpriteFactory` avval shu PNG'larni oladi, topolmasa kodda oddiy oq sprite yasaydi. Shuning uchun fayllarni o'chirsangiz ham o'yin ishlayveradi.
- `AudioManager` ovozlarni `Resources/TiltMirror/Audio` dan oladi, GameController uni o'zi yaratadi. Musiqa avtomatik chalinadi. Ovozni o'chirish: `AudioManager.Instance.Muted = true` (saqlanadi). Tugma `ui_sound_on/off` uchun tayyor.

## App ikonkani ulash

**Project Settings > Player > Icons**
- iOS: "App Store" (1024) ga `AppIcon_1024.png`.
- Android: Adaptive (API 26+) ga Background, Foreground, Monochrome fayllarini qo'ying (Monochrome Android 13 tematik ikonka uchun). Legacy uchun `AppIcon_1024.png` yetadi.
- Play Console'ga `PlayStore_Icon_512.png` yuklang.

## Qo'shilmagan narsa

- **Shriftlar.** Bu muhitdan internetga chiqib bo'lmadi, shuning uchun Unbounded va DM Sans'ni yuklay olmadim. Ikkalasi Google Fonts'da bepul (OFL): fonts.google.com. Unbounded Bold/Black va DM Sans Regular/Bold'ni yuklab, TextMeshPro'da *Font Asset Creator* orqali asset yarating.
- **Do'kon grafikalari** (Google Play feature graphic 1024x500, skrinshotlar). Shriftlar kelgach yasash oson.

## Qayta generatsiya

Rang yoki shakl o'zgarsa, generatorlar loyiha bilan birga: `Tools/make_graphics.py` va `Tools/make_audio.py` (Python 3, Pillow, numpy, scipy). Yo'llarni skript boshida o'zgartiring.
