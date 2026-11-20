# Accounting Immersive Analytics

A mixed-reality app for Meta Quest 3, built in Unity, that helps people new to accounting read
business data. In passthrough, you open a document in the Documents panel. A PDF shows its pages,
and each table in it becomes a flat 2D chart in a column beside the panel; a CSV file becomes a
column of charts. Ada, a voice assistant, can open
documents, pin and move charts, sort and filter them, and answer questions about their data.

- [What you will see](#what-you-will-see)
- [Using the app](#using-the-app)
- [Setting up a build](#setting-up-a-build)
- [FAQ](#faq)

## What you will see

When the app starts, a **Loading** line floats ahead of you while everything gets ready. Then the
Documents panel opens the first document in `StreamingAssets/Documents`, in file-name order. Today
that is *01 Sales Review 2025*, one of five practice reports from a made-up business, Maple Street
Bakery. Each is two US Letter pages with two data tables. A column of every document's first page
stands to the left of the panel. When a document is open, its charts appear in a scrolling column to
the right of the panel, at half size, without keys: one per table in a PDF, of the kind the table's
caption names, or one per insight in a CSV file.

Charts are flat white cards with black text and gridlines, a title, axis titles and tick values, and
coloured marks. Press a card in the column to pin it: a full-size copy of the card appears just in front
of the Documents panel, and the card in the column gets a black outline. Only one chart is pinned at a
time; press another card and it takes the place of the first, or press the outlined card again to unpin it.

| Chart | Shows | Uses |
|---|---|---|
| **Bar** (vertical or horizontal) | A value for each category, optionally split into groups | category, value, group |
| **Line** | Change over time, one line per group | x, y, group |
| **Donut** | Parts of a whole, with the total in the middle | category, value |
| **Scatter** | How two numbers relate, one dot per row | x, y, group |
| **Bubble** | A scatter where dot area shows a third number | x, y, size, group |
| **Histogram** | How one number is spread | value |
| **Heatmap** | A value across two sets of categories | x, y, value |
| **Box plot** | How spreads compare across categories | category, value |
| **Treemap** | Parts of a whole with many items | item, group, value |

Which charts a file gets is planned by Gemini Flash-Lite. It reads the column names, their ranges and
the first few rows, and suggests one chart per useful insight, with plain-English titles. Each plan is
checked against the data (for example at most 40 bars, 12 donut slices and 10 groups), and any chart
that doesn't fit is dropped. Without Gemini, simple rules pick a line, bar, donut, box plot, scatter and
histogram from the columns.

## Using the app

The app is used with your hands; put the controllers down. Everything responds to a fingertip
**poke** (push a fingertip into it from the front) or a **pinch** (thumb and index finger together).
Everything also works from a distance: point at it and pinch. See
[Pointing from a distance](#pointing-from-a-distance).

### Wrist buttons

Two icon buttons float over your left hand whenever it is tracked:

- **Assistant** turns Ada on and off. While Ada is on but not yet connected, the icon is replaced by
  *Connecting* with pulsing dots.
- **Documents** opens and closes the Documents panel.

Every button in the app is an icon with no text: white with a black border and a black icon at rest,
and black with a white icon while it is on or its panel is open. Buttons don't change colour when you
point at them. Instead, pointing at a button or bringing a fingertip near makes it rise about 3 mm
toward you. Pressing it sinks it back flat and swaps its colours. Buttons that can't be used right now
are faded and don't rise.

The buttons that float beside a pinned chart have no border: white with a black icon at rest, and black
with a white icon when on. While one of them is on, the others are faded and can't be pressed.

### The Documents panel

The panel's title is the name of the open document, and blank when nothing is open. The name shows
only in the title bar, not again in the panel's body. The documents are the PDF and CSV files in
`StreamingAssets/Documents`. Press a page in the documents column on the panel's left to open that
document, or ask Ada. The column shows each document's first page at half the size it has in the
panel, as the chart column shows charts at half their pinned size; it scrolls like the chart column,
and the open document has a black outline. A document that can't be shown as pages, such as a CSV file, appears as a blank US Letter
page saying *Cannot be displayed*. Press the outlined page to close the document: the panel goes
back to US Letter size and says *Select a document*.

The panel is at least the size of a US Letter page and grows to fit the open document: every page is
shown at the same scale, so the panel widens for landscape pages and grows taller for long ones, such
as Legal. Smaller pages sit centred.

Every document gets a short plain summary at the top of the panel when Gemini can be reached. It is
written the first time a document is opened and saved on the headset, so opening it again, even after
a restart, shows it at once. Chart plans and mark explanations are saved the same way.

- **A PDF** shows its pages exactly as printed, as pictures you scroll by dragging, with the page count
  and reading time above them. PDF pages are drawn by Android's PDF renderer, so they only show on the
  headset; in the Unity editor the panel shows a blank page saying *Cannot be displayed*, under the
  page count, reading time and summary. Each table in the PDF becomes one chart in the column to its right, in the editor too. A
  table is read from the PDF's text: a header row and at least two rows below it with the same
  columns. If the caption under it names a chart, such as "Shown best as a heatmap", the chart is
  that kind.
- **A CSV** isn't shown as a table. The panel shows its summary above a blank page saying *Cannot be
  displayed*, and its charts fill the column to its right. Opening another document replaces the column.

### Moving panels

Panel icons sit at the far right of the title bar, in this order: icons that open the panel's side
panes, then **Move**, then **✕**.

- **Move** is a switch. While it's on, the whole panel becomes one surface: pinch it with one hand, or
  point at it and pinch, to carry it; pinch with both hands and swing them around each other to turn it
  to face you. It only turns around the vertical axis. The panel's other buttons don't respond while
  Move is on.
- **✕** closes the panel.

Open panels stand side by side on a curve in front of you, centred on where you're facing. A panel
you move by hand leaves the row and stays where you put it until you close and reopen it. When a
panel in the row changes size, such as the Documents panel when its chart column appears or it opens
a page of another size, it stays where it is and the panels beside it slide to make room.

### Language

All of the app's text follows the language you last spoke to Ada: panels, notices, chart titles, axis
titles, keys, tooltips and summaries. It starts in English each launch. When you speak another
language, the app works out which one and switches all the text at once a moment later; short replies
such as "OK" don't switch it. Text is translated by Gemini as needed and saved on the headset, so it is ready the next time.
Numbers aren't translated; they're written the way the language writes them, with its decimal and
thousands signs, digits and short number words (1,25 Mio., 12.3万). Every script is drawn in Google's Noto fonts by the OneText text engine. Without
Gemini the text stays in English.

### The voice assistant

Poke the **Assistant** button on your wrist and allow the microphone when asked. While it is black,
Ada is listening; speak naturally. Ada can:

- Open documents ("open the quarterly sales file"), go to a page, chart or edit by its number ("go to
  page 2"), or slide slowly down a list so you can read along ("scroll through it").
- Describe the charts in the column, and pin or unpin one.
- Move, turn and resize the pinned chart ("move it a bit to the left").
- Sort a chart or filter its rows, which recalculates it. Histograms and treemaps can't be sorted.
- Answer questions about a chart's data: totals, averages, minimums, maximums and counts, by group.
- Tell you what you're pointing at ("what's this one?").
- Open, close and place the Documents panel.
- Turn a scatter or bubble chart's trend line on, change its kind, or turn it off. Charts start
  without one.
- Undo changes, hers and yours, newest first. There is no redo.

Anything you can do by hand, Ada can do too, and every change either of you makes can be undone. If
you take over while Ada is in the middle of something, she stops, and a notice says how many changes
she had already made. Poke the Assistant button again to turn her off.

### Pointing from a distance

Both hands always have a ray alongside poke; there is nothing to switch on. A white line and dot
appear while a hand points at something it can use: a panel, a wrist button or a chart. Bring your
hand within about 12 cm and the ray gives way to poke. Pinch to press; the dot turns black while you
hold.

### Reading the data

Point a ray at the pinned chart, or hover a fingertip within about 3 cm of it, while none of its buttons
is on. Charts in the column don't show tooltips; pressing one pins it. The mark you're on (a bar, dot,
slice, cell, box or tile) turns black, and a tooltip by your hand shows its name and value.

**Pinch** the mark to ask Ada to explain it, whether or not the voice assistant is on. The tooltip
shows *Connecting…* and then *Ada is writing…* before filling in two plain-language lines: what the mark
stands for, and one comparison that gives its value meaning. Each explanation is written once and saved on the headset.
Hovering alone never asks Ada, so it costs nothing. When the voice assistant is on, Ada also knows what
you pinched, so you can follow up: *"why is this one so high?"*

### Putting things back in front of you

Panels and the pinned chart stay where you leave them. If you take the headset off for five seconds
or more, or recenter from the Quest menu (hold the Meta button), the layout turns and shifts to be in
front of you again.

## Setting up a build

### Prerequisites

- [ ] A Meta Quest 3 with a USB-C cable
- [ ] A Google Gemini API key on a paid tier (needed for Ada, chart planning, explanations,
      summaries and translation)
- [ ] Internet access on the headset (only Gemini needs it)

### Prepare the Meta Quest 3

1. Pair the headset with the Meta Horizon app on your phone.
2. Create or join an organization at developer.meta.com and verify your account.
3. In the Horizon app, open your headset's settings, turn on **Developer Mode**, and restart the
   headset.
4. Plug the headset into your computer, put it on, and accept **Allow USB debugging**. Check *Always
   allow from this computer*.
5. Run **Space Setup**. The app uses passthrough.

### Install Unity

1. Install Unity Hub and sign in or create an account.
2. Install Unity **6000.6.8f1**.
3. During installation, check **Android Build Support** and both of its sub-modules, **OpenJDK** and
   **Android SDK & NDK Tools**.

### Open the project

1. Clone the repository, or download the ZIP and unpack it.
2. In Unity Hub choose **Add → Add project from disk**, select the folder, and open it with
   6000.6.8f1.
3. Open `Assets/AccountingImmersiveAnalytics/Scenes/Main.unity`.

All of the project's own code and assets live in `Assets/AccountingImmersiveAnalytics`, grouped by
feature. [CONVENTIONS.md](CONVENTIONS.md) describes the folders, naming rules and design constants.

The fonts are Google's Noto family in `Assets/AccountingImmersiveAnalytics/Text/Fonts/Noto`, one file per
script. **Tools > Text > Build Noto Fonts** turns them into the text engine's font assets; run it again
after adding or replacing a font file.

### Adding documents

Put PDF or CSV files in `Assets/StreamingAssets/Documents`. The list the app reads,
`Documents/index.json`, is rewritten automatically when files change and before each build, or by hand
with **Tools > Documents > Write Document Index**.

A CSV needs a header row. Number columns are read as numbers and everything else as categories. Give
columns plain names with units in brackets, such as `Revenue ($)`, so the planned charts get good axis
titles.

### Add your Gemini API key

Create a Gemini API key in [Google AI Studio](https://aistudio.google.com/apikey). Put it on one line,
and nothing else, in `Assets/StreamingAssets/gemini.key`. Git ignores this file.

Without it the app still runs. Charts are planned by simple rules, text stays in English, there are no
summaries or explanations, and Ada turns herself off when started.

### Build to the headset

1. Connect the headset by USB and put it on, so it stays awake.
2. Open **File → Build Profiles**, select **Quest Default**, and choose your headset in the device
   list. Platform, architecture and SDK levels are already set in that profile; change nothing.
3. Click **Build And Run**. The first build takes 10 to 30 minutes; later builds are much quicker.
4. In the headset, accept the microphone prompt. Denying it leaves everything working except Ada.

You are done when the loading line finishes and the Documents panel opens with its charts beside it.

## FAQ

### Getting started

**Do I need to know Unity to use it?**
No. Setting up a build needs Unity once; using the app needs only the headset.

**Can I try it without a headset?**
Not meaningfully. Hand input and passthrough are the interface, and PDF pages only draw on the headset.

**Does it work sitting down?**
Yes. Everything opens at the height of your head. If you change between sitting and standing, take the
headset off for five seconds or recenter.

### The charts

**Why did a file get these charts and not others?**
Flash-Lite chose them from the columns, and any that didn't fit the data were dropped. Ask Ada to
describe a chart to hear what it shows and why.

**How do I move a chart?**
Pin it, then ask Ada to move, turn or resize it. Charts in the column scroll with the column: drag it
with a fingertip, or point, pinch and drag. Let go mid-swipe and it keeps gliding, slowing to a stop.

### The assistant

**It turned itself off.**
A notice says why: the microphone is not allowed, it lost its connection, or the conversation grew too
long for it to hold. Poke the Assistant button on your wrist to start a fresh conversation.

### Troubleshooting

**The Documents list is empty.**
`Assets/StreamingAssets/Documents` held no PDF or CSV files, or its `index.json` was out of date, when
the app was built.

**A PDF says it could not be loaded.**
In the Unity editor, that is expected: PDF pages only draw on the headset. On the headset, the panel
says what went wrong.

**The loading line never finishes.**
It gives up waiting after 45 seconds and opens with whatever loaded.

**The wrist buttons are missing.**
They show only while your left hand is tracked. Hold it up in view of the headset.

**A panel disappears when I lean very close.**
Anything within about 2 cm of your eyes is not drawn. Lean back a little.

### Cost and privacy

**Does it cost anything?**
Only the Gemini API, for Ada, chart planning, explanations, summaries and translation.

**Does the app store anything about me?**
No. Documents are bundled with the app. Your speech goes to Gemini only while Ada is on, and the
columns and first rows of an opened CSV, or of each table in an opened PDF, go to Gemini to plan
their charts.
