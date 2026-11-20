# Document tools

- `./build.sh` compiles the tools against the app's own PDF reader (`Assets/AccountingImmersiveAnalytics/Documents`) and PdfPig.
- `dotnet Build/Tools.dll samples <output> <data> <fonts>` writes the sample PDFs into `<output>`, normally `Assets/StreamingAssets/Documents`. Their datasets live in `Assets/StreamingAssets/Datasets`.
  - `<data>` holds the trimmed Our World in Data CSVs and the chart PNGs.
  - `<fonts>` holds static Noto Sans Regular, Bold and Italic, and Noto Sans Mono. They are embedded in the PDFs, not shipped with the app.
- `dotnet Build/Tools.dll accounting <output> <fonts>` writes the five Maple Street Bakery practice PDFs into `<output>`, normally `Assets/StreamingAssets/Documents`: US Letter, two pages each, and two data tables each, one for each of the ten chart types (vertical and horizontal bar, line, donut, scatter, bubble, histogram, heatmap, box plot, treemap). The data is written in `AccountingSamples.cs`.
- `dotnet Build/Tools.dll read <pdf>` prints the title, pages and text passages the app's reader finds in a PDF. Ada reads this text; the panel shows the pages as pictures.

The dotnet path is written to `Build/dotnet-path` by `build.sh`.
