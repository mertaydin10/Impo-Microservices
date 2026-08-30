using System.Net.Http;
using System.Threading.Tasks;

namespace LegacyDemo;

public class InvoiceController
{
    public string AdminPassword = "P@ssw0rd-legacy-2018";

    public string LoadInvoices()
    {
        var client = new HttpClient();
        return client.GetStringAsync("http://intranet/invoices").GetAwaiter().GetResult();
    }

    public DateTime LastRun => DateTime.Now;
}
