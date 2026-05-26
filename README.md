# PuxDesign - detekce změn v adresáři

Testovací úloha pro firmu PuxDesign na pozici web developera.

## Zadání

CVIČNÝ ÚKOL - Hledáme šikovného web developera!

Napište jednoduchý program, který bude umět detekovat změny v lokálním adresáři uvedeném na
vstupu. Při prvním spuštění si program obsah daného adresáře analyzuje a při každém dalším
spuštění bude hlásit změny od svého posledního spuštění, tj:
a) seznam nových souborů,
b) seznam změněných souborů (změnou se rozumí změna obsahu daného souboru),
c) seznam odstraněných souborů a podadresářů.
U každého souboru evidujte číslo jeho aktuální verze (na začátku budou mít všechny soubory verzi 1,
s každou detekovanou změnou daného souboru bude jeho verze navýšena o 1).
Program realizujte jako jednoduchou ASP.NET aplikaci naprogramovanou v C#. UI vytvořte jako
webovou aplikaci dle své volby (Core MVC, MVC, REST API)
Můžete předpokládat, že velikost souborů v adresáři bude do 50 MB a že počet souborů v každém
adresáři bude nanejvýš 100.
Program se bude spouštět ručně z UI stiskem tlačítka (nedetekujte změny filesystému automaticky).
Pro perzistenci dat nepoužívejte databázi.
UI bude obsahovat alespoň textbox (textový input) pro zadání cesty k analyzovanému adresáři,
tlačítko pro spuštění analýzy a výpis jejího výsledku.
Své řešení stručně popište a zmiňte i jeho případná omezení.

[Zadání v PDF](./zadani.pdf)
	 
## Moje řešení

Řešení je postavené na **.NET 8** jako **ASP.NET Core Web API** a frontend je vytvořený v **Reactu**.

Frontend komunikuje s backendem přes **REST API** a pro jednoduché UI jsem použil **Bootstrap**.

Data se neukládají do databáze. Stav analyzovaných adresářů se ukládá do **JSON souborů**.

Aplikace umí pracovat s více složkami. Již analyzované složky se zobrazují v UI a lze je znovu načíst.

Změna obsahu souboru se detekuje pomocí **SHA-256 hashe**. Verze souboru se navýší pouze tehdy, když se změní jeho obsah.

Umístění JSON snapshotů je nastavitelné v `appsettings.json`.

## Spuštění

Backend:

```powershell
dotnet run --project PuxDesign.Server
````

Frontend:

```powershell
cd puxdesign.client
npm install
npm run dev
```
