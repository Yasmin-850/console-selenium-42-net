using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;

[assembly: DoNotParallelize]

namespace ConsoleSelenium.Tests;

[TestClass]
public sealed class BuscaDesafioTest
{
    private ChromeDriver? _driver;
    private PaginaDuckDuckGo _duckDuckGo = null!;

    [TestInitialize]
    public void AbrirNavegador()
    {
        var options = new ChromeOptions();
        options.AddArgument("--window-size=1280,900");
        _driver = new ChromeDriver(options);
        _duckDuckGo = new PaginaDuckDuckGo(_driver);
        _duckDuckGo.Abrir();
    }

    [TestCleanup]
    public void FecharNavegador()
    {
        _driver?.Dispose();
    }

    [TestMethod]
    public void PesquisaBasicaCarregaResultados()
    {
        const string termo = "Selenium WebDriver C#";

        _duckDuckGo.Pesquisar(termo);
        var resultado = _duckDuckGo.AguardarResultados(termo);

        Assert.AreEqual(termo, PaginaDuckDuckGo.ObterTermo(resultado));
    }

    [TestMethod]
    public void TerceiraSugestaoAbreResultadosCorrespondentes()
    {
        var sugestao = _duckDuckGo.ClicarNaTerceiraSugestao("C# unit testing");
        var resultado = _duckDuckGo.AguardarResultados(sugestao);

        Assert.AreEqual(sugestao, PaginaDuckDuckGo.ObterTermo(resultado));
    }

    [TestMethod]
    public void HistoricoVoltaAosResultadosEAvancaAoSiteExterno()
    {
        const string termo = "Automated Testing";
        var driver = _driver!;
        var espera = new WebDriverWait(driver, TimeSpan.FromSeconds(25));

        _duckDuckGo.Pesquisar(termo);
        _duckDuckGo.AguardarResultados(termo);
        _duckDuckGo.ClicarNoPrimeiroResultadoOrganico();

        var destino = espera.Until(d =>
        {
            return Uri.TryCreate(d.Url, UriKind.Absolute, out var uri)
                && (uri.Scheme is "http" or "https")
                && !PaginaDuckDuckGo.EhDuckDuckGo(uri)
                ? uri
                : null;
        })!;
        Assert.IsFalse(PaginaDuckDuckGo.EhDuckDuckGo(destino));

        driver.Navigate().Back();
        var resultadosRestaurados = _duckDuckGo.AguardarResultados(termo);
        Assert.AreEqual(termo, PaginaDuckDuckGo.ObterTermo(resultadosRestaurados));

        driver.Navigate().Forward();
        espera.Until(d => d.Url == destino.AbsoluteUri);
        Assert.AreEqual(destino.AbsoluteUri, driver.Url);
    }
}
