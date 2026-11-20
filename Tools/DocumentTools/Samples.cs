using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

public static class Samples
{
    private const double ChartAspect = 900.0 / 560.0;

    private sealed class Row
    {
        public string Country;
        public int Year;
        public double Value;
        public double Gdp;
        public double Population;
    }

    public static void Write(string output, string data)
    {
        List<Row> life = Read(Path.Combine(data, "life-expectancy-vs-gdp-per-capita-small.csv"));
        List<Row> carbon = Read(Path.Combine(data, "co2-emissions-vs-gdp-small.csv"));
        byte[] lifeChart = File.ReadAllBytes(Path.Combine(data, "life-chart.png"));
        byte[] carbonChart = File.ReadAllBytes(Path.Combine(data, "co2-chart.png"));

        Save(output, "Life expectancy and income", LifeExpectancy(life, lifeChart));
        Save(output, "Carbon emissions and income", Carbon(carbon, carbonChart));
        Save(output, "Esperanza de vida e ingreso", Spanish(life, lifeChart));
    }

    private static void Save(string output, string name, byte[] pdf)
    {
        Directory.CreateDirectory(output);
        File.WriteAllBytes(Path.Combine(output, name + ".pdf"), pdf);
        Console.WriteLine($"wrote {name}: {pdf.Length / 1024} KB");
    }

    private static List<Row> Read(string path)
    {
        var rows = new List<Row>();
        foreach (string line in File.ReadLines(path).Skip(1))
        {
            string[] cells = line.Split(',');
            if (cells.Length < 5) continue;
            rows.Add(new Row
            {
                Country = cells[0], Year = int.Parse(cells[1], CultureInfo.InvariantCulture),
                Value = double.Parse(cells[2], CultureInfo.InvariantCulture), Gdp = double.Parse(cells[3], CultureInfo.InvariantCulture),
                Population = double.Parse(cells[4], CultureInfo.InvariantCulture)
            });
        }
        return rows;
    }

    private static (double a, double b) Fit(IEnumerable<Row> rows, bool logValue)
    {
        var points = rows.Where(r => r.Gdp > 0 && (!logValue || r.Value > 0))
            .Select(r => (x: Math.Log10(r.Gdp), y: logValue ? Math.Log10(r.Value) : r.Value)).ToList();
        double mx = points.Average(p => p.x), my = points.Average(p => p.y);
        double b = points.Sum(p => (p.x - mx) * (p.y - my)) / points.Sum(p => (p.x - mx) * (p.x - mx));
        return (my - b * mx, b);
    }

    private static string N(double value, string format = "N0") => value.ToString(format, CultureInfo.InvariantCulture);

    private static string[][] Table(List<Row> rows, string[] header, string[] countries, string valueFormat)
    {
        var table = new List<string[]> { header };
        foreach (string country in countries)
        {
            Row row = rows.FirstOrDefault(r => r.Country == country && r.Year == 2020);
            if (row != null) table.Add(new[] { country, "$" + N(row.Gdp), N(row.Value, valueFormat) });
        }
        return table.ToArray();
    }

    private static byte[] LifeExpectancy(List<Row> rows, byte[] chart)
    {
        var latest = rows.Where(r => r.Year == 2020).ToList();
        (double a, double b) = Fit(latest, false);
        var doc = new SampleWriter("Do people in richer countries live longer?", "Study sample");
        doc.Title("Do people in richer countries live longer?");
        doc.Paragraph(("In 2020, people in the ", Run.Regular), ("richest", Run.Bold), (" countries could expect to live about ", Run.Regular),
            ("twenty years longer", Run.Bold), (" than people in the poorest ones. This short guide looks at how a country's income and its people's ", Run.Regular),
            ("life expectancy", Run.Italic), (" move together, using data for " + latest.Count + " countries.", Run.Regular));

        doc.Heading("What the chart shows", 1);
        doc.Image(chart, ChartAspect,
            "Each circle is a country in 2020. Left to right: income per person (GDP per capita, log scale, $500 to $150,000). " +
            "Bottom to top: life expectancy at birth (45 to 86 years). Circle size shows population; colour shows region: " +
            "blue Africa, orange Asia, teal Europe, yellow North America, green South America, purple Oceania.");
        doc.Paragraph(("The circles climb from the bottom left to the top right: as income rises, so does life expectancy. " +
            "The climb is ", Run.Regular), ("steepest", Run.Bold), (" among the poorest countries, where a little more income buys a lot more health.", Run.Regular));

        doc.Heading("Key points", 2);
        doc.Bullets(
            "Income and life expectancy rise together across almost every region.",
            "Each tenfold rise in income goes with about " + N(b, "0.0") + " more years of life.",
            "Some countries live much longer than their income alone would suggest.");

        doc.Heading("A common mistake", 2);
        doc.Paragraph(("Richer always means healthier.", Run.Strike), (" ", Run.Regular), ("Richer usually means healthier.", Run.Bold),
            (" Health care, clean water, education and peace matter too, which is why countries with similar incomes can differ by ten years or more.", Run.Regular));

        doc.Heading("The trend in one line", 2);
        doc.Paragraph(("A straight line through the 2020 circles, with income on a log scale, is:", Run.Regular));
        doc.Code("life_expectancy = " + N(a, "0.0") + " + " + N(b, "0.0") + " * log10(gdp_per_capita)");
        doc.Paragraph(("Read ", Run.Regular), ("log10(gdp_per_capita)", Run.Code), (" as the number of zeros in a country's income: 3 for $1,000, 4 for $10,000.", Run.Regular));

        doc.Heading("Five countries in 2020", 2);
        doc.Table(Table(rows, new[] { "Country", "GDP per capita", "Life expectancy" },
            new[] { "Nigeria", "India", "Brazil", "Germany", "Japan" }, "0.0"));

        doc.Heading("How to read the 3D chart", 2);
        doc.Numbered(
            "Find the axis for income: further along means richer.",
            "Find the axis for life expectancy: higher means longer lives.",
            "Look along the third axis to step through the years from 1990 to 2020.",
            "Pinch a point to see which country it is.");
        doc.Paragraph(("Data: Our World in Data, from the UN World Population Prospects and the World Bank (CC BY 4.0).", Run.Italic));
        return doc.Build();
    }

    private static byte[] Carbon(List<Row> rows, byte[] chart)
    {
        var latest = rows.Where(r => r.Year == 2020).ToList();
        (double a, double b) = Fit(latest, true);
        var doc = new SampleWriter("Carbon emissions and wealth", "Study sample");
        doc.Title("Carbon emissions and wealth");
        doc.Paragraph(("Richer countries emit far more carbon dioxide per person. In 2020 the average person in a high-income country was responsible for ", Run.Regular),
            ("many times", Run.Bold), (" the emissions of someone in a low-income country.", Run.Regular));

        doc.Heading("What the chart shows", 1);
        doc.Image(chart, ChartAspect,
            "Each circle is a country in 2020. Left to right: income per person (log scale). Bottom to top: carbon dioxide emissions per person " +
            "in tonnes (log scale, 0.05 to 40). Circle size shows population; colour shows region.");
        doc.Paragraph(("On both log scales the circles fall close to a straight line: when income is ten times higher, emissions are about ", Run.Regular),
            (N(Math.Pow(10, b), "0") + " times higher", Run.Bold), (".", Run.Regular));

        doc.Heading("Why it matters", 2);
        doc.Bullets(
            "Most of the carbon dioxide in the air today came from countries that grew rich first.",
            "Countries that are getting richer now will add more unless their energy gets cleaner.",
            "Some rich countries have cut emissions per person while their incomes kept rising.");

        doc.Heading("Wealth is not destiny", 2);
        doc.Paragraph(("Getting richer means emitting more.", Run.Strike), (" ", Run.Regular),
            ("Getting richer has usually meant emitting more.", Run.Bold), (" Countries with clean electricity, like France and Sweden, emit much less than others as rich as them.", Run.Regular));

        doc.Heading("The trend in one line", 2);
        doc.Code("log10(co2_per_person) = " + N(a, "0.00") + " + " + N(b, "0.00") + " * log10(gdp_per_capita)");

        doc.Heading("Five countries in 2020", 2);
        doc.Table(Table(rows, new[] { "Country", "GDP per capita", "Tonnes CO2 per person" },
            new[] { "Ethiopia", "India", "China", "France", "United States" }, "0.0"));
        doc.Paragraph(("Data: Our World in Data, from the Global Carbon Budget and the World Bank (CC BY 4.0).", Run.Italic));
        return doc.Build();
    }

    private static byte[] Spanish(List<Row> rows, byte[] chart)
    {
        var latest = rows.Where(r => r.Year == 2020).ToList();
        (double a, double b) = Fit(latest, false);
        var doc = new SampleWriter("¿Vivimos más en los países más ricos?", "Ejemplo de estudio");
        doc.Title("¿Vivimos más en los países más ricos?");
        doc.Paragraph(("En 2020, una persona en los países ", Run.Regular), ("más ricos", Run.Bold),
            (" podía esperar vivir unos veinte años más que en los más pobres. Esta guía breve compara el ingreso de cada país con la ", Run.Regular),
            ("esperanza de vida", Run.Italic), (" de su gente.", Run.Regular));

        doc.Heading("Qué muestra el gráfico", 1);
        doc.Image(chart, ChartAspect,
            "Cada círculo es un país en 2020. De izquierda a derecha: ingreso por persona (PIB per cápita, escala logarítmica). " +
            "De abajo arriba: esperanza de vida al nacer. El tamaño muestra la población y el color, la región.");
        doc.Paragraph(("Los círculos suben de abajo a la izquierda hacia arriba a la derecha: cuando sube el ingreso, sube la esperanza de vida.", Run.Regular));

        doc.Heading("Ideas clave", 2);
        doc.Bullets(
            "El ingreso y la esperanza de vida suben juntos en casi todas las regiones.",
            "Cada vez que el ingreso se multiplica por diez, la vida se alarga unos " + N(b, "0.0") + " años.",
            "Algunos países viven mucho más de lo que su ingreso haría pensar.");

        doc.Heading("Un error común", 2);
        doc.Paragraph(("Más rico siempre significa más sano.", Run.Strike), (" ", Run.Regular), ("Más rico suele significar más sano.", Run.Bold),
            (" La salud pública, el agua limpia y la educación también cuentan.", Run.Regular));

        doc.Heading("La tendencia en una línea", 2);
        doc.Code("esperanza_de_vida = " + N(a, "0.0") + " + " + N(b, "0.0") + " * log10(pib_per_capita)");

        doc.Heading("Cómo leer el gráfico 3D", 2);
        doc.Numbered(
            "Busca el eje del ingreso: más lejos significa más rico.",
            "Busca el eje de la esperanza de vida: más arriba significa vidas más largas.",
            "Recorre el tercer eje para ver los años de 1990 a 2020.");
        doc.Paragraph(("Datos: Our World in Data (CC BY 4.0).", Run.Italic));
        return doc.Build();
    }
}
