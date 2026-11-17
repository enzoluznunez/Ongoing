# Accounting Immersive Analytics: Project Conventions

These rules apply to everything under `Assets/AccountingImmersiveAnalytics`. They follow Unity 6's guidance on project organisation and naming. When a rule changes, update this file and the code in the same pass.

## Folders

All of our content lives in one top-level folder, separate from third-party packages:

```
Assets/
  AccountingImmersiveAnalytics/   our code and assets
  Plugins/ Oculus/ InteractionSDK/ XR/ Settings/   third-party and generated
  StreamingAssets/                Unity special folder (documents)
  Resources/                      only what other packages load by name
```

Inside the project folder, content is grouped **by feature**. Each feature keeps its scripts and the assets they use together.

| Folder | Holds |
|---|---|
| `App/` | Start-up, camera, head pose, recentring, the asset library |
| `Assistant/` | Ada (Gemini): `Live/` connection, `Session/` turn state, `Tools/` Ada's tools |
| `Audio/` | Speaker and voice |
| `Data/` | Streaming-file access |
| `Documents/` | PDF reading |
| `Hands/` | Hand rays, ray visuals, the data inspector |
| `Text/` | Languages, translation, strings, number formats (generated from CLDR), `Fonts/` source fonts |
| `UI/Charts/` | Chart engine: `Data/` CSV reading, `Spec/` chart specs, planner and sort/filter, `Scales/` axes, `Layout/` the shared card layout, `Marks/` the shape surface, `Elements/` text, key panel and its slider, `Types/` one class per chart type, `View/` cards, the chart column and the pinned chart, `Tools/` tool selection; `ChartStyle` holds the chart-only constants; `ChartMarks` shader and material |
| `UI/Panels/` | Panels, cards, tooltips, panel materials and shaders |
| `UI/Icons/` | Icon images and icon scripts |
| `UI/Interactions/` | Buttons, scrolls, scrollbars, pointer and grab handling |
| `UI/Helpers/` | Shared helpers: `Style`, layout, measuring, formatting |
| `Scenes/` | `Main.unity` |
| `Editor/` | Editor tools |

Folder names are PascalCase, with no spaces.

## Assemblies

- `AccountingImmersiveAnalytics.asmdef` covers the runtime code. Its root namespace is `AccountingImmersiveAnalytics`.
- `Editor/AccountingImmersiveAnalytics.Editor.asmdef` is limited to the Editor platform.
- Don't split the runtime into more assemblies unless the dependency loops between Charts and Assistant are removed first. Unity can't compile assemblies that depend on each other.

## Scripts

- One class, struct or interface per file, and the file is named exactly after it. Small enums may stay with the class that owns them.
- Namespaces match folders, for example `AccountingImmersiveAnalytics.UI.Charts.Tools`. A file imports other features with `using` lines and never relies on the global namespace.
- Names follow C# conventions:

| Kind | Style | Example |
|---|---|---|
| Classes, structs, enums, methods, properties | PascalCase noun or verb phrase | `ChartManager`, `SetOrder` |
| Interfaces | `I` + PascalCase | `IChartModule` |
| Private fields | `_camelCase` | `_geometry` |
| Inspector fields | `[SerializeField] private _camelCase` | `[SerializeField] private AssetLibrary _library;` |
| Constants | PascalCase | `MostBars` |
| Static builders | `…Factory` | `ButtonFactory`, `LabelFactory` |

- Don't repeat what the folder or namespace already says, for example no `UI` prefix or suffix inside `UI/`.
- Interactive or stateful things are `MonoBehaviour` components. Rules and maths are static helpers.
- No comments in code. Names and structure carry the meaning.

## Assets

- Our code doesn't use `Resources.Load`. Icons, materials and shaders are reached through `AssetLibrary` (`App/AssetLibrary.asset`), which `AssetLibraryLink` on the **Systems** object in `Main.unity` hands to the code at start-up.
- Ask for icons by name: `AssetLibrary.Icon("Sort")`.
- To add an icon, put the PNG (Sprite, single) in `UI/Icons` and add it to the library's Icons list.
- The Select a Document pager asks for `Left` and `Right`.
- Move and rename assets together with their `.meta` files, so scene and prefab links survive.
- Fixed objects live in `Main.unity`, and code finds them by name. Only data content is built at runtime.

## Assistant parity

Every action a user can take by hand has an Ada tool that does the same thing, and every change either makes can be undone.

## Chart design constants

All sizes are in design units, where 1 unit is 1 mm in the headset.

| Constant | Value |
|---|---|
| Chart card | 721 × 481 (3:2), white, corner radius 32 |
| Key panel | white, corner radius 32, padding 30, 10 below the card; 3 entries show before the row slides |
| Outer spacing (card edge to axis titles, axis titles to tick values, card edge to plot box on the right) | 30 |
| Tick values to plot box | 10 |
| Title row | 30 from the top, 40 tall (`ChartStyle.TitleRow`); the plot box starts 20 below it |
| Plot box | corner radius 8, outline 2; 5 evenly spaced ticks per number axis from a labelled min to a labelled max, with black gridlines 2 wide between them |
| Marks | corner radius 2 on free corners, square where they touch the plot box or a baseline; 2 white gap between touching marks; bars fill 60% of their slot |
| Dots | radius 5, 75% opacity (scatter and bubble only); every other mark is opaque |
| Text | ticks 12, axis titles 16 (weight 600), chart title 32 (weight 600), centred across the card in the title row |
| Tick labels | at most 10 characters, then … |
| Buttons | every button, wrist menu included: 40 × 40 (`Style.ButtonSize`), corner radius 8 (`Style.MediumRadius`), icons 30 (`Style.IconSize`); white with a 2 border in black (`Style.Stroke`); selected: black with a white icon; unavailable: white with border and icon in grey (`Style.Disabled`, black at 35%); hover rises 3 mm toward the viewer (`Style.HoverRise`) with no colour change, and a press sinks it back flat and swaps the colours; `ButtonFactory.SetBorderless` drops the border for buttons that float on their own (unselected white, no outline) |
| Panels and floating cards | white (`Style.Card`), corner radius 32 (`Style.LargeRadius`), content 30 from the edges (`Style.LargePad`); tracks and inner boxes grey (`Style.Control`) |
| Shared tokens | `Style` holds text sizes 12/16/32, weights 400/600, ink measures, paddings 10/20/30, radii 2/8/32, the 2 stroke and the colours; `ChartStyle` holds only chart-specific values |
| Pinned chart buttons | Sort, Filter, Trendline, Key, Edits, Place and Scale in a column right of the pinned card, 10 apart, sized together to fill the card's height |
| Where charts show | a scrolling column beside the Documents panel (cards at half size, no key panels; it scrolls by poke or ray drag and glides to a stop, with no scrollbar); pressing a card pins it and outlines it in black (2), pressing it again unpins; one pinned chart at a time in front of the panel, card only; mark tooltips only on the pinned chart while no tool is selected |

## Checking a change

Compile with Unity's compiler from the shell, or let Unity recompile, before handing a change over. Every C# file under `Assets/AccountingImmersiveAnalytics` belongs to the runtime build except `Editor/`.
