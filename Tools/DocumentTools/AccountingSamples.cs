using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

public static class AccountingSamples
{
    private const string Business = "Maple Street Bakery";
    private const string Author = "Maple Street Bakery, practice documents";
    private const string Invented = "Maple Street Bakery is a made-up business. Every figure in this document is invented for practice.";

    private static readonly string[] Months = { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec" };
    private static readonly double[] MonthlyRevenue = { 86400, 82100, 95300, 99800, 104600, 108900, 103200, 101700, 106400, 112300, 117800, 130100 };
    private static readonly (int year, double revenue)[] YearlyRevenue =
    {
        (2016, 612000), (2017, 655400), (2018, 701900), (2019, 748300), (2020, 590200),
        (2021, 689500), (2022, 842700), (2023, 963100), (2024, 1104800)
    };

    private static double AnnualRevenue => MonthlyRevenue.Sum();

    public static void Write(string output)
    {
        Save(output, "01 Sales Review 2025", SalesReview());
        Save(output, "02 Operating Expenses 2025", Expenses());
        Save(output, "03 Customer Invoices Q4 2025", Invoices());
        Save(output, "04 Shop Performance 2025", Shops());
        Save(output, "05 Budget vs Actual 2025", Budget());
    }

    private static void Save(string output, string name, byte[] pdf)
    {
        Directory.CreateDirectory(output);
        File.WriteAllBytes(Path.Combine(output, name + ".pdf"), pdf);
        Console.WriteLine($"wrote {name}: {pdf.Length / 1024} KB");
    }

    private static SampleWriter Letter(string title) =>
        new SampleWriter(title, Author, SampleWriter.LetterWidth, SampleWriter.LetterHeight);

    private static string N(double value, string format = "N0") => value.ToString(format, CultureInfo.InvariantCulture);

    private static string Money(double value) => "$" + N(value);

    private static string[][] Rows(string[] header, IEnumerable<string[]> body) => new[] { header }.Concat(body).ToArray();

    private static byte[] SalesReview()
    {
        double best = MonthlyRevenue.Max(), worst = MonthlyRevenue.Min();
        string bestMonth = Months[Array.IndexOf(MonthlyRevenue, best)], worstMonth = Months[Array.IndexOf(MonthlyRevenue, worst)];
        double last = YearlyRevenue[YearlyRevenue.Length - 1].revenue;
        double growth = (AnnualRevenue - last) / last * 100;

        var doc = Letter($"{Business}: Sales Review 2025");
        doc.Title($"{Business}: Sales Review 2025");
        doc.Paragraph(("Revenue", Run.Bold), (" is the money a business earns from selling its goods before any costs are taken away. In 2025 " + Business +
            " earned ", Run.Regular), (Money(AnnualRevenue), Run.Bold), (" in revenue across its ten shops.", Run.Regular));

        doc.Heading("Revenue by month", 1);
        doc.Paragraph(("Sales were lowest in " + worstMonth + " (" + Money(worst) + ") and highest in " + bestMonth + " (" + Money(best) +
            "), when holiday orders for cakes and gift boxes peak. Most months after spring sold more than ", Run.Regular), ("$100,000", Run.Bold), (".", Run.Regular));
        doc.Table(Rows(new[] { "Month", "Revenue ($)" }, Months.Select((m, i) => new[] { m, N(MonthlyRevenue[i]) })));
        doc.Caption("Table 1. Revenue by month in 2025. Shown best as a vertical bar chart.");

        doc.PageBreak();
        doc.Heading("Ten years of revenue", 1);
        doc.Paragraph(("Comparing whole years shows the trend behind the monthly ups and downs. Revenue fell in 2020, when the shops closed for part of the year, " +
            "then grew every year after. 2025 was ", Run.Regular), (N(growth, "0.0") + "% higher", Run.Bold), (" than 2024.", Run.Regular));
        doc.Table(Rows(new[] { "Year", "Revenue ($)" },
            YearlyRevenue.Select(y => new[] { y.year.ToString(CultureInfo.InvariantCulture), N(y.revenue) })
                .Concat(new[] { new[] { "2025", N(AnnualRevenue) } })));
        doc.Caption("Table 2. Revenue by year, 2016 to 2025. Shown best as a line chart.");
        doc.Heading("Key points", 2);
        doc.Bullets(
            "Revenue more than doubled between 2016 and 2025.",
            "December was the busiest month of 2025.",
            "Revenue is not profit: the costs in the Operating Expenses report come out of it.");
        doc.Paragraph((Invented, Run.Italic));
        return doc.Build();
    }

    private static byte[] Expenses()
    {
        var supplies = new (string item, double cost)[]
        {
            ("Flour and grains", 96400), ("Dairy and eggs", 71800), ("Packaging and boxes", 38200), ("Card processing fees", 27300),
            ("Equipment repairs", 18900), ("Delivery van fuel", 14600), ("Insurance", 12400), ("Accounting and legal", 9800)
        };
        var types = new (string type, double cost)[]
        {
            ("Wages", 418700), ("Ingredients", 312500), ("Rent", 156000), ("Utilities", 48300), ("Marketing", 37900)
        };
        double total = types.Sum(t => t.cost);
        double wagesShare = types[0].cost / total * 100;

        var doc = Letter($"{Business}: Operating Expenses 2025");
        doc.Title($"{Business}: Operating Expenses 2025");
        doc.Paragraph(("Expenses", Run.Bold), (" are the costs of running the business. ", Run.Regular), ("Operating expenses", Run.Italic),
            (" are the everyday ones: ingredients, wages, rent and the bills that keep the ovens on. In 2025 they added up to ", Run.Regular),
            (Money(total), Run.Bold), (".", Run.Regular));

        doc.Heading("Supplies and services", 1);
        doc.Paragraph(("Beyond wages and rent, the bakery buys many smaller things. Flour and dairy cost the most, but fees and repairs add up too. " +
            "Watching these lines is how a small business finds savings.", Run.Regular));
        doc.Table(Rows(new[] { "Category", "Cost ($)" }, supplies.Select(s => new[] { s.item, N(s.cost) })));
        doc.Caption("Table 1. Spending on supplies and services in 2025, largest first. Shown best as a horizontal bar chart.");

        doc.PageBreak();
        doc.Heading("Where each dollar goes", 1);
        doc.Paragraph(("Grouping every expense into five types shows the shape of the business. Wages are the biggest share at ", Run.Regular),
            (N(wagesShare, "0") + "%", Run.Bold), (", which is common for businesses that make their goods by hand.", Run.Regular));
        doc.Table(Rows(new[] { "Expense type", "Cost ($)" }, types.Select(t => new[] { t.type, N(t.cost) })));
        doc.Caption("Table 2. All operating expenses in 2025 by type. Shown best as a donut chart.");
        doc.Heading("Key points", 2);
        doc.Bullets(
            "Wages and ingredients together are more than three quarters of all costs.",
            "Revenue of " + Money(AnnualRevenue) + " minus these expenses leaves " + Money(AnnualRevenue - total) + " of operating profit.",
            "Rent is a fixed cost: it stays the same however much the shops sell.");
        doc.Paragraph((Invented, Run.Italic));
        return doc.Build();
    }

    private static byte[] Invoices()
    {
        double[] amounts =
        {
            420, 385, 1260, 540, 310, 2380, 760, 455, 690, 1840,
            515, 330, 980, 470, 1120, 605, 395, 1490, 830, 560
        };
        var days = new (string type, int[] values)[]
        {
            ("Cafés", new[] { 12, 15, 9, 18, 14, 21, 11 }),
            ("Grocery stores", new[] { 31, 28, 44, 36, 30, 39, 33 }),
            ("Offices", new[] { 19, 58, 24, 41, 17, 63, 29 })
        };

        var doc = Letter($"{Business}: Customer Invoices, Q4 2025");
        doc.Title($"{Business}: Customer Invoices, Q4 2025");
        doc.Paragraph(("Shops are paid at the till, but wholesale customers buy on credit and are sent an ", Run.Regular), ("invoice", Run.Bold),
            (". Money owed on unpaid invoices is called ", Run.Regular), ("accounts receivable", Run.Italic), (".", Run.Regular));

        doc.Heading("Invoice sizes", 1);
        doc.Paragraph(("These are the " + amounts.Length + " wholesale invoices sent from October to December, worth " + Money(amounts.Sum()) +
            " in all. Most are small regular orders; a few large ones come from catering.", Run.Regular));
        doc.Table(Rows(new[] { "Invoice", "Amount ($)" }, amounts.Select((a, i) => new[] { "INV-" + (2401 + i).ToString(CultureInfo.InvariantCulture), N(a) })));
        doc.Caption("Table 1. Wholesale invoice amounts, Q4 2025. Shown best as a histogram.");

        doc.PageBreak();
        doc.Heading("How long customers take to pay", 1);
        doc.Paragraph(("The bakery asks to be paid within 30 days. Cafés pay quickly; grocery stores take about a month; offices vary the most, " +
            "with some paying in under three weeks and others in two months.", Run.Regular));
        doc.Table(Rows(new[] { "Customer type", "Days to pay" },
            days.SelectMany(d => d.values.Select(v => new[] { d.type, v.ToString(CultureInfo.InvariantCulture) }))));
        doc.Caption("Table 2. Days between invoice and payment, by customer type. Shown best as a box plot.");
        doc.Paragraph((Invented, Run.Italic));
        return doc.Build();
    }

    private static byte[] Shops()
    {
        var shops = new (string name, double weight, double ads, double hours, double customers)[]
        {
            ("Maple Street", 15.2, 9800, 7600, 71200), ("Riverside", 12.1, 8100, 6400, 58900), ("Old Town", 11.4, 7200, 6100, 55300),
            ("Hillcrest", 9.6, 5900, 5300, 46800), ("Harbor", 10.3, 6400, 5700, 50100), ("University", 8.8, 3100, 5200, 63400),
            ("Westgate", 8.1, 4800, 4600, 38200), ("Lakeview", 7.6, 4100, 4400, 35900), ("Northfield", 8.9, 5200, 4900, 41700),
            ("Station Square", 8.0, 2400, 3900, 52600)
        };
        double weights = shops.Sum(s => s.weight);
        double[] sales = shops.Select(s => Math.Round(s.weight / weights * AnnualRevenue / 100) * 100).ToArray();
        sales[0] += AnnualRevenue - sales.Sum();

        var doc = Letter($"{Business}: Shop Performance 2025");
        doc.Title($"{Business}: Shop Performance 2025");
        doc.Paragraph((Business + " runs ten shops. Comparing them side by side shows which ones earn the most for what they spend. Their sales add up to the ", Run.Regular),
            (Money(AnnualRevenue), Run.Bold), (" of revenue in the Sales Review.", Run.Regular));

        doc.Heading("Advertising and sales", 1);
        doc.Paragraph(("Each shop pays for its own local advertising. Shops that advertise more tend to sell more, but not always: the University and " +
            "Station Square shops sell well on foot traffic alone.", Run.Regular));
        doc.Table(Rows(new[] { "Shop", "Advertising ($)", "Sales ($)" }, shops.Select((s, i) => new[] { s.name, N(s.ads), N(sales[i]) })));
        doc.Caption("Table 1. Local advertising spend and sales by shop, 2025. Shown best as a scatter chart.");

        doc.PageBreak();
        doc.Heading("Staff hours, sales and customers", 1);
        doc.Paragraph(("Staff hours are the biggest cost a shop controls. A shop that serves many customers with few hours is ", Run.Regular),
            ("efficient", Run.Bold), ("; one that needs many hours for each sale may be overstaffed.", Run.Regular));
        doc.Table(Rows(new[] { "Shop", "Staff hours", "Sales ($)", "Customers" },
            shops.Select((s, i) => new[] { s.name, N(s.hours), N(sales[i]), N(s.customers) })));
        doc.Caption("Table 2. Staff hours, sales and customer visits by shop, 2025. Shown best as a bubble chart, with customers as the bubble size.");
        doc.Heading("Key points", 2);
        doc.Bullets(
            "Maple Street, the first shop, sells the most and also advertises the most.",
            "University serves many customers on little advertising.",
            "Station Square makes the most sales for each staff hour.");
        doc.Paragraph((Invented, Run.Italic));
        return doc.Build();
    }

    private static byte[] Budget()
    {
        string[] departments = { "Baking", "Front counter", "Delivery", "Marketing", "Administration" };
        string[] quarters = { "Q1", "Q2", "Q3", "Q4" };
        double[,] variance =
        {
            { 2.1, 3.4, -1.2, 6.8 },
            { -0.8, 1.5, 0.4, 4.2 },
            { 5.6, 7.9, 11.3, 9.4 },
            { -6.5, -3.1, 2.7, -8.2 },
            { 0.3, -1.4, -0.6, 1.1 }
        };
        var items = new (string item, string department, double budget)[]
        {
            ("Bakers' wages", "Baking", 246000), ("Ingredients", "Baking", 305000), ("Ovens and mixers", "Baking", 42000),
            ("Counter wages", "Front counter", 158000), ("Display cases", "Front counter", 18500), ("Uniforms", "Front counter", 6200),
            ("Van lease", "Delivery", 21600), ("Fuel", "Delivery", 13000),
            ("Local ads", "Marketing", 34000), ("Loyalty program", "Marketing", 9500),
            ("Software", "Administration", 7800), ("Accounting", "Administration", 9000)
        };

        var doc = Letter($"{Business}: Budget vs Actual 2025");
        doc.Title($"{Business}: Budget vs Actual 2025");
        doc.Paragraph(("A ", Run.Regular), ("budget", Run.Bold), (" is the plan for what each part of the business will spend. Comparing it with what was actually spent " +
            "gives the ", Run.Regular), ("variance", Run.Bold), (". A positive variance here means a department spent more than planned.", Run.Regular));

        doc.Heading("Variance by department and quarter", 1);
        doc.Paragraph(("Delivery went over budget every quarter as fuel prices rose. Marketing came in under budget in most quarters " +
            "because two campaigns were postponed.", Run.Regular));
        doc.Table(Rows(new[] { "Department", "Quarter", "Variance (%)" },
            departments.SelectMany((d, di) => quarters.Select((q, qi) => new[] { d, q, N(variance[di, qi], "0.0") }))));
        doc.Caption("Table 1. Spending over (+) or under (−) budget, in percent. Shown best as a heatmap.");

        doc.PageBreak();
        doc.Heading("The 2025 budget", 1);
        doc.Paragraph(("The whole budget was ", Run.Regular), (Money(items.Sum(i => i.budget)), Run.Bold),
            (". Baking takes the largest part, because it pays for both the bakers and what they bake with.", Run.Regular));
        doc.Table(Rows(new[] { "Line item", "Department", "Budget ($)" }, items.Select(i => new[] { i.item, i.department, N(i.budget) })));
        doc.Caption("Table 2. Budget by line item, grouped by department. Shown best as a treemap.");
        doc.Heading("Key points", 2);
        doc.Bullets(
            "Small variances, within a few percent, are normal.",
            "A variance that repeats every quarter, like Delivery's, means the budget itself needs changing.",
            "Spending under budget is not always good: Marketing's savings came from work that did not happen.");
        doc.Paragraph((Invented, Run.Italic));
        return doc.Build();
    }
}
