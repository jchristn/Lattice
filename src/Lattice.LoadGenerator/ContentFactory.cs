namespace Lattice.LoadGenerator
{
    using System;
    using System.Collections.Generic;
    using System.Text.Json;

    /// <summary>
    /// Produces realistic synthetic content — collection themes, JSON documents (with nested objects and
    /// arrays so schemas and multi-valued index entries populate), request paths, source IPs, and identity
    /// data — so the seeded dataset reads like a real, in-use system.
    /// </summary>
    public class ContentFactory
    {
        #region Public-Members

        #endregion

        #region Private-Members

        private readonly Random _Random;
        private static readonly JsonSerializerOptions _JsonOptions = new JsonSerializerOptions { WriteIndented = false };

        private static readonly string[] _FirstNames = { "Ava", "Liam", "Noah", "Emma", "Olivia", "Mateo", "Sofia", "Kai", "Priya", "Jonas", "Mila", "Omar", "Yuki", "Lena", "Diego", "Zara", "Finn", "Aisha", "Hugo", "Nina" };
        private static readonly string[] _LastNames = { "Nguyen", "Patel", "Garcia", "Kim", "Rossi", "Haddad", "Andersson", "Silva", "Okafor", "Tanaka", "Novak", "Costa", "Meyer", "Ivanov", "Reyes", "Bauer", "Khan", "Moreau", "Weber", "Santos" };
        private static readonly string[] _Cities = { "Austin", "Berlin", "Toronto", "Lisbon", "Osaka", "Nairobi", "Bogota", "Warsaw", "Dublin", "Melbourne", "Seattle", "Amsterdam" };
        private static readonly string[] _Countries = { "US", "DE", "CA", "PT", "JP", "KE", "CO", "PL", "IE", "AU", "NL" };
        private static readonly string[] _Companies = { "Northwind", "Contoso", "Acme", "Globex", "Initech", "Umbrella", "Hooli", "Wonka", "Stark", "Wayne", "Cyberdyne", "Soylent" };
        private static readonly string[] _Categories = { "hardware", "software", "networking", "storage", "peripherals", "accessories" };
        private static readonly string[] _Colors = { "black", "silver", "blue", "red", "green", "white" };
        private static readonly string[] _Statuses = { "pending", "processing", "shipped", "delivered", "cancelled" };
        private static readonly string[] _Priorities = { "low", "normal", "high", "urgent" };
        private static readonly string[] _Severities = { "info", "warning", "error", "critical" };
        private static readonly string[] _Regions = { "us-east-1", "us-west-2", "eu-central-1", "ap-northeast-1", "sa-east-1" };
        private static readonly string[] _Metrics = { "cpu", "memory", "disk", "latency", "throughput", "temperature" };
        private static readonly string[] _Words = { "system", "data", "index", "query", "vector", "graph", "schema", "tenant", "cluster", "cache", "stream", "batch", "policy", "signal", "metric" };

        #endregion

        #region Constructors-and-Factories

        /// <summary>Instantiate.</summary>
        /// <param name="random">Random number generator.</param>
        /// <exception cref="ArgumentNullException">Thrown when the random number generator is null.</exception>
        public ContentFactory(Random random)
        {
            _Random = random ?? throw new ArgumentNullException(nameof(random));
        }

        #endregion

        #region Public-Methods

        /// <summary>The catalog of collection themes, each with a name, description, and document factory.</summary>
        /// <returns>List of themes.</returns>
        public List<CollectionTheme> Themes()
        {
            return new List<CollectionTheme>
            {
                new CollectionTheme("users", "Application user accounts and profiles", BuildUserDoc),
                new CollectionTheme("orders", "Customer orders with line items and shipping", BuildOrderDoc),
                new CollectionTheme("products", "Product catalog with attributes and tags", BuildProductDoc),
                new CollectionTheme("events", "Application and system event stream", BuildEventDoc),
                new CollectionTheme("sensor-readings", "Time-series device telemetry readings", BuildSensorDoc),
                new CollectionTheme("articles", "Published articles and their metadata", BuildArticleDoc),
                new CollectionTheme("support-tickets", "Customer support tickets and comments", BuildTicketDoc),
                new CollectionTheme("invoices", "Billing invoices with line items", BuildInvoiceDoc)
            };
        }

        /// <summary>Generate a random source IP address that looks like real client traffic.</summary>
        /// <returns>Dotted-quad IPv4 string.</returns>
        public string SourceIp()
        {
            return _Random.Next(11, 223) + "." + _Random.Next(0, 256) + "." + _Random.Next(0, 256) + "." + _Random.Next(1, 255);
        }

        /// <summary>Pick a person's first name.</summary>
        /// <returns>First name.</returns>
        public string FirstName()
        {
            return _FirstNames[_Random.Next(_FirstNames.Length)];
        }

        /// <summary>Pick a person's last name.</summary>
        /// <returns>Last name.</returns>
        public string LastName()
        {
            return _LastNames[_Random.Next(_LastNames.Length)];
        }

        /// <summary>Pick a random element from an array.</summary>
        /// <param name="values">Candidate values.</param>
        /// <returns>A random element.</returns>
        public string Pick(string[] values)
        {
            return values[_Random.Next(values.Length)];
        }

        #endregion

        #region Private-Methods

        private string BuildUserDoc()
        {
            string first = FirstName();
            string last = LastName();

            Dictionary<string, object> doc = new Dictionary<string, object>
            {
                ["firstName"] = first,
                ["lastName"] = last,
                ["email"] = (first + "." + last).ToLowerInvariant() + "@" + Pick(_Companies).ToLowerInvariant() + ".example",
                ["active"] = _Random.NextDouble() < 0.85,
                ["roles"] = PickMany(new[] { "admin", "editor", "viewer", "billing", "support" }, 1, 3),
                ["profile"] = new Dictionary<string, object>
                {
                    ["city"] = Pick(_Cities),
                    ["country"] = Pick(_Countries),
                    ["timezoneOffset"] = _Random.Next(-8, 10)
                },
                ["loginCount"] = _Random.Next(0, 500)
            };

            return Serialize(doc);
        }

        private string BuildOrderDoc()
        {
            int itemCount = _Random.Next(1, 5);
            List<object> items = new List<object>();
            double total = 0.0;

            for (int i = 0; i < itemCount; i++)
            {
                int qty = _Random.Next(1, 6);
                double price = Math.Round(5.0 + (_Random.NextDouble() * 500.0), 2);
                total += qty * price;
                items.Add(new Dictionary<string, object>
                {
                    ["sku"] = "SKU-" + _Random.Next(1000, 9999),
                    ["name"] = Pick(_Words) + " " + Pick(_Categories),
                    ["quantity"] = qty,
                    ["price"] = price
                });
            }

            Dictionary<string, object> doc = new Dictionary<string, object>
            {
                ["orderNumber"] = "ORD-" + _Random.Next(100000, 999999),
                ["customer"] = FirstName() + " " + LastName(),
                ["status"] = Pick(_Statuses),
                ["currency"] = "USD",
                ["total"] = Math.Round(total, 2),
                ["items"] = items,
                ["shippingAddress"] = new Dictionary<string, object>
                {
                    ["city"] = Pick(_Cities),
                    ["country"] = Pick(_Countries)
                }
            };

            return Serialize(doc);
        }

        private string BuildProductDoc()
        {
            Dictionary<string, object> doc = new Dictionary<string, object>
            {
                ["sku"] = "SKU-" + _Random.Next(1000, 9999),
                ["name"] = Capitalize(Pick(_Words)) + " " + Capitalize(Pick(_Categories)),
                ["category"] = Pick(_Categories),
                ["price"] = Math.Round(5.0 + (_Random.NextDouble() * 900.0), 2),
                ["inStock"] = _Random.NextDouble() < 0.7,
                ["tags"] = PickMany(new[] { "new", "sale", "popular", "limited", "refurbished", "bundle" }, 1, 3),
                ["attributes"] = new Dictionary<string, object>
                {
                    ["color"] = Pick(_Colors),
                    ["weightKg"] = Math.Round(0.1 + (_Random.NextDouble() * 12.0), 2)
                }
            };

            return Serialize(doc);
        }

        private string BuildEventDoc()
        {
            Dictionary<string, object> doc = new Dictionary<string, object>
            {
                ["eventType"] = Pick(new[] { "login", "logout", "purchase", "signup", "error", "deploy", "scale" }),
                ["severity"] = Pick(_Severities),
                ["source"] = Pick(_Words) + "-service",
                ["message"] = Capitalize(Pick(_Words)) + " " + Pick(_Words) + " completed",
                ["tags"] = PickMany(_Words, 1, 4),
                ["metadata"] = new Dictionary<string, object>
                {
                    ["host"] = "host-" + _Random.Next(1, 40),
                    ["region"] = Pick(_Regions)
                }
            };

            return Serialize(doc);
        }

        private string BuildSensorDoc()
        {
            int readingCount = _Random.Next(3, 8);
            List<object> readings = new List<object>();
            for (int i = 0; i < readingCount; i++)
            {
                readings.Add(Math.Round(_Random.NextDouble() * 100.0, 2));
            }

            Dictionary<string, object> doc = new Dictionary<string, object>
            {
                ["deviceId"] = "dev-" + _Random.Next(1000, 9999),
                ["metric"] = Pick(_Metrics),
                ["value"] = Math.Round(_Random.NextDouble() * 100.0, 2),
                ["unit"] = Pick(new[] { "percent", "ms", "celsius", "mbps", "gb" }),
                ["readings"] = readings,
                ["location"] = new Dictionary<string, object>
                {
                    ["site"] = Pick(_Cities),
                    ["region"] = Pick(_Regions)
                }
            };

            return Serialize(doc);
        }

        private string BuildArticleDoc()
        {
            Dictionary<string, object> doc = new Dictionary<string, object>
            {
                ["title"] = Capitalize(Pick(_Words)) + " " + Capitalize(Pick(_Words)) + " in Practice",
                ["author"] = FirstName() + " " + LastName(),
                ["published"] = _Random.NextDouble() < 0.8,
                ["wordCount"] = _Random.Next(300, 4000),
                ["tags"] = PickMany(_Words, 2, 5),
                ["sections"] = PickMany(new[] { "intro", "background", "design", "results", "conclusion" }, 2, 5)
            };

            return Serialize(doc);
        }

        private string BuildTicketDoc()
        {
            int commentCount = _Random.Next(0, 4);
            List<object> comments = new List<object>();
            for (int i = 0; i < commentCount; i++)
            {
                comments.Add(new Dictionary<string, object>
                {
                    ["author"] = FirstName(),
                    ["body"] = Capitalize(Pick(_Words)) + " " + Pick(_Words) + " needs attention"
                });
            }

            Dictionary<string, object> doc = new Dictionary<string, object>
            {
                ["ticketId"] = "TCK-" + _Random.Next(10000, 99999),
                ["subject"] = Capitalize(Pick(_Words)) + " issue with " + Pick(_Categories),
                ["priority"] = Pick(_Priorities),
                ["status"] = Pick(new[] { "open", "in-progress", "resolved", "closed" }),
                ["assignee"] = FirstName() + " " + LastName(),
                ["labels"] = PickMany(new[] { "bug", "question", "feature", "regression", "docs" }, 1, 3),
                ["comments"] = comments
            };

            return Serialize(doc);
        }

        private string BuildInvoiceDoc()
        {
            int lineCount = _Random.Next(1, 5);
            List<object> lines = new List<object>();
            double amount = 0.0;
            for (int i = 0; i < lineCount; i++)
            {
                double lineTotal = Math.Round(20.0 + (_Random.NextDouble() * 2000.0), 2);
                amount += lineTotal;
                lines.Add(new Dictionary<string, object>
                {
                    ["description"] = Capitalize(Pick(_Words)) + " " + Pick(_Categories),
                    ["amount"] = lineTotal
                });
            }

            Dictionary<string, object> doc = new Dictionary<string, object>
            {
                ["invoiceNumber"] = "INV-" + _Random.Next(100000, 999999),
                ["customer"] = Pick(_Companies) + " Inc.",
                ["amount"] = Math.Round(amount, 2),
                ["currency"] = "USD",
                ["paid"] = _Random.NextDouble() < 0.6,
                ["lineItems"] = lines
            };

            return Serialize(doc);
        }

        private List<object> PickMany(string[] values, int min, int max)
        {
            int count = _Random.Next(min, max + 1);
            List<object> result = new List<object>();
            List<string> pool = new List<string>(values);
            for (int i = 0; i < count && pool.Count > 0; i++)
            {
                int index = _Random.Next(pool.Count);
                result.Add(pool[index]);
                pool.RemoveAt(index);
            }
            return result;
        }

        private static string Capitalize(string value)
        {
            if (String.IsNullOrEmpty(value)) return value;
            return Char.ToUpperInvariant(value[0]) + value.Substring(1);
        }

        private static string Serialize(Dictionary<string, object> doc)
        {
            return JsonSerializer.Serialize(doc, _JsonOptions);
        }

        #endregion
    }
}
