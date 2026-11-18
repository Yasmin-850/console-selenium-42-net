using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;

namespace ConsoleSelenium.Tests;

internal sealed class PaginaDuckDuckGo
{
    private static readonly By CampoBusca = By.Name("q");
    private static readonly By Sugestoes = By.CssSelector(
        "[role='option'], .search-suggestions-list li, .sbox-suggestions li, .search__autocomplete .acp");
    private static readonly By ResultadosOrganicos = By.CssSelector(
        "a[data-testid='result-title-a'], a.result__a");

    private readonly IWebDriver _driver;
    private readonly WebDriverWait _espera;

    public PaginaDuckDuckGo(IWebDriver driver)
    {
        _driver = driver;
        _espera = new WebDriverWait(driver, TimeSpan.FromSeconds(25));
        _espera.IgnoreExceptionTypes(typeof(StaleElementReferenceException));
    }

    public void Abrir()
    {
        _driver.Navigate().GoToUrl("https://duckduckgo.com/");
    }

    public void Pesquisar(string termo)
    {
        var campo = AguardarCampo();
        campo.SendKeys(termo);
        campo.SendKeys(Keys.Enter);
    }

    public string ClicarNaTerceiraSugestao(string termo)
    {
        AguardarCampo().SendKeys(termo);

        return _espera.Until(driver =>
        {
            var opcoes = driver.FindElements(Sugestoes)
                .Where(opcao => opcao.Displayed && opcao.Enabled)
                .ToArray();
            if (opcoes.Length < 3)
                return null;

            var terceiraOpcao = opcoes[2];
            var texto = terceiraOpcao.Text.Trim();
            if (string.IsNullOrWhiteSpace(texto))
                return null;

            terceiraOpcao.Click();
            return texto;
        })!;
    }

    public Uri AguardarResultados(string termo)
    {
        return _espera.Until(driver =>
        {
            if (!Uri.TryCreate(driver.Url, UriKind.Absolute, out var url)
                || !EhDuckDuckGo(url)
                || ObterTermo(url) != termo)
                return null;

            return driver.FindElements(ResultadosOrganicos).Any(link => link.Displayed)
                ? url
                : null;
        })!;
    }

    public void ClicarNoPrimeiroResultadoOrganico()
    {
        var primeiroLink = _espera.Until(driver => driver.FindElements(ResultadosOrganicos)
            .FirstOrDefault(link => link.Displayed && link.Enabled))!;

        // Mantém a navegação na mesma aba para exercitar o histórico.
        ((IJavaScriptExecutor)_driver).ExecuteScript(
            "arguments[0].setAttribute('target', '_self');", primeiroLink);
        primeiroLink.Click();
    }

    public static bool EhDuckDuckGo(Uri url)
    {
        return url.Host.Equals("duckduckgo.com", StringComparison.OrdinalIgnoreCase)
            || url.Host.EndsWith(".duckduckgo.com", StringComparison.OrdinalIgnoreCase);
    }

    public static string? ObterTermo(Uri url)
    {
        var parametro = url.Query.TrimStart('?').Split('&')
            .FirstOrDefault(parte => parte.StartsWith("q=", StringComparison.Ordinal));

        return parametro is null
            ? null
            : Uri.UnescapeDataString(parametro[2..].Replace('+', ' '));
    }

    private IWebElement AguardarCampo()
    {
        return _espera.Until(driver => driver.FindElements(CampoBusca)
            .FirstOrDefault(campo => campo.Displayed && campo.Enabled))!;
    }
}
