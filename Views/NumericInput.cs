using System.Globalization;
using System.Windows;
using System.Windows.Controls;

namespace InventarioAppDesktop.Views;

// Filtro de entrada para el campo Precio: solo dígitos con un único separador
// decimal ('.' o ','), sin letras ni símbolos. Garantiza que el valor siempre
// sea parseable a decimal y no rompa la futura suma del valor del inventario.
public static class NumericInput
{
    private static readonly System.Text.RegularExpressions.Regex PatronPrecio =
        new(@"^\d{0,10}([.,]\d{0,2})?$", System.Text.RegularExpressions.RegexOptions.Compiled);

    public static void AdjuntarFiltroPrecio(TextBox txt)
    {
        txt.PreviewTextInput += (_, e) =>
        {
            var tb = (TextBox)e.Source;
            var resultado = tb.Text.Remove(tb.SelectionStart, tb.SelectionLength)
                                   .Insert(tb.SelectionStart, e.Text);
            e.Handled = !PatronPrecio.IsMatch(resultado);
        };

        DataObject.AddPastingHandler(txt, (_, e) =>
        {
            if (e.DataObject.GetDataPresent(DataFormats.Text))
            {
                var texto = (string)e.DataObject.GetData(DataFormats.Text)!;
                if (!PatronPrecio.IsMatch(texto))
                    e.CancelCommand();
            }
        });
    }

    // "12", "12.5", "12,50" -> decimal. Vacío -> null (opcional).
    public static decimal? ParsePrecio(string texto)
    {
        if (string.IsNullOrWhiteSpace(texto))
            return null;
        var normalizado = texto.Trim().Replace(',', '.');
        return decimal.TryParse(normalizado, NumberStyles.Number, CultureInfo.InvariantCulture, out var valor)
            ? valor
            : null;
    }

    public static string FormatearPrecio(decimal? precio)
        => precio.HasValue
            ? "$" + precio.Value.ToString("N2", CultureInfo.InvariantCulture)
            : "$0.00";
}
