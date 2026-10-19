# Tilt Mirror — Unity boshlang'ich loyiha

Unity 2021.3 LTS yoki yangiroq (Unity 6 ham mos), 2D. Tashqi paket kerak emas, faqat TextMeshPro (UI uchun, ixtiyoriy).

## Papkalar

```
Assets/
  Scripts/
    Core/MirrorSim.cs        Sof C# o'yin mantiqi (Unity'ga bog'liq emas) + BFS yechuvchi
    Data/LevelData.cs        Bitta level (ScriptableObject)
    Data/LevelDatabase.cs    Levellar ro'yxati
    Data/ProgressService.cs  Yulduzlar va joriy level (PlayerPrefs)
    Visual/Palette.cs        Dizayndagi ranglar
    Visual/SpriteFactory.cs  Rasm fayllarsiz, kod bilan yaratiladigan spritelar
    Visual/BoardView.cs      Ikki maydon, yarim doiralar, animatsiya, tilt
    Game/GameController.cs   Holat, qoidalar, undo, g'alaba, klaviatura
    UI/SwipeInput.cs         Swipe boshqaruvi
    UI/HudView.cs            Level / yurishlar / par
    UI/WinPanel.cs           JOINED paneli, yulduzlar
  Editor/
    SampleLevels.cs          12 va 13-level (dizayn kanvasidagi)
    LevelGeneratorWindow.cs  Level generator + Level Database yig'ish
    MirrorSimSelfTest.cs     Mantiq uchun tezkor test
```

## 5 daqiqada ishga tushirish

1. Yangi 2D loyiha oching va `Assets` papkasini ko'chirib o'tkazing.
2. Menyu: **Tilt Mirror > Run Logic Self-Test**. Console'da "all checks passed" chiqishi kerak.
3. Menyu: **Tilt Mirror > Create Sample Levels (12, 13)**. Buni bir marta bosing, qayta bossangiz nusxalar paydo bo'ladi. `Assets/Levels/` ichida levellar va `LevelDatabase` yaratiladi.
4. Sahnada Main Camera'ni **Orthographic** qiling. Bo'sh GameObject yarating, unga **GameController** qo'shing, `Database` slotiga `Assets/Levels/LevelDatabase` ni sudrang.
5. Play bosing. Strelka tugmalari bilan yuring, **Z** undo, **R** restart.

Bu qadamda UI shart emas: board, qahramonlar, animatsiya va g'alaba mantiqi ishlaydi.

## UI ulash (ixtiyoriy)

- **Window > TextMeshPro > Import TMP Essential Resources**.
- Canvas yarating. Unga `HudView` (3 ta TMP matn) va `WinPanel` (root, 3 ta Image yulduz, 2 ta matn) qo'ying, GameController'ga sudrang.
- WinPanel ichidagi Replay tugmasi `GameController.Restart`, Next tugmasi `GameController.Next` ga ulanadi.
- Butun ekran Image'ga (Raycast Target yoqilgan) `SwipeInput` qo'ying. Sahnada `EventSystem` bo'lishi shart.
- D-pad tugmalari: `MoveUp`, `MoveDown`, `MoveLeft`, `MoveRight`.

## Qoidalar (kodda)

- Koordinata: x o'ngga, y **pastga** o'sadi (0-qator tepada).
- Yurish: qahramon devor yoki chetga yetguncha **sirpanadi**.
- Teskari: `Left/Right` da Oy Quyoshning teskarisiga yuradi. `Up/Down` ikkalasida bir xil.
- G'alaba: bitta yurishdan keyin ikkalasi ham o'z halqasida turishi kerak.
- Yulduz: par yoki undan kam = 3, par+3 gacha = 2, qolgani = 1.
- Hech narsa siljimasa (ikkalasi ham to'siqda) yurish sanalmaydi.
- Undo yurishni qaytarmaydi, shuning uchun yulduzni aldab bo'lmaydi.

## Level generator

**Tilt Mirror > Level Generator**: tasodifiy devorlar qo'yadi, BFS bilan yechilishini tekshiradi va par (eng qisqa yechim) berilgan oraliqda bo'lganlarini `Assets/Levels` ga saqlaydi. Tasodifiy level hali qiziqarli level degani emas, hammasini qo'lda ko'rib chiqing.

## Eslatma

Bu kod Unity'da hali kompilyatsiya qilib ko'rilmagan (yozilgan muhitda C# kompilyator yo'q edi). Mantiq (`MirrorSim`) dizayn bosqichida Python'da tekshirilgan BFS bilan bir xil algoritm. Kompilyatsiya xatosi chiqsa, Console matnini yuboring.
