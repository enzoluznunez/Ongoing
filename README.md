# Ongoing

A mixed-reality data visualization for Meta Quest 3, built in Unity. In passthrough, a 3D
chart stands in front of you. You place it with your hands, and a voice assistant can move
it, open other charts and answer questions about its data.

The project is growing toward twelve chart types on one engine: bar, surface, ridgeline,
volume, trajectory, vector field, scatter, histogram, pie, treemap, network and Sankey.
Bar, histogram, scatter, treemap, vector field and network are built on that engine today;
the datasets for the others are in place.

- [What you will see](#what-you-will-see)
- [Using the app](#using-the-app)
- [Setting up a build](#setting-up-a-build)
- [FAQ](#faq)

## What you will see

When the app starts, a **Loading** line floats ahead of you while the first chart is read
and built. When it finishes, the chart named by `start` in the catalog rises in front of you.

| Chart | Dataset | How it reads |
|---|---|---|
| **Bar** | Quarterly revenue review | One row per region, one column per quarter; height and colour both show revenue |
| **Histogram** | Web engagement analysis | Sessions counted by length and pages viewed; taller and brighter means more |
| **Scatter** | Customer segmentation | One sphere per customer, placed by recency, order frequency and order value (vertical), sized by lifetime value, coloured by segment |
| **Treemap** | Product portfolio profitability | One tile per product line: area is revenue share, height is operating margin, colour is segment |
| **Vector field** | Wind and airflow modeling | One arrow per point, pointing with the wind; longer and brighter is faster |
| **Network** | Team collaboration mapping | One sphere per person, coloured by team and sized by how many ties they have, with a beam for each tie |

## Using the app

The app is used with your hands; put the controllers down. Everything responds to a
fingertip **poke** (push a fingertip into it from the front) or a **pinch** (thumb and
index finger together, or a full-hand grip) to grab and move. Everything also works from
a distance: point at it and pinch. See [Pointing from a distance](#pointing-from-a-distance).

### Wrist buttons

Two buttons float over your **left hand** (or your right, see [Settings](#settings))
whenever it is tracked:

- **Settings** opens and closes the settings panel.
- **Tool Panel** opens and closes the tool panel.

A button is filled black while its panel is open. A panel opens ahead of you, beside the
other when that one is open. The chart belongs to neither panel; it stays up whichever is
open.

### Settings

Each row shows a setting and its value. Poke `<` or `>` to step through its options, and
scroll the list by dragging it. A subpanel below explains the row you last changed. Settings
are kept between sessions.

| Group | Setting | Options | Default |
|---|---|---|---|
| Interface | **Animations** | On, Off | On |
| | **Sound Effects** | On, Off | On |
| | **Sound Volume** | 25%, 50%, 75%, 100% | 100% |
| | **Handedness** | Left, Right. Which hand the wrist buttons float over | Left |
| | **Text Size** | Small, Medium, Large. Panel and button text | Large |
| | **Panel Contrast** | Standard, High. Bolder text and clearer greyed-out rows | Standard |
| | **Language Mode** | English, Español | English |
| Chart Parts | **Backdrop** | On, Off. A floor and two back walls that swap sides to stay behind the chart | On |
| | **Backdrop Gridlines** | On, Off | On |
| | **Ticker Labels** | On, Off. Values and names along the axes, and names on tiles and people | On |
| | **Axis Labels** | On, Off | On |
| | **Chart Key** | On, Off. On a card beside the chart | On |
| | **Chart Description** | On, Off. The title and what the chart shows, on the same card | On |
| Chart Look | **Chart Style** | Solid, Shaded | Shaded |
| | **Chart Opacity** | 25%, 50%, 75%, 100% | 100% |
| | **Backdrop Opacity** | Low, Medium, High | Medium |
| | **Chart Label Size** | Small, Medium, Large | Medium |
| | **Chart Theme** | Dark, Light. Light uses dark labels on a pale backdrop for bright rooms | Dark |
| | **Color Mode** | Standard, Red-Green, Blue-Yellow. The subpanel keys the open chart's colours | Standard |
| Comfort and Accessibility | **Default Chart Size** | 75%, 100%, 125% | 100% |
| | **Motion Speed** | Slow, Normal, Fast | Normal |
| | **Chart Height** | Low, Medium, High. Low suits sitting | Medium |
| | **Open Distance** | Near, Normal, Far. For charts and panels | Normal |
| | **Captions** | On, Off. What you and the assistant say, in a bar low in your view | Off |
| Voice Assistant | **Explanation Level** | Simple, Standard, Expert | Standard |
| | **Assistant Speed** | 0.5 m/s, 1 m/s, Instant | 0.5 m/s |
| | **Assistant Voice** | Charon, Puck, Kore, Aoede, Fenrir. Changing it starts a new conversation | Charon |
| | **Assistant Volume** | 25%, 50%, 75%, 100% | 100% |
| | **Web Search** | On, Off. Changing it starts a new conversation | On |
| Data | **Number Format** | Short (12.5k), Full (12,500) | Short |
| | **Network Labels** | Hubs only, Everyone | Hubs only |
| | **Startup Chart** | Default, or any chart in the catalog | Default |
| App | **Recenter** | Run. Brings the layout back in front of you | |
| | **Reset All Settings** | Run. Puts every setting back to its default | |

### The tool panel

The panel is titled **Tools**. Pinch it to carry it, or pinch with both hands and twist to
turn it.

Each tool is a row: poke `<` or `>` to switch it on, and again to put it down. While a tool
is on, the subpanel to the right lists how to use it step by step, with an image under each
step, and the subpanel below describes it. **Undo** and **Undo All** are rows too.

| Tool | What it does | How |
|---|---|---|
| **Move** | Moves the chart anywhere in the room | Pinch the chart with one hand and carry it |
| **Rotate** | Turns the chart to face any direction | Pinch it with both hands and twist |
| **Scale** | Makes the chart larger or smaller, from 75% to 125% of its starting size | Pinch it with both hands and move them apart or together |
| **Assistant** | Turns the voice assistant on or off | See below |

**Sort**, **Filter** and **Profile** are greyed out for now, until their behaviour on each
chart type is decided.

**Undo** reverses the last change; **Undo All** reverses every change to the current chart.
Each chart keeps its own changes, so switching to another chart and back finds it as you
left it.

### The voice assistant

Poke **Assistant** on the tool panel and allow the microphone when asked. While the
button is filled black it is listening; speak naturally. It can:

- Move, turn and resize the chart ("move it a bit to the left").
- Open another chart ("show me the customer segmentation").
- Answer questions about the open chart's data: totals, averages, counts, comparisons.
- Open and close the tool panel and place it.

Its speed, voice and volume are in Settings. Poke **Assistant** again to turn it off.

### Pointing from a distance

Both hands always have a ray, alongside poke; there is nothing to switch on. A white line
and dot appear while a hand points at something it can use: a panel, a wrist button, or the
chart while a tool is selected. Bring your hand within about 12 cm and the ray gives way
to poke and grab. Pinch to press; the dot turns black while you hold.

| Pointing at | What a pinch does |
|---|---|
| A button or tile | Presses it when you let go |
| A panel, away from its buttons | Carries it at the same distance as your ray moves. Use both hands to turn it |
| The chart, with **Move** | Carries it the same way |
| The chart, with **Rotate** or **Scale** | Pinch it with both rays, then twist or move them apart |

### Putting things back in front of you

The chart and the tool panel stay where you leave them. If you take the headset off for five seconds
or more, or recenter from the Quest menu (hold the Meta button), the view turns and
shifts so the layout is in front of you again, at the height it first opened at.

## Setting up a build

### Prerequisites

- [ ] A Meta Quest 3 with a USB-C cable
- [ ] A Google Gemini API key on a paid tier (only the assistant needs it; the assistant
      always may search Google, which the free tier refuses)
- [ ] Internet access on the headset (only the assistant needs it)

### Prepare the Meta Quest 3

1. Pair the headset with the Meta Horizon app on your phone.
2. Create or join an organization at developer.meta.com and verify your account.
3. In the Horizon app, open your headset's settings, turn on **Developer Mode**, and
   restart the headset.
4. Plug the headset into your computer, put it on, and accept **Allow USB debugging**.
   Check *Always allow from this computer*.
5. Run **Space Setup**. The app uses passthrough.

### Install Unity

1. Install Unity Hub and sign in or create an account.
2. Install Unity **6000.6.8f1**.
3. During installation, check **Android Build Support** and both of its sub-modules,
   **OpenJDK** and **Android SDK & NDK Tools**.

### Open the project

1. Clone the repository, or download the ZIP and unpack it.
2. In Unity Hub choose **Add → Add project from disk**, select the folder, and open it
   with 6000.6.8f1.
3. Open `Assets/Scenes/Main.unity`.

### The charts and their data

Everything the app shows is read from files in `Assets/StreamingAssets`, packed into the
build; nothing is fetched from a server.

```
Assets/StreamingAssets/
  Charts/catalog.json    the charts that can be opened: dataset, blueprint, and which
                         data column drives which part of the chart
  Blueprints/<kind>.json one chart type: the channels it needs and its layout settings
  <name>.csv             the datasets, with a header row
```

Step images for the tool panel go in `Assets/Resources/ToolSteps`, named like `Move_1`, as
sprites. A step with no image shows an empty frame.

To add a chart, add its CSV and a catalog entry: its `blueprint` (a file in `Blueprints`) and
an `encoding` that gives a data column for each channel that blueprint lists, plus optional
`titles` for its axes. `start` in the catalog names the chart that opens first, unless **Startup Chart** in
Settings picks another.

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

You are done when the loading line finishes and the chart stands in front of you in
passthrough. A **No Charts** notice means `Assets/StreamingAssets/Charts/catalog.json` was
missing when the app was built, not that the build failed.

## FAQ

### Getting started

**Do I need to know Unity to use it?**
No. Setting up a build needs Unity once; using the app needs only the headset.

**Can I try it without a headset?**
Not meaningfully. Hand input and passthrough are the interface.

**Does it work sitting down?**
Yes. Everything opens at the height of your head when it first appears. If you change
between sitting and standing, take the headset off for five seconds or recenter, and the
layout comes back to eye level.

### The chart

**What does a bar's height mean?**
Its value, summed if the data has several rows for that bar, scaled so the largest bar is
full height.

**Why are only some people named on the network?**
Only hubs are labelled: people with far more ties than the rest.

**What do the colours mean?**
Whichever column the catalog names as `color`. The key is on the card beside the chart.

**I cannot grab the chart.**
Select Move, Rotate or Scale on the tool panel first. With any other tool, or none, the
chart stays put so it is not moved by accident.

### The assistant

**It turned itself off.**
A notice says why: the microphone is not allowed, it lost its connection, or the
conversation grew too long for it to hold. Poke Assistant to start a fresh conversation.

### Troubleshooting

**The app says "No Charts" and nothing loads.**
`Assets/StreamingAssets/Charts/catalog.json` was missing or unreadable when the app was built.

**A "Chart Unavailable" notice appeared.**
That chart's dataset or blueprint is missing, or its catalog entry names a column the
dataset does not have. The notice names the problem.

**The loading line never finishes.**
It gives up waiting after 45 seconds and opens with whatever loaded.

**The wrist button is missing.**
It shows only while the hand chosen under Handedness is tracked. Hold it up in view of the
headset.

**Everything went dim or grey.**
That is usually the Quest itself: its menu or a notification is open, or you are close to
your play-area boundary. Close the menu or step back toward the middle of your space.

**A panel disappears when I lean very close.**
Anything within about 2 cm of your eyes is not drawn. Lean back a little.

### Cost and privacy

**Does it cost anything?**
Only the Gemini API, for whatever the assistant is used. The data ships with the app.

**Does the app store anything about me?**
No. It reads the bundled data, and sends your speech to the assistant only while the assistant
is on.
