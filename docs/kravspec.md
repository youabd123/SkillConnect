# Kravspecifikation för SkillConnect

SkillConnect är en marknadsplats för tjänster. En utförare publicerar en tjänst och lägger upp lediga tider. En kund hittar tjänsten, väljer en tid och får bokningen bekräftad direkt.

## Funktionella krav för första versionen

| ID | Krav |
| --- | --- |
| K1 | Besökare kan bläddra bland publicerade tjänster, filtrera på kategori och öppna en tjänsts detaljsida. |
| K2 | Kunder och utförare kan registrera sig och logga in. Endast inloggade kunder kan boka och endast inloggade utförare kan publicera tjänster och lägga upp tider. |
| K3 | Utförare kan skapa, redigera och publicera egna tjänster. En tjänst har ett namn, en beskrivning och en kategori. |
| K4 | Utförare kan lägga upp enskilda lediga tider för sina egna tjänster. Varje tid har datum, starttid och sluttid. Sluttiden måste ligga efter starttiden. |
| K5 | Tjänstens detaljsida visar kommande lediga tider. Kunden kan välja och boka en tid på en publicerad tjänst. |
| K6 | En lyckad bokning bekräftas direkt och tiden försvinner från listan över lediga tider. En tid kan bara bokas en gång, även vid samtidiga försök. |
| K7 | Kunden kan se sina egna bekräftade bokningar. Utföraren kan se bokningar för sina egna tjänster. |

## Acceptanskriterier

- När en utförare publicerar en tjänst med en framtida ledig tid kan en besökare hitta tjänsten via dess kategori och se tiden på detaljsidan.
- När en inloggad kund bokar en ledig tid visas en bekräftelse. Bokningen visas under **Mina bokningar**, och tiden visas inte längre som ledig.
- Om två kunder försöker boka samma tid samtidigt skapas exakt en bokning. Den andra kunden får besked att tiden har tagits och kan välja en annan tid.
- En kund kan inte läsa en annan kunds bokningsöversikt. En utförare kan inte ändra en annan utförares tjänst eller tider. API:t kontrollerar behörigheten för varje sådan åtgärd.

## Öppna beslut och avgränsningar

- Regler för avbokning, ombokning och överlappande tider mellan en utförares tjänster behöver beslutas innan de implementeras.
- Betalning, chatt, recensioner och återkommande tider ligger utanför första versionen.

## Teknik

- Backend med ASP.NET Core och SQL Server.
- Frontend med React och TypeScript.
- Backend, frontend, tester och dokumentation ligger i samma repository.
