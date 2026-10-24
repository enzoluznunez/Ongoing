# Ongoing

A mixed-reality data visualization for Meta Quest 3, built in Unity, for exploring
cyberattacks on publicly traded US companies. In passthrough, a map of the United States
and a 3D bar chart of breach data (the **sheet**) stand in front of you. You move between
the country, its six regions and their metros on the map; the sheet beside it shows that
place's breaches. You reshape the sheet with your hands, and a voice assistant can drive
the same tools.

- [What you will see](#what-you-will-see)
- [Using the app](#using-the-app)
- [Setting up a build](#setting-up-a-build)
- [FAQ](#faq)

## What you will see

When the app starts, a **Loading** line floats ahead of you while every sheet is fetched
and prepared. When it finishes, two things appear in front of you:

- **The map panel.** A flat white map, the United States first, its six regions filled in
  their colours. Below it, the **Region** slider steps through the maps: the United States,
  then each region on its own, west to east. Up its left side, the **Scale** slider makes
  the maps larger or smaller. A region's map shows its metros as round pins, each sized by
  its number of breaches. A metro is a city, or several neighbouring cities whose pins
  would overlap, merged into one and named after the largest.
- **The sheet.** One row per breach, titled by the company's ticker and disclosure date,
  and a column for each of six figures: revenue, net income, assets, book value, market
  cap and stock price. Each bar is as tall as that figure's place among every US breach,
  so bars compare across every sheet. Each row is coloured by its metro, as its pin is on
  the map.

What the sheet shows follows the map:

| Map | Nothing selected | Something selected |
|---|---|---|
| United States | Every US breach, coloured by region | A region's breaches (poke the region) |
| A region | Every breach in that region, coloured by metro | One metro's breaches (poke its pin) |

Each region colours its ten largest metros. The rest are drawn in greys, on the map and on
the sheet.

## Using the app

The app is used with your hands; put the controllers down. Everything responds to a
fingertip **poke** (push a fingertip into it from the front) or a **pinch** (thumb and
index finger together, or a full-hand grip) to grab and move.

### Wrist buttons

Two buttons float over your **left hand** whenever it is tracked:

- **Map Panel** opens and closes the map panel.
- **Tool Panel** opens and closes the tool panel.

A button is filled black while its panel is open. The sheet belongs to neither panel; it
stays up whichever is open.

### The map panel

- **Change map:** poke the Region slider's thumb and drag it along the bar. Let go and it
  settles on the nearest map, and the sheet changes to that map's. Each map remembers its
  selection: come back to a map and what you had selected on it is selected again, with
  its sheet as you left it.
- **Select:** on the United States map, poke a region. On a region's map, poke a metro's
  pin. The selection is edged in black and the sheet changes to it.
- **Deselect:** poke the selected region or pin again, or poke the map away from any
  region or pin. The sheet goes back to the whole map's.
- **Resize:** poke the Scale slider's thumb and drag it up (larger) or down (smaller).
- **Move:** pinch anywhere on the panel and carry it. Pinch with both hands and twist to
  turn it.

### The tool panel

Poke a tool to select it; poke it again to put it down. Each tool's subpanel describes it
in a sentence.

| Tool | What it does | How |
|---|---|---|
| **Move** | Moves the sheet anywhere in the room | Pinch the sheet with one hand and carry it |
| **Rotate** | Turns the sheet to face any direction | Pinch it with both hands and twist |
| **Scale** | Makes the sheet larger or smaller | Pinch it with both hands and move them apart or together |
| **Profile** | Raises rows or columns above the sheet so they stand out | Press a bar, then sweep your fingertip along the row or column. Do it again on a raised line to lower it |
| **Sort** | Reorders the rows and columns | Pinch low on the bars to take a row, high on the bars to take a column, and slide it to its new place |
| **Filter** | Hides or shows rows and columns | Poke low on the bars to hide that row, high to hide that column. Poke the empty place where it stood to bring it back |
| **Assistant** | Turns the voice assistant on or off | See below |

**Undo** reverses the last change; **Undo All** reverses every change to the current
sheet. Each sheet keeps its own changes, so switching to another sheet and back finds it
as you left it.

### The voice assistant

Poke **Assistant** on the tool panel and allow the microphone when asked. While the
button is filled black it is listening; speak naturally. It can:

- Open sheets and maps ("show me the West", "open the Seattle metro").
- Use every tool ("sort the rows by market cap", "hide net income", "raise the third row").
- Answer questions about what is on the sheet: counts, averages, totals, comparisons.
- Open and close the panels, place the tool panel, and resize the map.

The assistant's subpanel has a speed setting for its changes: **0.5 m/s**, **1 m/s** or
**Instant**. Poke **Assistant** again to turn it off.

### Putting things back in front of you

The map and sheet stay where you leave them. If you take the headset off for five seconds
or more, or recenter from the Quest menu (hold the Meta button), the view turns and
shifts so the layout is in front of you again, at the height it first opened at.

## Setting up a build

### Prerequisites

- [ ] A Meta Quest 3 with a USB-C cable
- [ ] A Google Gemini API key on a paid tier (only the assistant needs it)
- [ ] Internet access on the headset (only the assistant needs it)

### Prepare the Meta Quest 3

1. Pair the headset with the Meta Horizon app on your phone.
2. Create or join an organization at developer.meta.com and verify your account.
3. In the Horizon app, open your headset's settings, turn on **Developer Mode**, and
   restart the headset.
4. Plug the headset into your computer, put it on, and accept **Allow USB debugging**.
   Check *Always allow from this computer*.
5. Run **Space Setup**. The app uses passthrough and reads your room's scene data.

### Install Unity

1. Install Unity Hub and sign in or create an account.
2. Install Unity **6000.4.0f1**.
3. During installation, check **Android Build Support** and both of its sub-modules,
   **OpenJDK** and **Android SDK & NDK Tools**.

### Open the project

1. Clone the repository, or download the ZIP and unpack it.
2. In Unity Hub choose **Add → Add project from disk**, select the folder, and open it
   with 6000.4.0f1.
3. Open `Assets/Scenes/File Reader.unity`.

### Add the datasets

Everything the app shows is read from files in `Assets/StreamingAssets`, packed into the
build; nothing is fetched from a server.

```
Assets/StreamingAssets/
  map.json                  regions, their colours, and every city's pin
  Sheets/United States.csv  the national sheet
  Sheets/<region>.csv       one per region in map.json, such as "US West.csv"
  Sheets/<metro>.csv        one per metro in map.json, such as "Boston.csv"
```

`map.json` is `{"regions": [{"region", "color", "breaches"}], "cities": [{"city",
"region", "metro", "color", "lat", "lon", "breaches", "charted"}]}`, colours as `#rrggbb`.
A sheet is a CSV whose first header cell is `Rows / Columns`; an ordinary CSV works, and
optional leading `#metro`, `#city`, `#color` and `#heights` lines add row colours and
bar heights.

### Add your Gemini API key

Create a Gemini API key in [Google AI Studio](https://aistudio.google.com/apikey). Put it on one line, and
nothing else, in `Assets/StreamingAssets/gemini.key`. Git ignores this path too.

Without it everything works except the assistant, which turns itself off when started.

### Build to the headset

1. Connect the headset by USB and put it on, so it stays awake.
2. Open **File → Build Profiles**, select **Quest Default**, and choose your headset in
   the device list. Platform, architecture and SDK levels are already set in that
   profile; change nothing.
3. Click **Build And Run**. The first build takes 10 to 30 minutes; later builds are much
   quicker.
4. In the headset, accept the microphone prompt. Denying it leaves everything working
   except the assistant.

You are done when the loading line finishes and the map of the United States and its
sheet stand in front of you in passthrough. A **No Map** notice means
`Assets/StreamingAssets/map.json` was missing when the app was built, not that the build
failed.

## FAQ

### Getting started

**What is this for?**
Seeing, side by side, where breaches at publicly traded US companies happened and how
those companies compared financially when they were breached. The map shows where; the
sheet shows the figures.

**Where does the data come from?**
For now, placeholder datasets in `Assets/StreamingAssets` (see
[Add the datasets](#add-the-datasets)).

**Do I need to know Unity to use it?**
No. Setting up a build needs Unity once; using the app needs only the headset.

**Can I try it without a headset?**
Not meaningfully. Hand input and passthrough are the interface.

**Does it work sitting down?**
Yes. Everything opens at the height of your head when it first appears. If you change
between sitting and standing, take the headset off for five seconds or recenter, and the
layout comes back to eye level.

**Why does it take a few seconds to start?**
The loading line reads every sheet and builds everything it needs up front, so that
nothing has to load or be built while you are using the app. Switching between sheets is
smooth because of it.

### The map

**Why are some pins grey?**
Each region colours only its ten largest metros, so the colours stay easy to tell apart.
The rest are drawn in greys, darker for more breaches. Their rows on the sheet are grey
too.

**I poked a pin and got a "No Figures" notice.**
None of that metro's breaches reported all six figures, so it has no sheet. The sheet
stays as it was.

**The sliders do not respond.**
Bring your fingertip to the thumb (the long button on the slider) from in front and press
into it; then drag along the bar. A pinch does not take it.

### The sheet

**What does a bar's height mean?**
Where that figure ranks among every US breach. A tall bar is near the top of all US
breaches for that figure, not just the top of this sheet, so the same company looks the
same on every sheet.

**What do the colours mean?**
On a region's or a metro's sheet, each row is its metro's colour, as on the map. On the
United States sheet, each row is its region's colour. Within a row, brighter bars rank
higher.

**The row and column titles disappeared.**
They hide when the sheet is scaled smaller than its starting size, where they would be
too small to read. Make it larger with the Scale tool and they come back.

**I cannot grab the sheet.**
Select Move, Rotate or Scale on the tool panel first. With any other tool, or none, the
sheet stays put so it is not moved by accident.

**I moved the sheet. Where does the next one open?**
Each sheet remembers its own moves. One you have moved opens where you left it; one you
have not opens to the right of the map, level with the Region slider.

**Why does the sheet sink and rise when I switch?**
The old sheet sinks away and the new one rises in the same place, so you can see the
switch happen.

### The tools

**How does Filter know whether I mean a row or a column?**
By height: poking low on the bars means the row, high means the column. Poking just past
the end of a row or column always means that line.

**How long do raised lines stay up?**
Until you lower them, even when you change tools. For a raised line's average, minimum,
maximum or sum, ask the assistant.

### The assistant

**It turned itself off.**
A notice says why: the microphone is not allowed, it lost its connection, or the
conversation grew too long for it to hold. Poke Assistant to start a fresh conversation.

### Troubleshooting

**The app says "No Map" and nothing loads.**
`Assets/StreamingAssets/map.json` was missing or unreadable when the app was built.

**A "Sheet Unavailable" notice appeared.**
That one sheet's file in `Assets/StreamingAssets/Sheets` is missing or could not be
read. The current sheet stays up.

**The loading line never finishes.**
It gives up waiting after 45 seconds and opens with whatever loaded.

**The wrist buttons are missing.**
They show only while your left hand is tracked. Hold it up in view of the headset.

**Everything went dim or grey.**
That is usually the Quest itself: its menu or a notification is open, or you are close to
your play-area boundary. Close the menu or step back toward the middle of your space.

**A panel disappears when I lean very close.**
Anything within about 2 cm of your eyes is not drawn. Lean back a little.

### Cost and privacy

**Does it cost anything?**
Only the Gemini API, for whatever the assistant is used. The data ships with the app.

**Does the app store anything about me?**
No. It reads breach data, and sends your speech to the assistant only while the assistant
is on.
